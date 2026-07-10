# Bước 6. Verify end-to-end & commit

> Mục tiêu: chạy trọn kịch bản lỗi một lượt (400 validation → 409 nghiệp vụ → 200 thành công → 500 exception câm), chắc cả ba tuyến lỗi khớp, rồi commit theo Conventional Commits tiếng Việt.
>
> Lưu ý: chỉ lệnh CLI, cứ chạy theo.

---

## 6.1. Verify end-to-end

Build + chạy host:

```bash
dotnet build EventHub.slnx
dotnet run --project src/Bootstrap/EventHub.Api
```

Gợi ý: gom vào một file `.http` tạm (đặt ở `src/Bootstrap/EventHub.Api`) để chạy tuần tự trong IDE. Dưới đây dùng `curl` cho rõ từng tuyến (thay `5xxx` bằng port thật):

```bash
# --- Tuyến 1: VALIDATION (400) — chặn ở ValidationFilter, chưa vào service ---
curl -i -X POST http://localhost:5xxx/identity/register \
  -H "Content-Type: application/json" \
  -d '{"email":"khong-phai-email","password":""}'

# --- Tuyến 2: NGHIỆP VỤ (409) — email trùng, qua Result -> ProblemDetails ---
curl -i -X POST http://localhost:5xxx/identity/register \
  -H "Content-Type: application/json" \
  -d '{"email":"day5@eventhub.local","password":"Passw0rd!"}'
# lần 2 cùng email:
curl -i -X POST http://localhost:5xxx/identity/register \
  -H "Content-Type: application/json" \
  -d '{"email":"day5@eventhub.local","password":"Passw0rd!"}'

# --- Tuyến 3: HỆ THỐNG (500 câm) — cần một endpoint ném thử tạm (xem 6.2) ---
curl -i http://localhost:5xxx/debug/boom
```

Đối chiếu kỳ vọng:

| Tuyến | Request | Status | Content-Type | Body |
|---|---|---|---|---|
| 1. Validation | email sai + password rỗng | **400** | `application/problem+json` | có field `errors` (email, password) từ FluentValidation |
| 2a. Nghiệp vụ (lần 1) | email mới, hợp lệ | **200/201** | `application/json` | có `userId` |
| 2b. Nghiệp vụ (trùng) | cùng email | **409** | `application/problem+json` | `detail` báo email đã đăng ký; `errorCode` = `Identity.DuplicateEmail` |
| 3. Hệ thống | endpoint ném thử | **500** | `application/problem+json` | **câm**: không có message/stack; log server **có** chi tiết |

Ba điều then chốt phải thấy:

- **Tuyến 1 không chạm service.** Nếu bạn tạm log trong `IdentityService.RegisterUserAsync`, request tuyến 1 **không** kích log đó — nó bị filter chặn trước.
- **Tuyến 3 câm.** Body 500 **không** chứa chuỗi bạn ném (vd `"boom"`), không tên type, không stack. Cửa sổ chạy host thì **có** log đầy đủ.
- **Một hình dạng lỗi.** Cả 400/409/500 đều là `application/problem+json` — client đọc cùng một cấu trúc.

## 6.2. Endpoint ném thử (tạm, xóa trước khi commit)

Tuyến 3 cần một chỗ ném exception không lường. Tạm thêm một route trong `Program.cs` hoặc một module, vd `GET /debug/boom` ném `throw new InvalidOperationException("boom")`. Sau khi xác nhận 500 câm, **xóa route này** — nó không thuộc sản phẩm.

> Nếu ngại thêm route: tạm sửa một endpoint thật để ném một lần, kiểm, rồi hoàn tác. Miễn **không** commit code ném thử.

## 6.3. Định nghĩa "hoàn thành" Day 5

Đối chiếu checklist ở [README Day 5](README.md#định-nghĩa-hoàn-thành-day-5). Cốt lõi:

- [x] `Result`/`Result<T>`/`Error`/`ErrorType` ở `SharedKernel` (thuần, không ASP.NET); bất biến ép trong constructor.
- [x] Mapping `Error.Type` → status ở `Modularity` (một chỗ); `ToProblemDetails`/`Match`.
- [x] `GlobalExceptionHandler` trả 500 ProblemDetails câm; `AddProblemDetails`/`UseExceptionHandler`/`UseStatusCodePages` đã wire.
- [x] `RegisterRequestValidator` + `ValidationFilter<T>` chặn input rác 400 trước service.
- [x] Register trùng email → 409 qua `Result`; `RegisterOutcome`/`RegisterFailureReason` đã xóa.
- [x] FluentValidation `12.1.1` (Apache 2.0) trong CPM.
- [x] Build xanh; bạn tự giải thích được ba tuyến lỗi.

## 6.4. Commit

Conventional Commits, tiếng Việt, imperative (xem [docs/conventional-commits.md](../../conventional-commits.md)). Day 5 chạm nhiều scope (shared kernel, modularity, host, identity) — dùng scope rộng hoặc tách vài commit theo mảnh.

```bash
git add -A
git status
git commit
```

**Khuyến nghị message** (một commit gộp cho cả ngày):

```text
feat(errors): thêm Result pattern + ProblemDetails + FluentValidation cho register
```

Nếu thích commit nhỏ theo bước (**lựa chọn phong cách của bạn**, mentor khuyến nghị gộp vì cả ngày là một đơn vị "chuẩn hóa xử lý lỗi"):

```text
feat(shared): thêm Result/Result<T>/Error/ErrorType vào SharedKernel
feat(shared): map Result sang ProblemDetails trong Modularity
feat(api): thêm GlobalExceptionHandler trả ProblemDetails cho lỗi không lường
feat(identity): validate register bằng FluentValidation qua endpoint filter
refactor(identity): chuyển register từ RegisterOutcome sang Result<Guid>
```

Đẩy lên remote nếu muốn:

```bash
git push
```

## 6.5. Cạm bẫy thường gặp

- **Lỡ commit endpoint ném thử.** `/debug/boom` (6.2) phải **xóa** trước commit. `git status`/`git diff` rà trước.
- **Quên commit thay đổi ở nhiều project.** Day 5 chạm `SharedKernel`, `Modularity`, `Bootstrap`, `Identity.*`, và `Directory.Packages.props`. `git add -A` rồi soi `git status` để không sót project nào.
- **Migration lỡ sinh ra.** Day 5 **không** đổi schema DB. Nếu `dotnet ef migrations add` chạy nhầm, xóa migration thừa trước khi commit.
- **File `.http` tạm chứa mật khẩu thật.** Xóa hoặc để placeholder trước commit.

## 6.6. Xong Day 5 khi

- [x] Ba tuyến lỗi (400/409/500) đúng kỳ vọng ở 6.1; body 500 câm; validation chặn trước service.
- [x] Endpoint ném thử đã xóa; grep `RegisterOutcome` = 0.
- [ ] Đã commit (và push nếu muốn) với message Conventional Commits tiếng Việt.
- [ ] Nhắn mentor **"review Day 5"**.

→ Quay lại [README Day 5](README.md) hoặc xem trước [Day 6–7: Module Events](../README.md).
