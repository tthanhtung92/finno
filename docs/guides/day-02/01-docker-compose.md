# Bước 1: Docker Compose cho Postgres, Redis, MinIO

> Mục tiêu: tạo `docker/docker-compose.yml` mô tả ba dịch vụ hạ tầng, sao cho một lệnh `up` là dựng xong, dữ liệu bền qua restart.
>
> Lưu ý mentor: file này là YAML cấu hình, **mình không viết hộ**. Mình mô tả từng phần cần có bằng lời; bạn tự gõ. Đọc kỹ phần "Cấu trúc cần có".

---

## 1.1. Cái gì

Tạo thư mục `docker/` ở gốc repo và file `docker/docker-compose.yml` định nghĩa **3 service**: `postgres`, `redis`, `minio`. Mỗi service khai: image, port mở ra host, biến môi trường (mật khẩu...), named volume để giữ dữ liệu, và healthcheck.

## 1.2. Vì sao

**Named volume, không phải bind mount, cho dữ liệu DB.** Có hai cách gắn lưu trữ bền: *bind mount* (trỏ thẳng vào một thư mục trên máy host, vd `./data:/var/lib/postgresql/data`) và *named volume* (vùng do Docker quản lý, đặt tên, vd `pgdata:/var/lib/postgresql/data`). Với dữ liệu DB nên dùng **named volume** vì Docker quản lý vòng đời và quyền (uid/gid) đúng cho image, trong khi bind mount hay dính lỗi permission và khác biệt filesystem giữa Windows/macOS/Linux; named volume không phụ thuộc đường dẫn tuyệt đối trên máy nên **portable** giữa các máy; và nó tách hẳn khỏi cây source (không lỡ commit data lên git). Bind mount hợp cho *source code* cần sửa-thấy-ngay, không hợp cho *dữ liệu nhị phân của DB*.

**Healthcheck: vì "container đã start" ≠ "service đã sẵn sàng".** Postgres mất vài giây sau khi container lên mới thật sự nhận kết nối. `healthcheck` cho Docker một lệnh để hỏi "khỏe chưa?" (`pg_isready` với Postgres, `redis-cli ping` với Redis, endpoint `/minio/health/live` với MinIO) và chỉ đánh dấu `healthy` khi lệnh đó pass. Nhờ vậy bạn (hoặc CI, hoặc một service phụ thuộc khai `depends_on: condition: service_healthy`) biết chờ đến khi DB **thật sự** phục vụ được, tránh lỗi "connection refused" do nối quá sớm.

**Tách `docker-compose.yml` khỏi `docker-compose.override.yml` (tùy chọn, chưa dùng ở đây).** Ý tưởng: file `docker-compose.yml` giữ cấu hình **chung** (đúng cho mọi môi trường); `docker-compose.override.yml` giữ phần **riêng cho dev** (mở thêm port debug, mount source, bật log verbose...). Docker Compose **tự động** merge `override` lên `base` khi bạn chạy `docker compose up` ở cùng thư mục, nên dev có thêm tùy biến mà production (chỉ dùng file base) không bị ảnh hưởng. Hiện project để **một file duy nhất** cho gọn; khi cần tách cấu hình prod/dev thì thêm `override` sau (ROADMAP mục 3 phác sẵn hướng này).

## 1.3. Các bước làm

Mô tả bằng lời (bạn tự gõ YAML):

1. Tạo thư mục `docker/` ở **gốc repo** (ngang `EventHub.slnx`).
2. Trong `docker/docker-compose.yml`, khai một khối `services` gồm ba mục con (các tên biến/đường dẫn/healthcheck dưới đây đã đối chiếu tài liệu chính thức: xem Link):
   - **postgres**: image `postgres:17`; biến môi trường `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`; map port `5432`; gắn named volume vào **`/var/lib/postgresql/data`** (thư mục dữ liệu của Postgres) để **không mất dữ liệu** khi container bị xóa; healthcheck dạng `["CMD-SHELL", "pg_isready -U <user> -d <db>"]` (kèm `interval`, `timeout`, `retries`, `start_period`).
   - **redis**: image `redis:8`; map port `6379`; (tùy chọn) volume `/data` cho persistence; healthcheck `["CMD", "redis-cli", "ping"]` (phản hồi `PONG` khi khỏe).
   - **minio**: image `minio/minio`; chạy lệnh `server /data --console-address ":9001"`; biến môi trường `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`; map **hai** port: `9000` (API S3) và `9001` (console UI); volume vào **`/data`**; healthcheck gọi endpoint **`/minio/health/live`** (vd `["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]`).
