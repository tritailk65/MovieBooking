# Booking Client – Yêu cầu phát triển

## 1. Mục đích tài liệu

Tài liệu này mô tả yêu cầu dành cho team phát triển ứng dụng booking client bằng Flutter, tích hợp với hệ thống MovieBooking hiện tại.

Ứng dụng giai đoạn đầu tập trung vào khách hàng đặt vé trên Android và iOS. Web admin để quản lý movie, cinema, hall, seat và showtime không nằm trong phạm vi của giai đoạn này.

API Gateway sẽ được hoàn thiện trước khi frontend bắt đầu tích hợp API. Booking client chỉ được giao tiếp với API Gateway, không gọi trực tiếp Catalog API, Seat API, Booking API, Payment API, RabbitMQ, Redis hoặc gRPC.

## 2. Mục tiêu sản phẩm

Booking client phải cho phép người dùng hoàn thành happy path:

1. Xem danh sách phim.
2. Chọn phim và suất chiếu.
3. Xem sơ đồ ghế.
4. Chọn và giữ ghế trong một khoảng thời gian.
5. Xem thông tin reservation và thời gian còn lại.
6. Tạo booking từ reservation.
7. Bắt đầu thanh toán.
8. Theo dõi trạng thái xử lý bất đồng bộ.
9. Nhận kết quả booking thành công và xem chi tiết booking.
10. Xem lịch sử booking của người dùng.

## 3. Phạm vi giai đoạn đầu

### 3.1. Trong phạm vi

- Ứng dụng Flutter cho Android và iOS.
- Danh sách phim.
- Danh sách suất chiếu.
- Sơ đồ ghế có hỗ trợ zoom/pan.
- Lock và release ghế.
- Countdown thời gian giữ ghế.
- Tạo booking.
- Bắt đầu payment giả lập hiện tại.
- Theo dõi booking cho đến trạng thái kết thúc.
- Chi tiết và lịch sử booking.
- Xử lý loading, empty state, lỗi mạng và xung đột ghế.
- Chuẩn bị kiến trúc để bổ sung authentication sau này.

### 3.2. Ngoài phạm vi

- Web admin.
- CRUD master data từ booking client.
- App gọi API tạo showtime.
- Thanh toán provider thật và provider SDK.
- Refund hoặc cancel booking từ UI.
- Push notification.
- SignalR/realtime ở giai đoạn đầu.
- Offline booking.
- Lưu thông tin thẻ thanh toán trong app.

## 4. Điều kiện tiên quyết từ backend và API Gateway

Frontend chỉ bắt đầu ghép API khi API Gateway đáp ứng tối thiểu các yêu cầu sau:

- Cung cấp một `API_BASE_URL` duy nhất cho ứng dụng.
- Route đầy đủ tới Catalog, Seat và Booking service.
- HTTPS cho môi trường staging và production.
- Công bố một OpenAPI document dùng cho client, hoặc các OpenAPI document có URL ổn định.
- Không công bố endpoint nội bộ của Saga, Payment consumer, Redis hoặc gRPC.
- Chuyển tiếp `X-Correlation-Id` và các trace header tới backend.
- Chuẩn hóa error response, ưu tiên RFC Problem Details.
- Giữ nguyên HTTP status quan trọng như `400`, `404`, `409`, `500`, `503`.
- Có timeout phù hợp cho request HTTP thông thường; không giữ request chờ Saga hoàn tất.
- Có endpoint health/readiness dành cho vận hành, không cần hiển thị trong app.
- Có bộ URL riêng cho development, staging và production.

Các đường dẫn trong tài liệu này là đường dẫn backend hiện tại. Nếu Gateway rewrite URL, OpenAPI của Gateway là contract cuối cùng và là nguồn sự thật cho frontend.

## 5. Tech stack bắt buộc

### 5.1. Nền tảng

- Flutter stable.
- Dart stable tương ứng với Flutter SDK.
- Android và iOS từ cùng một codebase.
- Bật strict analysis và không bỏ qua analyzer warning nếu không có lý do rõ ràng.

### 5.2. Thư viện đề xuất

