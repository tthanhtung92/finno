# Day 5. Xử lý lỗi chuẩn: `Result<T>` + ProblemDetails + FluentValidation

> **Mentor mode.** Tài liệu giải thích *vì sao* và *làm gì*, **không kèm code C#/cấu hình**, bạn tự gõ. Mọi lệnh CLI (`dotnet`, `docker`, `git`, `curl`) thì cứ chạy theo. Mỗi file dưới đây là **một bước**, làm tuần tự từ trên xuống.
>
> Viết cho người **mới**: nếu một câu khiến bạn phải đoán, đó là lỗi của tài liệu. Nhắn mentor để bổ sung.

---

## Mục tiêu Day 5

Theo [ROADMAP](../../ROADMAP.md) (mục 5, Tuần 1, Ngày 5): *Global exception handling middleware, `Result<T>` pattern, FluentValidation cho Register → **Lỗi trả về chuẩn ProblemDetails**.*

Kết thúc Day 5 bạn có: *một cách xử lý lỗi thống nhất cho cả hệ thống.* Ba mảnh ghép vào nhau:

1. **`Result<T>`** trong `SharedKernel`: một cách biểu diễn "thành công hay thất bại" cho **lỗi nghiệp vụ được lường trước** (email trùng, không tìm thấy, sai quyền) mà **không ném exception**. Hàm trả về `Result`, caller đọc `IsSuccess` thay vì bọc `try/catch`.
2. **Global exception handler** ở host: bắt **mọi exception không lường trước** (bug, null, mất kết nối DB) và trả một body lỗi chuẩn **RFC 9457 ProblemDetails**, không rò stack trace ra client.
3. **FluentValidation** cho `/identity/register`: chặn input rác **ngay ở cửa** (email sai định dạng, mật khẩu rỗng) trước khi chạm nghiệp vụ, cũng trả ProblemDetails 400 chuẩn.

Điểm chốt của ngày: **phân biệt ba loại lỗi và xử mỗi loại đúng chỗ.** Lỗi validation (input rác) → chặn ở filter, 400. Lỗi nghiệp vụ lường trước (email trùng) → `Result` với `Error`, map sang status theo loại. Lỗi hệ thống không lường (bug) → exception handler bắt, 500 câm. Trước Day 5, endpoint `/identity/register` đang trộn cả ba bằng một `switch` tay trên enum ad-hoc; hôm nay ta dọn cho sạch.

Quỹ thời gian: ngày **nhẹ hơn Day 4**, nhưng khái niệm mới (Result pattern, IExceptionHandler, endpoint filter) đáng đọc kỹ. Chia làm 7 bước, làm chậm, verify sau mỗi bước.

> **Lưu ý phạm vi:** Day 5 **chưa** đụng Wolverine (Tuần 3) hay validation pipeline của message bus. Validation hôm nay chạy qua **endpoint filter** của minimal API, không qua mediator. Đừng kéo Wolverine vào sớm.

## Bạn cần có sẵn trước khi bắt đầu

- **Đã hoàn thành [Day 4](../day-04/README.md)**: luồng auth chạy end-to-end (`/register`, `/login`, `/refresh`, `/logout`, endpoint role); `dotnet build EventHub.slnx` xanh; host chạy được.
- **Hạ tầng đang chạy**: `docker compose --env-file .env -f docker/docker-compose.yml ps` thấy Postgres `healthy`.
- Terminal mở tại **thư mục gốc repo** (nơi có `EventHub.slnx`).
- Một công cụ gọi HTTP: file `.http` trong IDE, hoặc `curl` (bước verify dùng cả hai).
- Biết trạng thái xuất phát: endpoint `/identity/register` hiện dùng record `RegisterOutcome` + enum `RegisterFailureReason` + một `switch` tay map lỗi sang `Results.Conflict/ValidationProblem/Problem`. Day 5 sẽ **thay** cụm đó bằng `Result` + ProblemDetails.

## Các bước (làm theo thứ tự)

