# Kiến trúc Backend — LabBooking

**Đề tài 03 — Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung**

Tài liệu mô tả kiến trúc Clean Architecture của backend, cách các tầng kết nối, luồng xử lý một request, cơ chế DI, các background service và mô hình phát hiện xung đột lịch.

## 1. Tổng quan phân tầng

```
                    ┌────────────────────────┐
                    │  LabBooking.API        │  Controller, Filter, ExceptionHandler,
                    │  (Presentation)        │  CurrentUser, BackgroundService
                    └──────────┬─────────────┘
                               │ gọi MediatR (ISender)
                    ┌──────────▼─────────────┐
                    │  LabBooking.Application│  Command/Query + Handler
                    │  (Use-case/Không phụ    │  Contracts (DTO), ICurrentUser
                    │   thuộc framework)      │  Exception riêng
                    └──────────┬─────────────┘
              ┌────────────────┴────────────────┐
              │ gọi interface (IRepository,      │
              │ IUnitOfWork, ICurrentUser)       │
     ┌────────▼────────┐               ┌─────────▼─────────┐
     │ LabBooking.Domain│               │ Infrastructure.Sqlserver│
     │ Entities, Enums, │  implements ▶│ EF Core: DbContext,     │
     │ Interfaces,      │              │ Repository<T>,          │
     │ Scheduling (thuần)│             │ TokenService, DataSeeder│
     └─────────────────┘               └───────────────────────┘
```

**Quy tắc phụ thuộc (Dependency Rule):** mã nguồn luôn trỏ VÀO TRONG — Application/Infrastructure/API phụ thuộc Domain, không bao giờ ngược lại. Application và API **không tham chiếu package EF Core** hay SQL Server; Infrastructure nằm sau interface của Domain nên có thể thay bằng một provider khác mà không đụng tầng trên.

## 2. Trách nhiệm từng tầng

### 2.1. Domain
Không phụ thuộc framework, không tham chiếu package ngoài.

- `Entities/` — 13 thực thể nghiệp vụ (BaseEntity kế thừa `Id`, `CreatedAt`, `UpdatedAt`; navigation dùng cho EF).
- `Enums/` — `BookingStatus`, `UserRole`, `UserStatus`, `ResourceType`, `ResourceStatus`, `MaintenanceStatus`, `IncidentStatus`, `ViolationType`, `WaitlistStatus`, `NotificationType`.
- `Interfaces/` — hợp đồng: `IRepository<T>`, `IUnitOfWork`, `ITokenService`.
- `Scheduling/Scheduling.cs` — **logic thuần** tính khung giờ (không biết EF/DB) để kiểm thử độc lập:
  - `IsOverlap(aStart, aEnd, bStart, bEnd)` — hai khoảng chồng lấn.
  - `Merge(intervals)` — gộp các khoảng liền kề/giao nhau thành các khoảng bận rồi gộp.
  - `FreeGaps(windowStart, windowEnd, busy, openFrom, openUntil)` — tính khoảng trống sau khi khấu trừ khoảng bận, giới hạn theo **giờ hoạt động mỗi ngày** (07:00–22:00).
  - `SuggestSlots(gaps, requestedStart, duration, count=3)` — chọn tối đa `count` khung có độ dài `duration` nằm trọn trong khoảng trống, ưu tiên gần thời điểm yêu cầu nhất.

### 2.2. Application
Tầng use-case. Mỗi chức năng nghiệp vụ là một `Command`/`Query` (MediatR) + `Handler` trong `Features/<Module>/`. Handler chỉ làm việc với interface của Domain.

- Không trả về/thao tác trực tiếp entity cho client: dùng `Contracts/` (record DTO).
- Các kiểu lỗi dùng chung trong `Common/Exceptions/`:
  - `NotFoundException` → HTTP 404
  - `ConflictException` (kèm `Payload` để mang khung thay thế) → HTTP 409
  - `UnauthorizedException` → HTTP 401
  - `ArgumentException` (nghiệp vụ dữ liệu đầu vào sai) → HTTP 400