| Nhu cầu | Công nghệ |
|---|---|
| Navigation và deep link | `go_router` |
| State management | `flutter_riverpod` |
| Riverpod code generation | `riverpod_annotation`, `riverpod_generator` |
| HTTP | `dio` |
| API client | Generate từ Gateway OpenAPI với target `dart-dio` |
| Model do app tự định nghĩa | `freezed`, `json_serializable` |
| Generated code | `build_runner` |
| Secret/token storage sau này | `flutter_secure_storage` |
| Non-sensitive preferences | `shared_preferences` |
| Date/number/currency formatting | `intl` |
| Network status | `connectivity_plus` |
| Remote image | `cached_network_image` |
| Seat map | `CustomPainter` và `InteractiveViewer` |
| Logging | Logger abstraction; không gọi `print` trong production code |

Không sử dụng đồng thời Riverpod, Bloc và Provider trong cùng project. Riverpod là lựa chọn duy nhất cho state management của giai đoạn này.

## 6. Kiến trúc frontend

Sử dụng feature-first architecture, tách domain, data và presentation ở mức vừa đủ; không áp dụng Clean Architecture quá nặng cho các màn hình đơn giản.

```text
lib/
├── app/
│   ├── app.dart
│   ├── router.dart
│   └── bootstrap.dart
├── core/
│   ├── api/
│   ├── config/
│   ├── errors/
│   ├── logging/
│   ├── storage/
│   └── theme/
├── generated/
│   └── api/
├── features/
│   ├── catalog/
│   ├── showtime/
│   ├── seat_selection/
│   ├── reservation/
│   ├── booking/
│   └── payment/
└── shared/
    ├── widgets/
    └── utils/
```

### 6.1. Phân loại state

Riverpod async provider quản lý server state:

- Movies.
- Showtimes.
- Seat map.
- Reservation.
- Booking detail.
- Booking history.
- Polling booking status.

Riverpod notifier quản lý state tạm thời của checkout:

- `showtimeId` đang chọn.
- Danh sách `selectedSeatIds`.
- `lockToken` theo từng seat.
- `reservationId`.
- `bookingId`.

Không lưu lâu dài bản sao của giá vé, seat availability hoặc booking status. Backend là nguồn dữ liệu chính xác.

## 7. Navigation và màn hình

Route đề xuất:

```text
/movies
/movies/:movieId/showtimes
/showtimes/:showtimeId/seats
/checkout
/bookings/:bookingId/processing
/bookings/:bookingId
/bookings
```

### 7.1. Movies

- Hiển thị danh sách phim.
- Hiển thị poster, title, description rút gọn, duration và trạng thái đang chiếu.
- Có loading skeleton, empty state và retry state.

### 7.2. Showtimes

- Lọc suất chiếu theo movie, cinema và ngày.
- Hiển thị cinema, địa chỉ, hall, start time, end time và base price.
- Chỉ cho phép mở seat map khi `showtimeId` hợp lệ.

### 7.3. Seat map

- Có zoom và pan.
- Phân biệt tối thiểu: Available, Selected, Locked và Sold.
- Không chỉ sử dụng màu; phải có legend hoặc biểu tượng để hỗ trợ accessibility.
- Lock từng ghế ngay khi người dùng chọn.
- Nếu backend trả `409 Conflict`, bỏ chọn ghế, refresh seat map và hiển thị thông báo.
- Lưu `lockToken` theo từng ghế để release khi người dùng bỏ chọn.

### 7.4. Checkout

- Hiển thị phim, suất chiếu, cinema, hall, ghế, tổng tiền và countdown.
- Lấy `reservationId` từ Seat API trước khi tạo booking.
- Không tự tính giá để gửi lên backend như nguồn sự thật.
- Không cho tạo booking khi reservation hết hạn.

### 7.5. Payment processing

- Gọi API bắt đầu payment một lần.
- Sau đó poll booking detail; không gửi lặp payment command khi rebuild widget.
- Hiển thị processing state cho đến khi booking đạt trạng thái kết thúc.
- Cho phép retry việc đọc trạng thái, không tự retry command payment nếu chưa xác định tính idempotent.

### 7.6. Booking detail và history

- Hiển thị booking ID, trạng thái, thời gian tạo, showtime và danh sách ghế.
- Trạng thái phải được map sang text thân thiện với người dùng.
- Không hiển thị trực tiếp enum/raw state của Saga nếu không có mapping UI.