| Bước | File | Việc |
|------|------|------|
| A | [00-tong-quan.md](00-tong-quan.md) | **Hiểu** ba loại lỗi, vì sao dùng `Result` cho lỗi nghiệp vụ thay vì exception, ProblemDetails RFC 9457 là gì, và bản đồ ba tầng đặt code hôm nay (đọc, chưa gõ) |
| 1 | [01-result-sharedkernel.md](01-result-sharedkernel.md) | `ErrorType`, `Error`, `Result`, `Result<T>` trong `SharedKernel` (thuần, không ASP.NET) |
| 2 | [02-map-problem-details.md](02-map-problem-details.md) | `ResultExtensions` trong `Modularity`: map `Error.Type` → HTTP status + dựng ProblemDetails |
| 3 | [03-global-exception-handler.md](03-global-exception-handler.md) | `GlobalExceptionHandler : IExceptionHandler` + wiring `AddProblemDetails`/`UseExceptionHandler`/`UseStatusCodePages` ở host |
| 4 | [04-fluentvalidation-register.md](04-fluentvalidation-register.md) | Package FluentValidation, `RegisterRequestValidator`, `ValidationFilter<T>`, gắn `.AddEndpointFilter` |
| 5 | [05-refactor-register.md](05-refactor-register.md) | Refactor `/identity/register`: bỏ `RegisterOutcome`/enum ad-hoc → `Result<Guid>` + ProblemDetails |
| 6 | [06-verify-commit.md](06-verify-commit.md) | Verify e2e qua HTTP (400/409/200/500) → commit → push |

## Quy tắc kiểm chứng xuyên suốt

Sau **mỗi** bước, chạy lại lệnh kiểm chứng ở cuối file bước đó. Mỏ neo build:

```bash
dotnet build EventHub.slnx
```

Build phải xanh trước khi sang bước sau. Từ Bước 3 trở đi, mỏ neo thứ hai là **chạy host và gọi thật**:

```bash
dotnet run --project src/Bootstrap/EventHub.Api
```

## Định nghĩa "hoàn thành" Day 5

- [ ] `Result` + `Result<T>` + `Error` + `ErrorType` nằm trong `EventHub.SharedKernel`, **không** reference ASP.NET Core (project vẫn plain `Microsoft.NET.Sdk`).
- [ ] `Result` bất biến: `IsSuccess` và `Error` không thể mâu thuẫn (success mà mang `Error`, hoặc failure mà không có `Error`, phải ném ngay lúc dựng).
- [ ] Có ánh xạ `Error.Type` → HTTP status ở **một chỗ** (`ResultExtensions` trong `Modularity`): Validation → 400, NotFound → 404, Conflict → 409, Unauthorized → 401, Failure → 500.
- [ ] `GlobalExceptionHandler` bắt exception không lường → trả **ProblemDetails** (đúng `Content-Type: application/problem+json`), **không** lộ stack trace/chi tiết exception ra client.
- [ ] `AddProblemDetails()` + `AddExceptionHandler<GlobalExceptionHandler>()` + `UseExceptionHandler()` + `UseStatusCodePages()` đã wire ở host.
- [ ] `POST /identity/register` với email sai định dạng / mật khẩu rỗng → **400** body ProblemDetails có `errors` (từ FluentValidation), **không** chạm tới `IIdentityService`.
- [ ] `POST /identity/register` email trùng → **409** ProblemDetails (không còn `switch` tay trên enum ad-hoc).
- [ ] `RegisterOutcome` + `RegisterFailureReason` cũ đã **bị xóa**; `IIdentityService.RegisterUserAsync` trả `Result<Guid>` (hoặc `Result`).
- [ ] FluentValidation khai trong CPM (`FluentValidation` + `FluentValidation.DependencyInjectionExtensions`, version `12.1.1`), Apache 2.0 — **không** dính thư viện đã thương mại hóa.
- [ ] `dotnet build EventHub.slnx` xanh.
- [ ] **Bạn tự nói thành lời được:** ba loại lỗi khác nhau ở đâu và xử mỗi loại ở tầng nào; vì sao dùng exception cho luồng nghiệp vụ bình thường là **đắt và mờ ý định**; ProblemDetails RFC 9457 gồm field gì và vì sao chuẩn hóa body lỗi lại quan trọng cho client; vì sao `SharedKernel` **không được** biết ASP.NET còn mapping ProblemDetails thì phải; vì sao validation nên chặn ở filter **trước** khi vào service; vì sao exception handler phải "defensive" (không gọi DB/HTTP bên trong).

Xong Day 5, nhắn mentor **"review Day 5"** trước khi sang [Day 6–7](../README.md).
