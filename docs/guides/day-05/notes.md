# Ghi chú & đính chính Day 5

> Chỗ chốt những điểm *dễ hiểu mơ hồ* nhất khi dựng `Result` pattern + global exception handler + FluentValidation. Đây là các điểm "kể được khi phỏng vấn", không chỉ "chạy được". Mentor bổ sung khi review code từng bước lộ thêm chỗ mơ hồ.

---

## Ghi chú 1. Ba loại lỗi, ba cơ chế — đừng nhét hết vào một

**Hiểu sai thường gặp:** "cứ `throw` cho mọi lỗi rồi để một handler bắt hết là gọn." Gộp ba loại lỗi khác bản chất vào một cơ chế làm mất thông tin và đặt xử lý sai chỗ.

**Đúng cơ chế — ba trục:**

- **Validation (hình dạng input)** → chặn ở **endpoint filter** trước khi vào service, 400. Ví dụ: email sai định dạng, password rỗng. Đặc điểm: biết được **chỉ từ request**, không cần DB.
- **Nghiệp vụ (lường trước)** → **`Result` + `Error`**, map status theo `ErrorType`. Ví dụ: email đã tồn tại (409), không tìm thấy (404). Đặc điểm: là **kết cục hợp lệ** của nghiệp vụ, thường cần DB để biết.
- **Hệ thống (không lường)** → **exception** + `GlobalExceptionHandler`, 500 câm. Ví dụ: DB rớt, null bug. Đặc điểm: là **sự cố**, không phải nhánh logic.

Ranh giới "email trùng" là ví dụ đắt: nó **không** là validation (filter không biết email nào đã có trong DB), mà là **nghiệp vụ** → `Result`/409, không phải 400.

**Câu chốt phỏng vấn:** *"Tôi phân ba loại lỗi và xử mỗi loại ở đúng tầng: validation hình dạng chặn ở endpoint filter (400) trước khi vào service; lỗi nghiệp vụ lường trước đi qua Result và map status theo loại (email trùng → 409); còn exception không lường thì một global handler bắt và trả 500 câm. 'Email trùng' cố tình là nghiệp vụ chứ không validation vì chỉ DB mới biết."*

---

## Ghi chú 2. `Result` không thay exception — nó chia việc với exception

**Hiểu sai thường gặp:** học Result pattern xong bỏ luôn exception, `throw` bị coi là "xấu".

**Đúng cơ chế:** `Result` cho lỗi **lường trước và là kết cục hợp lệ**; exception cho **sự cố thật**. Dùng exception cho luồng nghiệp vụ bình thường thì đắt (dựng stack trace) và mờ ý định (chữ ký `Task<Guid>` nói dối là luôn trả Guid). Ngược lại, ép `Result` cho *mọi* thứ kể cả DB-rớt là vô lý — không ai `Result`-hóa được một mất kết nối bất chợt. Hai cơ chế **bổ nhau**: `Result` phủ nhánh nghiệp vụ, `GlobalExceptionHandler` phủ phần còn lại.

**Câu chốt phỏng vấn:** *"Result và exception không loại trừ nhau. Result làm chữ ký hàm trung thực về khả năng thất bại nghiệp vụ và tránh chi phí ném/bắt trên đường nóng; exception vẫn dành cho sự cố thật mà global handler bắt ở lưới cuối."*

---

## Ghi chú 3. `SharedKernel` phải mù ASP.NET — vì sao mapping ProblemDetails không nằm ở đó

**Hiểu sai thường gặp:** "cho tiện, viết luôn `result.ToProblemDetails()` ngay cạnh `Result` trong SharedKernel."

**Đúng cơ chế:** `SharedKernel` là primitive **miền** dùng bởi mọi Domain/Application của mọi module. Nếu nó biết `ProblemDetails`/status code, nó phải kéo `Microsoft.AspNetCore.App`, và tự dưng **toàn bộ tầng miền** lệ thuộc web — rò rỉ tầng nặng nhất. `Result` được biết "thất bại vì `Conflict`"; nó **không** được biết "`Conflict` = HTTP 409" (đó là kiến thức web). Mapping đặt ở `Modularity` (đã web-aware, được mọi `*.Api` reference). Bằng chứng cứng: `SharedKernel.csproj` **không** có `FrameworkReference` — giữ nguyên điều đó là giữ ranh giới.