## 8. Happy path và API hiện có

### 8.1. Danh sách phim

```http
GET /api/v1/catalog/movies
```

Backend hiện trả `PaginatedResult<MovieDto>`, mặc định trang 1 và 10 bản ghi.

### 8.2. Danh sách suất chiếu

```http
GET /api/v1/catalog/showtimes
GET /api/v1/catalog/showtimes?movieId=1
GET /api/v1/catalog/showtimes?cinemaId=1
GET /api/v1/catalog/showtimes?date=2026-08-02
GET /api/v1/catalog/showtimes?movieId=1&cinemaId=1&date=2026-08-02
```

Response hiện có:

```json
[
  {
    "id": 101,
    "movieId": 1,
    "movieTitle": "Movie title",
    "cinemaId": 1,
    "cinemaName": "Cinema 1",
    "cinemaAddress": "123 Street",
    "cinemaCity": "Ho Chi Minh",
    "hallId": 1,
    "hallName": "Hall 1",
    "startTime": "2026-08-02T18:00:00Z",
    "endTime": "2026-08-02T20:00:00Z",
    "basePrice": 90000
  }
]
```

### 8.3. Sơ đồ ghế

```http
GET /api/v1/seat/{showtimeId}/map
```

Seat map được tạo bất đồng bộ sau khi backend tạo showtime. App phải hỗ trợ trạng thái seat map chưa sẵn sàng và retry có giới hạn.

### 8.4. Lock ghế

```http
POST /api/v1/seat/lock
Content-Type: application/json
```

```json
{
  "showtimeId": 101,
  "seatId": "A1",
  "userId": "frontend-dev-user"
}
```

Success response:

```json
{
  "message": "Locked seat successfully!",
  "lockToken": "...",
  "lockExpiration": "2026-08-02T18:10:00Z"
}
```

Seat đã bị người khác chọn trả `409 Conflict`.

### 8.5. Release ghế

```http
POST /api/v1/seat/release
Content-Type: application/json
```

```json
{
  "showtimeId": 101,
  "seatId": "A1",
  "userId": "frontend-dev-user",
  "lockToken": "..."
}
```

### 8.6. Lấy reservation

```http
GET /api/v1/seat/reservation?showtimeId=101&userId=frontend-dev-user
```

Các trường quan trọng:

```json
{
  "id": "reservation-guid",
  "showtimeId": 101,
  "userId": "frontend-dev-user",
  "seatIds": ["A1", "A2"],
  "expiresAt": "2026-08-02T18:10:00Z",
  "remainingSeconds": 580,
  "basePrice": 90000,
  "reservationVersion": 0
}
```

### 8.7. Tạo booking từ reservation

Backend route hiện tại:

```http
POST /api/booking/from-reservation
Content-Type: application/json
```

```json
{
  "showtimeId": 101,
  "userId": "frontend-dev-user",
  "userName": "Frontend Dev User",
  "reservationId": "reservation-guid"
}
```

Success response là `201 Created`:

```json
{
  "bookingId": 123,
  "reservationId": "reservation-guid",
  "requestId": "request-guid",
  "status": "Submitted"
}
```

App phải lưu `bookingId` trong checkout state để bắt đầu payment và theo dõi booking.

### 8.8. Bắt đầu payment

```http
PUT /api/booking/payment
Content-Type: application/json
```

```json
{
  "bookingId": 123
}
```

Payment hiện được backend giả lập. API trả thành công chỉ có nghĩa là command bắt đầu payment được chấp nhận; không có nghĩa Saga đã hoàn tất.

### 8.9. Theo dõi booking

```http
GET /api/booking/{bookingId}
```

App poll API này mỗi 2 giây khi đang ở payment-processing screen và dừng khi đạt trạng thái kết thúc.

### 8.10. Lịch sử booking

Backend route hiện tại:

```http
GET /api/booking/{userId}
```

Gateway cần giữ khả năng phân biệt route booking ID và user ID. Trước production nên cân nhắc contract rõ ràng hơn, ví dụ `/api/v1/bookings/by-user/{userId}` hoặc `/api/v1/me/bookings`.

### 8.11. API không được gọi trực tiếp từ app