3. Khai khối `volumes` ở cấp trên cùng để đăng ký các named volume đã dùng.
4. (Khuyến nghị) Đặt **mật khẩu/secret** qua file `.env` cạnh compose thay vì hardcode; thêm `.env` vào `.gitignore` nếu chứa secret thật.

> **Lưu ý healthcheck MinIO:** endpoint `/minio/health/live` là chính thức, nhưng image `minio/minio` **có thể không kèm sẵn `curl`**. Nếu healthcheck báo "unhealthy" dù service chạy, kiểm tra bằng `docker compose exec minio sh -c "command -v curl"`; nếu thiếu, dùng công cụ có sẵn trong image (vd `mc ready local`), xác nhận lại trong [MinIO docs](https://min.io/docs/minio/container/operations/checklists/healthcheck.html) cho đúng tag bạn dùng.

## 1.4. Kiểm chứng

Tại gốc repo:

```bash
docker compose --env-file .env -f docker/docker-compose.yml up -d
docker compose --env-file .env -f docker/docker-compose.yml ps
```

> **Bắt buộc `--env-file .env`:** `.env` ở gốc repo, compose ở `docker/`; Compose lấy thư mục chứa `-f` làm project dir nên không tự nạp `.env` ở gốc. Thiếu cờ này thì `${POSTGRES_USER}`… rỗng → Postgres dựng lên với user/mật khẩu rỗng (hỏng). Kiểm nhanh: `docker compose --env-file .env -f docker/docker-compose.yml config` xem giá trị đã được thay chưa. *(Cách khác: khai `env_file:` trong từng service để compose tự chứa.)*

- Cả ba service phải lên; cột trạng thái chuyển sang `healthy` sau vài giây (chờ healthcheck).
- Mở MinIO console trên trình duyệt (port console đã map) đăng nhập bằng root user/password, vào được là MinIO ổn.
- Kiểm tra dữ liệu bền:

```bash
docker compose --env-file .env -f docker/docker-compose.yml down
docker compose --env-file .env -f docker/docker-compose.yml up -d
```

`down` (không kèm `-v`) rồi `up` lại, dữ liệu vẫn còn nhờ named volume.

## 1.5. Cạm bẫy thường gặp

- **Trùng port:** nếu máy đã có Postgres/Redis chạy sẵn ở `5432`/`6379`, `up` sẽ lỗi "port is already allocated". Đổi port phía host (vd map `5433:5432`) hoặc tắt service đang chiếm.
- **`down -v` xóa sạch dữ liệu:** cờ `-v` xóa luôn named volume. Chỉ dùng khi cố ý reset.
- **Healthcheck sai → service "mãi không healthy":** sai lệnh hoặc thiếu công cụ trong image khiến healthcheck luôn fail. Kiểm bằng `docker compose logs <service>`.
- **Secret commit nhầm lên git:** đừng hardcode mật khẩu thật rồi push. Dùng `.env` + `.gitignore`.

## 1.6. Góc kể khi phỏng vấn

*"Toàn bộ hạ tầng dev, gồm Postgres, Redis, MinIO, tôi khai trong một docker-compose: một lệnh `up` là dựng lại y hệt trên mọi máy và CI, không phụ thuộc 'máy tôi'. Dữ liệu bền qua named volume (Docker quản lý, portable, không dính lỗi permission như bind mount). Mỗi service có healthcheck riêng (`pg_isready`, `redis-cli ping`, endpoint health của MinIO) để biết service **thật sự sẵn sàng** chứ không chỉ 'container đã start', nền tảng để về sau service phụ thuộc chờ đúng bằng `depends_on: service_healthy`."*

## 1.7. Link tài liệu chính thức

- [Compose file reference: Docker Docs](https://docs.docker.com/reference/compose-file/)
- [Healthcheck trong Compose](https://docs.docker.com/reference/compose-file/services/#healthcheck)
- [Postgres image: Docker Hub](https://hub.docker.com/_/postgres) · [Redis image](https://hub.docker.com/_/redis) · [MinIO docs](https://min.io/docs/minio/container/index.html)

## 1.8. Xong bước này khi

- [x] `docker/docker-compose.yml` tồn tại, khai đủ 3 service + khối `volumes`.
- [x] `up -d` → cả 3 service `healthy`.
- [x] `down` rồi `up` lại không mất dữ liệu.

→ Sang [Bước 2: EF Core packages](02-ef-core-packages.md).