**Câu chốt phỏng vấn:** *"Result mù HTTP: nó ở SharedKernel không có FrameworkReference ASP.NET, nên tầng miền không lệ thuộc web. Việc dịch ErrorType sang status là kiến thức tầng web, tôi để trong Modularity — nơi đã có ASP.NET và mọi module Api đều thấy. Nếu nhét mapping vào SharedKernel thì cả Domain cũng 'biết' HTTP, đó là rò tầng."*

---

## Ghi chú 4. `IExceptionHandler` thay middleware tay — và vì sao handler phải "defensive" + câm

**Hiểu sai thường gặp:** tự viết middleware `try/catch` quanh `await next()` (cách trước .NET 8), hoặc để handler gọi DB/log ra ngoài thoải mái, hoặc đổ `exception.Message` vào body cho "dễ debug".

**Đúng cơ chế — ba điểm:**

- **`IExceptionHandler` (.NET 8+/10)** là cách chính chủ: một method `TryHandleAsync`, đăng ký `AddExceptionHandler<T>` + `UseExceptionHandler()`. Không tự bọc `try/catch`, chuỗi được nhiều handler, tích hợp `IProblemDetailsService`, .NET 10 tự suppress diagnostics trùng khi trả `true`.
- **Defensive:** handler là **lưới cuối**. Nếu nó gọi DB/HTTP và ném tiếp → không còn ai đỡ, client nhận 500 trần. Chỉ log (an toàn) + ghi ProblemDetails tĩnh. Đăng ký singleton nên **không** inject dịch vụ scoped (`DbContext`) — captive dependency.
- **Câm với client, đầy đủ với server:** `exception.Message`/stack chỉ vào **log server** (để debug), body client chỉ là câu chung ("Đã có lỗi xảy ra", 500). Lộ chi tiết ra client = rò thông tin nội bộ (tên bảng, đường dẫn, thư viện). Đây là ranh giới **bảo mật**.

**Câu chốt phỏng vấn:** *"Tôi dùng IExceptionHandler thay middleware tay vì nó tích hợp IProblemDetailsService và .NET 10 tự suppress diagnostics trùng. Handler phải defensive: singleton, không đụng DbContext, không gọi gì có thể ném tiếp vì nó là lưới cuối. Và nó câm với client — chi tiết exception chỉ vào log server, body trả về là ProblemDetails chung, không lộ stack; đó là ranh giới bảo mật chứ không phải thẩm mỹ."*

---

## Ghi chú 5. `UseStatusCodePages`: vì sao chỉ `UseExceptionHandler` là chưa đủ

**Hiểu sai thường gặp:** "có `UseExceptionHandler` rồi thì mọi lỗi đều có body ProblemDetails."

**Đúng cơ chế:** `UseExceptionHandler` chỉ xử **exception**. Nhưng nhiều response lỗi **không** kèm exception: 401 do `RequireAuthorization`, 404 do không match route, 405 method sai — chúng là status "trần", body **rỗng** mặc định. `UseStatusCodePages` (kết hợp `AddProblemDetails`) khiến các status lỗi trần này cũng nhận body ProblemDetails, để client luôn thấy **một** hình dạng lỗi cho *mọi* nguồn lỗi. Hai middleware phủ hai nhóm khác nhau: exception vs status trần.

**Câu chốt phỏng vấn:** *"UseExceptionHandler chỉ bắt exception, còn 401/404 trần thì body rỗng. Tôi thêm UseStatusCodePages với AddProblemDetails để cả những status không-exception cũng có body ProblemDetails, cho client một cấu trúc lỗi duy nhất bất kể lỗi đến từ đâu."*

---

## Ghi chú 6. Validation ở filter, không ở service — và ranh giới với password policy của Identity

**Hiểu sai thường gặp:** nhét kiểm email/password vào `AuthService`/`IdentityService`, hoặc lặp toàn bộ password policy của Identity trong `RegisterRequestValidator`.