Các endpoint sau là endpoint nội bộ, endpoint test hoặc được Saga điều phối; booking client không được sử dụng:

- `POST /api/v1/catalog/showtimes` – chỉ dùng Postman/bootstrap/master data trong giai đoạn hiện tại.
- `POST /api/v1/seat/markseatsold`.
- `POST /api/v1/seat/reservation-release`.
- `PUT /api/v1/seat/validation-reservation`.
- Payment service consumer endpoints/message queue.
- Saga command/event endpoints hoặc RabbitMQ queues.

## 9. Quy tắc countdown và thời gian

- Backend gửi thời gian theo UTC ISO 8601.
- App parse thành `DateTime` UTC và chỉ convert sang local time tại presentation layer.
- Countdown phải được tính từ `expiresAt`, không xem `remainingSeconds` ban đầu là đồng hồ chính xác tuyệt đối.
- Khi countdown về 0, app phải refresh reservation và seat map.
- Không tự gia hạn reservation nếu người dùng chưa thực hiện hành động rõ ràng.
- Không sử dụng thời gian trên thiết bị để kết luận thanh toán đã thành công.

## 10. Polling trạng thái Saga

App không triển khai lại Saga state machine. App chỉ quan sát trạng thái booking do backend trả về.

Quy tắc polling:

- Poll mỗi 2 giây ở payment-processing screen.
- Dừng polling khi widget bị dispose.
- Dừng polling khi booking đạt trạng thái kết thúc.
- Có timeout UI hợp lý, nhưng timeout UI không được tự kết luận payment thất bại.
- Khi timeout UI, cho phép người dùng vào booking history để tiếp tục kiểm tra.

Nhóm trạng thái UI đề xuất:

| Nhóm UI | Backend status ví dụ | Hiển thị |
|---|---|---|
| Processing | Submitted, AwaitingSeatValidation, PendingPayment, PaymentProcessing | Đang xử lý |
| Success | Paid | Đặt vé thành công |
| Failed | Cancelled, Expired | Không thể hoàn tất |

Mapping chính xác phải được cập nhật theo `BookingStatus` mà Booking API thực tế trả về.

## 11. Xử lý lỗi

| HTTP status | Hành vi app |
|---|---|
| `400` | Hiển thị lỗi input/reservation không hợp lệ; không retry tự động mutation |
| `404` | Hiển thị resource không tồn tại hoặc đã bị xóa |
| `409` | Refresh seat map; thông báo ghế vừa bị người khác chọn |
| `408`/timeout | Cho phép retry read request; mutation phải kiểm tra kết quả trước khi gửi lại |
| `429` | Tôn trọng `Retry-After` nếu có |
| `500` | Hiển thị lỗi hệ thống và correlation ID |
| `502`/`503`/`504` | Hiển thị hệ thống tạm thời gián đoạn; retry read với backoff |

Mọi lỗi phải đi qua một `AppException`/`ApiFailure` thống nhất. Widget không tự parse `DioException`.

## 12. HTTP client và API contract

- API client phải được generate từ OpenAPI của Gateway.
- Không viết tay lại DTO đã có trong OpenAPI nếu không cần thiết.
- Không gọi Dio trực tiếp từ widget.
- Mọi request đi qua repository/data source hoặc generated client.
- Cấu hình connect/send/receive timeout tập trung.
- Gửi `X-Correlation-Id` mới cho mỗi user operation nếu Gateway chưa tự tạo.
- Log `requestId`, `bookingId`, `reservationId` và correlation ID; không log token hoặc dữ liệu thẻ.
- Giá tiền dùng decimal-compatible representation từ API; không dùng phép tính `double` làm nguồn sự thật cho thanh toán.

## 13. Authentication trong tương lai

Authentication/authorization chưa bật ở backend trong giai đoạn hiện tại. Team frontend vẫn phải chuẩn bị abstraction:

```dart
class AuthSession {
  final String userId;
  final String? accessToken;
}
```

Trong development có thể dùng một `userId` cấu hình sẵn. Không rải hard-coded user ID trong widget hoặc repository.

Khi authentication được bật:

