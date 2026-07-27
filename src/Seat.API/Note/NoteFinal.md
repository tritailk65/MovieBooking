Mình đã đọc toàn bộ note trong [Seat.API/Note](/Users/tari/Project/Microservice_Learning/MovieBooking/Seat.API/Note) và đối chiếu với code hiện tại bằng CodeGraph. Kết luận lớn nhất: bạn đang đi đúng hướng, nhưng nên **chốt lại Seat Service như một workflow/state-machine service**, không tiếp tục mở rộng theo CRUD từng ghế rời rạc nữa.

**Tóm Tắt Vấn Đề**
Các note của bạn xoay quanh 5 vấn đề chính:

1. Redis cần 3 nhóm dữ liệu: `seat map`, `lock key`, `reservation key`.
2. `Locked` là trạng thái tạm thời, nếu lưu cứng vào seat map thì dễ bị mồ côi khi TTL hết.
3. REST snapshot không đủ realtime cho UI đặt vé, về sau cần SignalR/SSE.
4. `ReleaseSeat` và `MarkSeatSold` hiện xử lý từng ghế, nhưng nghiệp vụ cuối nên xử lý theo reservation.
5. Booking nên là DDD/CQRS, còn Seat không cần full DDD nhưng phải có workflow rõ.

**Làm Trước**
Việc đầu tiên nên làm là **chốt lại source of truth của Seat Service**.

Theo mình nên chọn:

```text
Sold / Available: nằm trong showtime seat map
Locked: tính từ active lock key/index
Reservation: nằm trong reservation key
```

Tức là không nên để `SeatStatus.Locked` trong seat hash là nguồn tin chính nữa. Hiện tại [LockSeatCommandHandler.cs](/Users/tari/Project/Microservice_Learning/MovieBooking/Seat.API/Application/Seats/Commands/LockSeat/LockSeatCommandHandler.cs:56) đang set `SeatStatus = Locked`, nên đây là điểm gây ra ghế mồ côi khi Redis lock key hết TTL.

Sau đó làm tiếp **Redis data model basic**:

```text
showtime:{showtimeId}:seats
lock:showtime:{showtimeId}:seat:{seatId}
reservation:showtime:{showtimeId}:user:{userId}
locks:showtime:{showtimeId}:active
```

`locks:showtime:{id}:active` nên là Sorted Set để biết lock nào còn hiệu lực/hết hạn.

**Làm Kế Tiếp**
Tiếp theo, sửa các command theo workflow nghiệp vụ.

Giữ:

```text
LockSeatCommand
ReleaseSeatCommand
```

nhưng hiểu là thao tác UI từng ghế.

Thêm mới:

```text
ReleaseReservationCommand
ConfirmReservationCommand hoặc MarkReservationSoldCommand
ValidateReservationCommand
```

Vì khi checkout/payment/cancel thì xử lý theo cả reservation, không nên gọi từng ghế. Hiện tại [MarkSeatSoldCommandHandler.cs](/Users/tari/Project/Microservice_Learning/MovieBooking/Seat.API/Application/Seats/Commands/MarkSeatSold/MarkSeatSoldCommandHandler.cs:33) đang xử lý một ghế, phần này nên được thay bằng confirm reservation về sau.

**Làm Sau Đó**
Sau command workflow, mới sửa luồng đọc.

Endpoint đọc chính nên là:

```http
GET /api/v1/seat/{showtimeId}/map
```

Nhưng handler không nên chỉ trả raw `ShowtimeSeat` như hiện tại ở [GetShowtimeSeatQueryHandler.cs](/Users/tari/Project/Microservice_Learning/MovieBooking/Seat.API/Application/Seats/Queries/GetShowtimeSeats/GetShowtimeSeatQueryHandler.cs:29). Nó nên trả DTO đã merge:

```text
seat map + active lock index => current seat status
```

Response nên có:

```text
showtimeId
serverTimeUtc
version
seats: seatId, status, lockedUntil, lockedByMe nếu cần
```

Đây là bước giúp UI render đúng mà không phụ thuộc background cleanup.

**Làm Sau Nữa**
Khi Seat Service ổn basic rồi, mới nối với Booking/Payment.

Basic flow nên là:

```text
User chọn ghế -> Seat lock + reservation
User bấm đặt vé -> Booking tạo PendingPayment
Booking validate reservation với Seat
Payment success -> Booking gọi Seat ConfirmReservation
Payment failed/cancel/expired -> Booking gọi Seat ReleaseReservation
```

Chưa cần event phức tạp ngay. Ban đầu dùng HTTP sync giữa Booking và Seat sẽ dễ debug hơn.

**Làm Cuối**
Realtime và event nên làm sau khi workflow đã đúng.

Thứ tự hợp lý:

```text
1. REST snapshot đúng
2. Command lock/release/confirm đúng
3. Reservation lifecycle đúng
4. SignalR/SSE push seat status changed
5. Integration events/outbox/idempotency
6. Production optimization/Lua script/rate limit
```

Đừng làm SignalR, keyspace notification, Lua script, outbox quá sớm. Những thứ đó tốt, nhưng nếu source of truth và workflow chưa rõ thì chỉ làm hệ thống rối hơn.

**Ưu Tiên Ngắn Gọn**
Thứ tự mình khuyên bạn làm:

1. Chốt lại data model Redis và bỏ tư duy CRUD `update seat status` thuần.
2. Refactor `Locked` thành derived state từ lock key/index.
3. Sửa `GET seat map` để trả trạng thái hiện tại đã merge.
4. Thêm `ReleaseReservation` và `ConfirmReservation/MarkReservationSold`.
5. Điều chỉnh Booking flow: `PendingPayment -> Confirmed/Cancelled/Expired`.
6. Sau đó mới thêm realtime update.
7. Cuối cùng mới tối ưu production bằng Lua/idempotency/outbox.

Nói gọn: **làm đúng state model trước, làm đúng reservation workflow sau, rồi mới làm realtime và event-driven.**