- `ICurrentUser` (Interface) — đọc user hiện tại từ token; hiện thực ở tầng API.

### 2.3. Infrastructure.Sqlserver
Hiện thực các interface của Domain.

- `Persistence/ApplicationDbContext` — đồng thời đóng vai `IUnitOfWork`.
- `Persistence/Repository<T>` — hiện thực `IRepository<T>` dùng EF Core; tự động lọc soft-delete (`HasQueryFilter`), dùng `AsNoTracking()` cho truy vấn đọc.
- `Configurations/*` — Fluent API: table, index, check constraint, quan hệ, query filter, `DeleteBehavior.Restrict` để tránh xoá lan toả.
- `Auth/TokenService` — tạo JWT (access) + refresh token.
- `Persistence/DataSeeder` — migrate + seed dữ liệu mẫu khi khởi động.
- Migrations EF Core (4 bản).

### 2.4. API
Presentation layer.

- Controllers mỏng: nhận request → bọc vào Command/Query → `_sender.Send(...)` → trả dữ liệu thô.
- `Common/ApiResponseWrapperFilter` — tự bọc MỌI kết quả controller vào envelope `ApiResponse { statusCode, isSuccess, errorMessages, result }`.
- `Common/GlobalExceptionHandler` — chặn exception chưa xử lý, ánh xạ đúng HTTP status + envelope (giấu chi tiết lỗi ngoài dự kiến, chỉ log).
- `Common/CurrentUserService` — hiện thực `ICurrentUser` đọc claims từ principal.
- `Common/*Service` — 3 `BackgroundService` (xem mục 5).

## 3. Đăng ký DI

Mỗi tầng có một `DependencyInjection` tĩnh; `Program.cs` gọi lần lượt:

```csharp
builder.Services.AddPresentation();        // controllers + filter + exception handler + Mapster
builder.Services.AddApplication();         // MediatR (quét toàn bộ assembly) + Mapster
builder.Services.AddInfrastructureSqlServer(builder.Configuration); // DbContext, Repository<>, IUnitOfWork, TokenService

builder.Services.AddAuthentication(JwtBearer)  // đọc Jwt:Key/Issuer/Audience từ config
builder.Services.AddAuthorization();

builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<BookingReminderService>();
builder.Services.AddHostedService<ViolationSweepService>();
```

**Mapster:** cả Application (`Mappings/ApplicationMappingRegister`) và API (`Mappings/ApiMappingRegister`) quét assembly và nạp cấu hình ánh xạ DTO vào lúc khởi động.

## 4. Luồng xử lý một request (Ví dụ: Đặt lịch)

```
Client
  │  POST /api/bookings  (JWT Bearer)
  ▼
BookingsController.Create
  │  JwtBearer Middleware xác thực                                     (Program.cs)
  │  ApiResponseWrapperFilter bọc kết quả khi đi ra                     (filter)
  ▼
_sender.Send(CreateBookingCommand)                                     (MediatR)
  ▼
CreateBookingCommandHandler.Handle
  1. Kiểm tra EndTime > StartTime; StartTime trong tương lai
  2. GetByIdAsync(Resource), kiểm tra PriorityRule nếu truyền
  3. _currentUser.UserId (từ token) — phải đăng nhập
  4. Kiểm tra Restriction đang hiệu lực cho requester → 400 nếu bị hạn chế
  5. Lấy booking (Pending/Approved) + maintenance cùng resource
  6. BookingEvaluation.Overlapping / maintenance overlap
     ├─ xung đột? → ném ConflictException("...", BookingConflictResponse
     │             chứa danh sách booking xung đột + 3 khung thay thế)  → HTTP 409
     └─ không?    → tạo Booking, AddAsync, uow.SaveChangesAsync
  7. Trả BookingDto (map qua BookingEvaluation.ToDto)
  ▼
ApiResponseWrapperFilter  → OK 200 { statusCode, isSuccess, errorMessages: [], result: {...} }
```

Hai điểm nhấn đáng chú ý:

- Controller không dựng ApiResponse thủ công — class filter lo việc đó.
- Lỗi nghiệp vụ được ném từ Handler (exception) thay vì dùng Result object, giữ Handler linear; `GlobalExceptionHandler` chặn phần lỗi, filter lo phần thành công.

## 5. Background Service

| Service | Chu kỳ (config) | Việc làm |
|---------|-----------------|----------|
| `RefreshTokenCleanupService` | `RefreshTokenCleanup:IntervalHours` | Xoá refresh token hết hạn/đã revoke (`ExecuteDeleteAsync`). |
| `BookingReminderService` | `Notification:ReminderIntervalMinutes` | Mỗi chu kỳ tìm booking Approved sắp bắt đầu (trong `Notification:ReminderHours`), tạo thông báo `BookingReminder`; chống trùng bằng prefix `BookingReminder:{id}:` trong nội dung. |
| `ViolationSweepService` | `Violation:SweepIntervalMinutes` | Gọi `ViolationSweeper.SweepAsync`: (1) ghi no-show cho booking Approved đã qua `end_time + NoShowGrace` mà không check-in; (2) nếu số vi phạm trong cửa sổ ≥ ngưỡng thì tự tạo Restriction; (3) đồng bộ `User.Status` với Restriction đang hiệu lực. Unique index `(BookingId, Type)` trên Violations chặn bản ghi trùng khi 2 instance cùng quét. |

Các service dùng `IServiceScopeFactory` để tạo scope mới mỗi chu kỳ (tránh phụ thuộc scope của request).

## 6. Mô hình xung đột & đề xuất khung thay thế

BookingEvaluation (Application) phối hợp với Scheduling (Domain):

- **Booked range:** booking ở trạng thái `Pending` hoặc `Approved` (đang "giữ khung"); maintenance trạng thái ≠ `Completed`.
- `BlockedRanges(start, end, bookings, maintenances)` — lấy toàn bộ khoảng bận trong cửa sổ `[start-3 ngày, end+3 ngày]`.
- `SuggestAlternatives(...)` — dùng `Scheduling.FreeGaps` + `SuggestSlots` trong khoảng ±3 ngày của khung yêu cầu, giới hạn 07:00–22:00, trả về **tối đa 3 slot** cùng độ dài, sắp theo khoảng cách tuyệt đối tới khung yêu cầu.
- Ràng buộc tầng DB (backstop cho đa instance): trigger `TR_Bookings_BlockOverlap` (chặn booking^booking, booking^maintenance) và `TR_Maintenances_BlockOverlap` (chặn maintenance^maintenance, maintenance^booking) trong migration `20260811021109_AddOverlapTriggers`; `SqlException` 50001/50002/51001/51002 → `409 Conflict`.

## 7. Xác thực & refresh token

1. **Đăng ký** (`Register`): kiểm tra email trùng, băm mật khẩu bằng BCrypt, tạo user `Requester`, trả `UserDto`.
2. **Đăng nhập** (`Login`): xác nhận email + BCrypt.Verify → `TokenService.GenerateAsync` tạo cặp token → lưu refresh token vào DB → trả `AuthResponse { AccessToken, RefreshToken, ExpiresIn, User }`.
3. **Refresh** (`Refresh`): kiểm tra token tồn tại, chưa revoke, chưa hết hạn → **revoke token cũ** (rotate) → cấp cặp token mới.
4. **Logout**: revoke refresh token hiện tại.

Claims của access token: `sub`, `name`, `email`, `role`, `jti`. `CurrentUserService` đọc `sub`/`NameIdentifier` → `UserId` và `role` → `Role`.

## 8. Phân quyền

- Cấp controller: `[Authorize]` / `[Authorize(Roles="...")]`.
- Cấp handler: kiểm tra nghiệp vụ sâu hơn (VD Lab Manager chỉ duyệt được booking trên resource `LabManagerId` của mình; Requester chỉ huỷ lịch của mình; chỉ Admin tạo Restriction...).
- Scope dữ liệu trả về: các Query handler lọc theo vai trò trước khi trả (GetBookings, GetViolations, GetIncidents...).

Chi tiết endpoint: xem [`docs/API.md`](API.md).