- Access token lưu trong `flutter_secure_storage`.
- Dio interceptor thêm bearer token.
- `userId` lấy từ authenticated session/claims.
- Backend/Gateway nên bỏ việc tin tưởng `userId` do request body truyền lên.

## 14. Environment configuration

App chỉ cần một URL:

```text
API_BASE_URL=https://gateway.example.com
```

Tối thiểu có ba flavor:

- Development.
- Staging.
- Production.

Không commit production secret vào source code. Base URL không phải secret nhưng phải được cấu hình theo flavor.

Trong local development:

- Android emulator không được giả định rằng `localhost` là máy host.
- Thiết bị thật cần địa chỉ Gateway có thể truy cập qua LAN hoặc tunnel HTTPS.
- Không đưa hostname nội bộ Aspire như `catalog-api`, `seat-api` hoặc `booking-api` vào app.

## 15. UI/UX và accessibility

- Hỗ trợ light/dark theme nếu thiết kế yêu cầu; tối thiểu phải có design tokens tập trung.
- Tất cả màn hình có loading, empty, error và retry state.
- Seat state không chỉ phân biệt bằng màu.
- Touch target đủ lớn trên mobile.
- Countdown phải có text dễ hiểu.
- Format tiền theo locale nhưng không làm thay đổi giá trị backend.
- Không chặn back navigation mà không cảnh báo khi người dùng đang giữ ghế.
- Nếu người dùng rời seat-selection screen, app phải có chiến lược release ghế hoặc thông báo lock sẽ tự hết hạn.

## 16. Chất lượng và kiểm thử

Team frontend cần chuẩn bị:

- Unit test cho status mapping, countdown và API failure mapping.
- Provider test cho seat selection và payment polling.
- Widget test cho seat state và checkout validation.
- Integration/E2E test cho happy path bằng staging Gateway.
- Không phụ thuộc vào master-data admin; staging phải có dữ liệu movie/cinema/hall/seat/showtime được bootstrap sẵn.

Happy-path E2E frontend tối thiểu:

```text
Movies
→ Showtimes
→ Seat map
→ Lock seat
→ Reservation
→ Create booking
→ Start payment
→ Poll booking
→ Paid
```

## 17. Definition of Done cho MVP

MVP được coi là hoàn thành khi:

- App build thành công cho Android và iOS.
- App chỉ gọi API Gateway qua một base URL.
- API client được generate từ Gateway OpenAPI.
- Người dùng hoàn thành được happy path trên staging.
- Seat conflict `409` được xử lý đúng.
- Countdown reservation không tiếp tục chạy sai sau khi app background/foreground.
- Payment command không bị gửi lặp do widget rebuild.
- Polling dừng đúng lúc và không chạy ngầm sau khi rời màn hình.
- Booking thành công hiển thị đúng booking ID, ghế và trạng thái Paid.
- Error UI có retry hợp lý và hiển thị correlation ID khi cần hỗ trợ.
- Không có token, secret hoặc dữ liệu nhạy cảm trong log.

## 18. Các giới hạn backend cần team frontend biết

- Payment hiện đang được giả lập, chưa có payment provider SDK/redirect flow.
- Authentication/authorization hiện đang comment.
- Lock nhiều ghế hiện thực hiện bằng nhiều request lock ghế đơn lẻ.
- Saga chạy bất đồng bộ; response tạo booking không đồng nghĩa toàn bộ workflow đã hoàn tất.
- Seat map phụ thuộc vào `ShowtimeCreatedIntegrationEvent` và Redis.
- API tạo showtime chỉ dùng Postman/bootstrap ở giai đoạn chưa có web admin.
- Gateway chưa hoàn thiện tại thời điểm viết tài liệu; frontend không được hard-code route cuối cùng trước khi Gateway OpenAPI được chốt.

## 19. Quyết định kỹ thuật đã chốt

- Booking client sử dụng Flutter.
- Riverpod là state-management duy nhất.
- `go_router` quản lý navigation.
- Dio/generated `dart-dio` client giao tiếp với API Gateway.
- Seat map bắt đầu với Flutter `CustomPainter` và `InteractiveViewer`.
- Polling được dùng cho booking status ở MVP; SignalR được xem xét sau.
- Web admin là một sản phẩm riêng và không ảnh hưởng lựa chọn Flutter cho booking client.

