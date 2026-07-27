
**1. DeleteMovieCommandHandlerTests**

Production

```text
Catalog.API/Application/Movies/Commands/DeleteMovie/DeleteMovieCommandHandler.cs
```

thêm test:

```text
tests/Catalog.API.UnitTests/Application/Movies/DeleteMovieCommandHandlerTests.cs
```

Test nên có:

```text
Handle_WhenMovieExists_ShouldDeleteMovie
Handle_WhenMovieDoesNotExist_ShouldReturnFalse
```

**2. GetMoviesQueryHandlerTests**

Production có query:

```text
GetMoviesQueryHandler
```

Nên thêm:

```text
tests/Catalog.API.UnitTests/Application/Movies/GetMoviesQueryHandlerTests.cs
```

Happy path nên test:

```text
Không có cache -> query DB -> trả danh sách movie theo ReleaseDate desc
Không có cache -> lưu kết quả vào cache
Có cache -> trả từ cache, không cần phụ thuộc DB nhiều
```

Phần này đáng test vì có logic pagination + cache.

**3. Bổ sung API tests còn thiếu**

Trong `CatalogApiTests.cs`

```text
CreateMovie
UpdateMovie success
UpdateMovie bad request khi route id != body id
CreateShowtime
```

Nên thêm:

```text
DeleteMovie_WhenMediatorReturnsTrue_ShouldReturnNoContent
DeleteMovie_WhenMediatorReturnsFalse_ShouldReturnNotFound
GetMovies_WhenMediatorReturnsResult_ShouldReturnOk
UpdateMovie_WhenMediatorReturnsFalse_ShouldReturnNotFound
```

Các test này  chỉ kiểm tra HTTP contract.

**4. Validator tests**

Catalog có validator:

```text
CreateMovieCommandValidator
UpdateMovieCommandValitor
DeleteMovieCommandValidator
```

Nếu bạn muốn chắc phần input validation, tạo thư mục:

```text
tests/Catalog.API.UnitTests/Application/Movies/Validators/
```

Test nên có:

```text
CreateMovieCommandValidator_WhenCommandIsValid_ShouldPass
CreateMovieCommandValidator_WhenTitleIsEmpty_ShouldFail
CreateMovieCommandValidator_WhenDurationIsZero_ShouldFail
CreateMovieCommandValidator_WhenTrailerUrlInvalid_ShouldFail

DeleteMovieCommandValidator_WhenIdIsZero_ShouldFail
```

API command thường dễ lỗi input.

**5. Negative path cho handler **

Bạn mới có happy path. Sau happy path, nên thêm vài case rẻ mà có giá trị:

```text
UpdateMovieCommandHandler_WhenMovieDoesNotExist_ShouldReturnFalse
CreateShowtimeCommandHandler_WhenHallHasNoSeats_ShouldPublishEventWithEmptySeats
```

**Nhận xét**

File này đang đặt tên thiếu chữ `l`:

```text
CreateShowtimeCommandHanderTests.cs
```

Nên đổi thành:

```text
CreateShowtimeCommandHandlerTests.cs
```

Và trong `CreateMovieCommandHandlerTests.cs` có thể đang có 2 using gần giống nhau:

```csharp
using Catalog.API.Application.Movies.Commands.CreateMovie;
using Catalog.API.Application.Moviess.Commands.CreateMovie;
```


Thứ tự làm tiếp:

```text
1. DeleteMovieCommandHandlerTests
2. API tests cho Delete + GetMovies + Update not found
3. GetMoviesQueryHandlerTests
4. Validator tests
```

Nếu chỉ “chốt happy path Catalog” thì thêm `DeleteMovieCommandHandlerTests` và `GetMoviesQueryHandlerTests` là đẹp rồi.