**Đúng cơ chế:**

- **Filter chặn trước service:** `ValidationFilter<T>` chạy trước handler; sai → 400, handler **không chạy** → service chỉ nhận input sạch hình dạng. Tách "luật hình dạng input" (validation) khỏi "logic nghiệp vụ" (service) — service không lẫn hai mối lo, luật validate test được riêng.
- **Ranh giới với Identity policy:** Identity **đã** có password policy chặn ở `CreateAsync`. Validator **không** nên nhân đôi toàn bộ (hai nơi luật lệch nhau). Chọn ranh: validator chặn **thô** (rỗng, độ dài tối thiểu) cho lỗi hiển nhiên + thông báo đẹp; luật sâu để Identity — và lỗi Identity trả về là lỗi **nghiệp vụ** (map qua `Result`, không phải validation). Một luật, một chủ.
- **"Email đã tồn tại" KHÔNG phải validation:** nó cần DB → là nghiệp vụ (409 qua `Result`), không đặt trong validator (validator mù DB, chỉ hình dạng).

**Câu chốt phỏng vấn:** *"Tôi validate hình dạng input ở endpoint filter nên request rác không vào tới service. Tôi cố tình không nhân đôi password policy của Identity trong validator — validator chặn thô, Identity lo luật sâu, và lỗi Identity là nghiệp vụ đi qua Result. 'Email đã tồn tại' cũng là nghiệp vụ chứ không validation vì chỉ DB mới biết, nên nó ra 409 chứ không 400."*

---

## Ghi chú 7. Map `IdentityResult` → `Error` vẫn ở Infrastructure (giữ ranh giới Day 4)

**Bối cảnh:** Day 4 đã map `IdentityResult` → enum `RegisterFailureReason` trong Infra (xem [notes Day 4, Ghi chú 7](../day-04/notes.md)). Day 5 đổi **đích** map: từ enum riêng sang `Error` chung — nhưng **giữ nguyên chỗ** map (Infra).

**Đúng cơ chế:** `IdentityResult`/`IdentityError` là type **Infrastructure**. Đọc `Code` để quyết loại lỗi phải làm trong Infra (extension `ToError` trên `IdentityResult`), trả lên Application một `Error` (miền trung tính). Nếu lỡ map ở `AuthService` (Application), Application phải thấy `IdentityResult` → phá ranh giới, đúng thứ NetArchTest sẽ fail. `Code` so bằng `nameof(IdentityErrorDescriber.DuplicateEmail)` (source of truth) + `==` ordinal, không `string.Contains`.

**Câu chốt phỏng vấn:** *"Tôi đổi đích map từ một enum riêng register sang Error chung, nhưng vẫn map trong Infrastructure vì IdentityError là type hạ tầng — Application chỉ nhận Result/Error, không bao giờ thấy IdentityError. Ranh giới này được ép bằng project reference chứ không phải quy ước."*

---

## Ghi chú 8. Bất biến trong constructor: làm trạng thái vô nghĩa không dựng được

**Hiểu sai thường gặp:** `Result` chỉ là túi dữ liệu `{ IsSuccess, Error, Value }`, ai gán gì cũng được.

**Đúng cơ chế:** một `Result` "success mà mang `Error`" hoặc "failure mà `Error` rỗng" là **trạng thái vô nghĩa** — để dựng được thì bug trốn tới tận chỗ đọc. Ép bất biến trong constructor (`protected`, ném khi mâu thuẫn) khiến sai lệch **nổ ngay dòng tạo**, và buộc caller đi qua factory `Success`/`Failure`. Tương tự, `Result<T>.Value` đọc khi `IsFailure` thì ném — bắt caller quên check `IsSuccess` thay vì trả `default(T)` âm thầm. Nguyên tắc "make illegal states unrepresentable".

**Câu chốt phỏng vấn:** *"Result của tôi ép bất biến trong constructor: success không được mang Error, failure phải có Error, sai thì ném ngay lúc dựng. Value đọc khi thất bại cũng ném. Nhờ vậy trạng thái vô nghĩa không tồn tại được và lỗi lộ tại dòng gây ra nó, không phải ba tầng sau."*
