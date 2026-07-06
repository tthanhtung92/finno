# Bước 5: Verify end-to-end, commit & push

> Mục tiêu: chốt lại toàn bộ Day 2 chạy thông từ đầu đến cuối, rồi commit theo chuẩn dự án và push.

---

## 5.1. Cái gì

Chạy lại chuỗi kiểm chứng tổng (hạ tầng → build → migration → host nạp module), đảm bảo tất cả xanh, rồi tạo commit Conventional Commits (tiếng Việt) và push.

## 5.2. Verify end-to-end

Chạy tuần tự, mỗi lệnh phải qua mới sang lệnh sau:

```bash
# 1. Hạ tầng lên và healthy (--env-file .env vì .env ở gốc, compose ở docker/)
docker compose --env-file .env -f docker/docker-compose.yml up -d
docker compose --env-file .env -f docker/docker-compose.yml ps

# 2. Solution build sạch
dotnet build EventHub.slnx

# 3. Migration đã áp (bảng __EFMigrationsHistory tồn tại): xem lại Bước 3
dotnet ef database update --project src/Modules/Identity/EventHub.Identity.Infrastructure --startup-project src/Bootstrap/EventHub.Api

# 4. Host chạy và nạp module
dotnet run --project src/Bootstrap/EventHub.Api
```

Checklist khớp [Định nghĩa hoàn thành Day 2](README.md#định-nghĩa-hoàn-thành-day-2):

- [x] 3 service `healthy`; dữ liệu bền qua `down`/`up`.
- [x] Build xanh, không warning.
- [x] `__EFMigrationsHistory` có trong Postgres.
- [x] Endpoint của module Identity gọi được qua `UseModules()`.

## 5.3. Commit & push

Nhắc quy ước [Conventional Commits tiếng Việt](../../conventional-commits.md), imperative mood. Day 2 nên tách thành **vài commit có nghĩa** thay vì một commit khổng lồ. Gợi ý chia:

- `chore(infra): thêm docker-compose cho postgres, redis, minio`
- `build: thêm EF Core 10 + Npgsql provider vào CPM`
- `feat(identity): thêm DbContext tối thiểu và migration đầu tiên`
- `feat(bootstrap): thêm pattern IModule với AddModules/UseModules`

**Nguyên tắc chia commit đúng scope:** mỗi commit là **một thay đổi logic hoàn chỉnh, tự build được**, với `type(scope)` phản ánh đúng vùng đụng tới: `chore(infra)` cho compose, `build` cho package/CPM, `feat(identity)` cho DbContext, `feat(bootstrap)` cho pattern module. Đừng gộp bốn việc rời vào một commit "day 2" (khó review, khó revert từng phần); cũng đừng băm quá vụn (mỗi dòng một commit). Mẹo: nếu message phải dùng chữ "và" nối hai việc không liên quan → nên tách.

> **Bắt buộc trước khi `git add`, kiểm secret không lọt vào staged:** chạy `git status` và soi kỹ. `.env` (chứa mật khẩu Postgres/MinIO) và mọi file User Secrets **không được** vào git. Xác nhận `.gitignore` đã loại `.env`, `bin/`, `obj/`. User Secrets nằm ngoài cây repo (thư mục profile người dùng) nên an toàn sẵn, nhưng đừng vô tình chép connection string thật vào `appsettings.json` rồi add. Nếu lỡ stage secret, `git restore --staged <file>` gỡ ra trước khi commit.

Các lệnh:

```bash
git status
git add <đường-dẫn-cụ-thể>
git commit -m "feat(identity): thêm DbContext tối thiểu và migration đầu tiên"
git push
```

## 5.4. Cạm bẫy thường gặp

- **Lỡ commit secret:** kiểm `git status` kỹ, đảm bảo `.env` và file User Secrets không nằm trong staged. Nếu lỡ add, gỡ khỏi stage trước khi commit.
- **Commit cả thư mục `bin/`, `obj/`:** xác nhận `.gitignore` đã loại trừ.
- **Migration không commit:** file migration EF sinh ra **phải** được commit (nó là một phần schema version hóa), đừng để sót.

## 5.5. Góc kể khi phỏng vấn

*"Tôi chia commit theo scope: hạ tầng, package, DbContext, pattern module tách riêng, nên lịch sử git kể được câu chuyện thay đổi và revert được từng phần. File migration nằm trong git cùng code, nên bất kỳ ai clone về chỉ cần `database update` là dựng lại đúng schema; schema không phải thứ 'truyền miệng' hay chạy SQL tay. Và tôi kỷ luật về secret: `.env`/User Secrets không bao giờ vào repo, kiểm `git status` trước mỗi commit."*

## 5.6. Xong Day 2 khi

- [x] Toàn bộ verify 5.2 xanh.
- [x] Đã commit (chia scope hợp lý) và push; không lộ secret.
- [ ] Bạn tự kể được 4 điểm trong [Định nghĩa hoàn thành](README.md#định-nghĩa-hoàn-thành-day-2).

Xong rồi nhắn mentor **"review Day 2"** trước khi sang [Day 3](../README.md).
