# LabBooking — Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung

**Đề tài 03** — Backend `.NET` theo Kiến trúc Sạch (Clean Architecture) với 4 tầng: `Domain`, `Application`, `Infrastructure.Sqlserver`, `API`.

Hệ thống quản lý toàn bộ vòng đời phòng/thiết bị dùng chung: danh mục tài nguyên, đặt lịch, phát hiện xung đột, duyệt theo quy tắc ưu tiên, check-in/check-out, sự cố, bảo trì, vi phạm, hàng đợi chờ và dashboard báo cáo.

## Mục lục

- [Tính năng](#tính-năng)
- [Công nghệ & thư viện](#công-nghệ--thư-luận)
- [Cấu trúc project](#cấu-trúc-project)
- [Chạy nhanh](#chạy-nhanh)
- [Cấu hình](#cấu-hình)
- [Dữ liệu seed](#dữ-liệu-seed)
- [Ảnh xạ user story → code](#ảnh-xạ-user-story--code)
- [Xác thực & phân quyền](#xác-thực--phân-quyền)
- [Luồng nghiệp vụ chính](#luồng-nghiệp-vụ-chính)
- [Kiểm thử](#kiểm-thử)
- [Tài liệu](#tài-liệu)

## Tính năng

| Vùng | Phát hành | Endpoint chính |
|------|-----------|----------------|
| Xác thực JWT (access + refresh, rotate, revoke) | Auth | `POST /api/auth/*` |
| CRUD phòng/thiết bị (soft-delete) | Resources | `GET/POST/PUT/DELETE /api/resources` |
| Lịch khả dụng theo ngày/tuần/tháng | Availability | `GET /api/resources/{id}/availability` |
| Đặt lịch + tự động phát hiện xung đột + đề xuất khung thay thế | Booking | `POST /api/bookings` |
| Kiểm tra xung đột trước khi đặt | Booking | `POST /api/bookings/check-conflict` |
| Duyệt/từ chối lịch theo quy tắc ưu tiên (nghiên cứu > môn học > tự học) | Approval | `POST /api/bookings/{id}/approve` |
| Huỷ lịch theo chính sách thời hạn, trả khung giờ cho waitlist | Booking | `POST /api/bookings/{id}/cancel` |
| Check-in / check-out, tự ghi vi phạm trả trễ | CheckInOut | `POST /api/bookings/{id}/checkin` |
| Báo cáo sự cố thiết bị, tự thông báo Lab Manager | Incident | `GET/POST /api/incidents` |
| Lập lịch bảo trì (khoá khung giờ), hoàn tất + chi phí | Maintenance | `GET/POST /api/maintenances` |
| Hàng đợi chờ (waitlist) khi kín khung giờ | Waitlist | `GET/POST/DELETE /api/waitlists` |
| Ghi nhận vi phạm no-show + tự hạn chế quyền đặt lịch khi quá ngưỡng | Violation | `GET /api/violations` |
| Hạn chế quyền đặt lịch (thủ công/Admin) | Restriction | `GET/POST/DELETE /api/restrictions` |
| Thông báo in-app (nhắc lịch, duyệt lịch, waitlist, sự cố) | Notification | `GET/PUT /api/notifications` |
| Dashboard tỷ lệ sử dụng + báo cáo bảo trì & chi phí | Dashboard | `GET /api/dashboard/*` |
| Quy tắc ưu tiên đặt lịch | PriorityRule | `GET/POST/PUT/DELETE /api/priority-rules` |

## Công nghệ & thư viện

- **.NET 10** (target framework `net10.0`)
- **ASP.NET Core Web API** + **OpenAPI / Swagger**
- **Entity Framework Core 10** + SQL Server (LocalDB)
- **MediatR** (14.x) — CQRS: mỗi use-case là một `Command`/`Query` + `Handler`
- **Mapster** (10.x) — ánh xạ DTO
- **BCrypt.Net-Next** — băm mật khẩu
- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **xUnit** — kiểm thử đơn vị

## Cấu trúc project

Tạo bởi giải pháp khai báo mới `LabBooking.slnx` (giải pháp XML không có `.sln`). Tuân thủ quy tắc phụ thuộc: `API → Application → Domain` và `Infrastructure → Domain`; **Application không tham chiếu Infrastructure**.

```
LabBooking.slnx
├── LabBooking.Domain                       # Lõi: không phụ thuộc gì
│   ├── Common/          BaseEntity, PagedResult
│   ├── Entities/        13 thực thể nghiệp vụ
│   ├── Enums/           BookingStatus, UserRole, ViolationType, ...
│   ├── Interfaces/      IRepository<T>, IUnitOfWork, ITokenService
│   └── Scheduling/      Logic thuần khung giờ (overlap, merge, gợi ý)
├── LabBooking.Application                   # Use-case (CQRS qua MediatR)
│   ├── Common/          ICurrentUser, ConflictException, NotFoundException, ...
│   ├── Contracts/       DTO giao tiếp
│   ├── Features/        Auth, Bookings, Dashboard, Departments, Incidents,
│   │                    Maintenances, Notifications, PriorityRules, Resources,
│   │                    Restrictions, Violations, Waitlists
│   └── Mappings/        Mapster config
├── LabBooking.Infrastructure.Sqlserver      # Hiện thực EF Core
│   ├── Auth/            TokenService (JWT + refresh)
│   ├── Configurations/  Fluent API mỗi entity một file
│   ├── Persistence/     ApplicationDbContext, Repository<T>, DataSeeder, Migrations
│   └── DependencyInjection.cs
├── LabBooking.API                           # Presentation
│   ├── Common/          ApiResponseWrapperFilter, GlobalExceptionHandler,
│   │                    CurrentUserService, BackgroundService* (3)
│   ├── Controllers/     14 controller
│   ├── Models/          ApiResponse (envelope), PaginationRequest
│   ├── Mappings/        Mapster config
│   └── Program.cs
└── LabBooking.Tests                         # xUnit, dùng fakes thay DB
```

## Chạy nhanh

Yêu cầu: .NET SDK 10, SQL Server LocalDB (đi kèm Visual Studio).

```bash
# Khởi tạo + chạy (migrate + seed tự động khi khởi động)
dotnet run --project LabBooking.API

# Chạy kiểm thử
dotnet test LabBooking.Tests

# Build toàn giải pháp
dotnet build LabBooking.slnx
```

Swagger UI khi chạy ở môi trường `Development`: `https://localhost:<port>/swagger/` (hỗ trợ nút **Authorize** Bearer token).

Cơ sở dữ liệu `LabBookingDb` (LocalDB) được tạo tự động ở lần chạy đầu tiên: `DataSeeder` gọi `Database.MigrateAsync()` rồi chèn dữ liệu mẫu nếu bảng `Departments` rỗng.

## Cấu hình

Tất cả cấu hình đọc từ `appsettings.json`. Đọc thêm các nút:

| Nhánh | Mục đích | Giá trị mặc định |
|-------|----------|------------------|
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối LocalDB | `Server=(localdb)\mssqllocaldb;Database=LabBookingDb;...` |
| `Jwt:Key` | Khoá HMAC-SHA256 ký token | `super_secret_development_key_!ChangeMe` — **đổi khi deploy** |
| `Jwt:Issuer` / `Jwt:Audience` | Issuer/Audience của token | `LabBooking.API` / `LabBookingClients` |
| `Jwt:ExpiryMinutes` | Tuổi thọ access token | `60` |
| `Jwt:RefreshExpiryDays` | Tuổi thọ refresh token | `7` |
| `Booking:CancellationDeadlineHours` | Huỷ lịch trước giờ sử dụng | `2` |
| `Violation:LateGraceMinutes` | Check-out muộn quá ngưỡng → vi phạm Late | `15` |
| `Violation:NoShowGraceMinutes` | Không check-in quá ngưỡng sau giờ kết thúc → no-show | `30` |
| `Violation:Threshold` | Số vi phạm trong cửa sổ → tự hạn chế | `3` |
| `Violation:WindowDays` | Cửa sổ đếm vi phạm | `30` |
| `Violation:RestrictionDays` | Số ngày hạn chế tự động | `7` |
| `Violation:SweepIntervalMinutes` | Chu kỳ quét no-show (background job) | `5` |
| `Notification:ReminderHours` | Nhắc lịch trước giờ bắt đầu | `1` |
| `Notification:ReminderIntervalMinutes` | Chu kỳ quét nhắc lịch | `5` |
| `RefreshTokenCleanup:IntervalHours` | Dọn refresh token đã hết hạn/thu hồi | `1` |

## Dữ liệu seed

`DataSeeder` chèn khi DB rỗng:

- **3 Khoa**: Computer Science, Electronics, Mechanical
- **8 người dùng** (2 Admin, 3 Lab Manager, 3 Requester) — dùng chung mật khẩu phát triển **`ChangeMe123!`**, đăng nhập được đầy đủ vai trò.
- **3 quy tắc ưu tiên**: Research Project (1), Course (2), Self-study (3).
- **3 tài nguyên**: CS Lab A, Oscilloscope, Mech Workshop.
- Booking (Approved/Pending), CheckInOut, Incident, Maintenance, Violation, Restriction, Waitlist, Notification mẫu.

> Lưu ý: mật khẩu seed chỉ dành cho phát triển; thay đổi trước khi triển khai production. Quản lý vai trò qua API: `POST/PUT /api/users` (chỉ Admin).

## Ảnh xạ user story → code

| User Story | Hiện thực |
|------------|-----------|
| US-01 CRUD phòng/thiết bị | `Resources` (Create/Update/Delete/Get) |
| US-02 Lịch khả dụng | `GetResourceAvailabilityQuery` |
| US-03 Đặt lịch | `CreateBookingCommand` |
| US-04 Xung đột + khung thay thế | `BookingEvaluation`, `Scheduling`, `CheckBookingConflictCommand` |
| US-05 Duyệt/từ chối theo ưu tiên | `ApproveBookingCommand`, `RejectBookingCommand` |
| US-06 Cấu hình quy tắc ưu tiên | `PriorityRules` CRUD |
| US-07 Waitlist | `JoinWaitlistCommand`, `WaitlistEvaluation` |
| US-08 Check-in/out | `CheckInBookingCommand`, `CheckOutBookingCommand` |
| US-09 Báo cáo sự cố | `CreateIncidentCommand` |
| US-10 Bảo trì khoá khung giờ | `CreateMaintenanceCommand`, `ResolveMaintenanceCommand` |
| US-11 Ghi nhận vi phạm | `ViolationSweeper`, `CheckOutBookingCommand` (Late) |
| US-12 Hạn chế quyền đặt lịch | `Restrictions`, `ViolationSweeper.AutoRestrictAsync` |
| US-13 Dashboard tỷ lệ sử dụng | `GetUsageDashboardQuery` |
| US-14 Báo cáo bảo trì & chi phí | `GetMaintenanceReportQuery` |
| US-15 Xác thực JWT | `Auth` (register/login/refresh/logout) |
| US-16 Nhắc lịch | `BookingReminderService` |
| Quản lý người dùng (Admin) | `Users` (GetUsers, CreateUser, UpdateUser, DeleteUser, ChangePassword, ResetPassword) |

## Xác thực & phân quyền

- **Access token**: JWT chứa claims `sub`, `name`, `email`, `role`, `jti`; ký HMAC-SHA256, hết hạn sau `Jwt:ExpiryMinutes`.
- **Refresh token**: chuỗi ngẫu nhiên 512-bit lưu DB, hết hạn sau `Jwt:RefreshExpiryDays`; **rotate khi dùng** (token cũ bị revoke), cấp cặp token mới.
- `POST /api/auth/logout` thu hồi refresh token hiện tại.
- All HTTP API responses được **bọc chung một envelope** `ApiResponse` (gồm `statusCode`, `isSuccess`, `errorMessages`, `result`) — do `ApiResponseWrapperFilter` + `GlobalExceptionHandler` đảm bảo.

### Matrận vai trò

| Vùng | Admin | Lab Manager | Requester | Khách |
|------|:-----:|:-----------:|:---------:|:-----:|
| Đọc danh mục tài nguyên / lịch khả dụng / danh mục | ✔ | ✔ | ✔ | ✔ |
| Đặt/huỷ lịch, check-in/out (lịch của mình) | ✔ | ✔ (nhân danh quản lý) | ✔ | |
| Duyệt/từ chối lịch phòng mình phụ trách | ✔ (mọi nơi) | ✔ (phòng của mình) | | |
| CRUD tài nguyên, quy tắc ưu tiên | ✔ | | | |
| Quản lý người dùng (CRUD, reset mật khẩu) | ✔ | | | |
| Lập lịch/hoàn tất bảo trì | ✔ | ✔ (phòng của mình) | | |
| Xem dashboard | ✔ | ✔ (phạm vi phòng mình) | | |
| Báo cáo bảo trì | ✔ | | | |
| Hạn chế quyền đặt lịch (Restriction) | ✔ | | | |
| Danh sách vi phạm | tất cả | phạm vi phòng mình | lịch của mình | |
| Danh sách waitlist | tất cả | | lịch của mình | |

## Luồng nghiệp vụ chính

**Đặt lịch:** `POST /api/bookings` → kiểm tra end > start, thời điểm tương lai, tài nguyên tồn tại, không có Restriction hiệu lực → tìm booking (Pending/Approved) và maintenance trùng khung giờ; nếu xung đột, trả `409 Conflict` kèm `BookingConflictResponse` chứa **tối đa 3 khung thay thế** cùng độ dài trong cửa sổ ±3 ngày, giờ hoạt động **07:00–22:00**.

**Duyệt lịch với tranh chấp ưu tiên:** Lab Manager/Admin duyệt booking Pending; nếu có booking Approved trùng khung giờ, so `PriorityLevel` (số nhỏ = ưu tiên cao): lịch mới ưu tiên cao hơn → các lịch Approved đang chiếm chỗ bị tự động Rejected để nhường; ngược lại → `409 Conflict`.

**Check-out trễ:** check-out sau `end_time + LateGraceMinutes` → tự tạo vi phạm `Late`.

**No-show:** background job quét booking Approved đã quá `end_time + NoShowGraceMinutes` mà không check-in → ghi vi phạm `NoShow`. Khi một người dùng đạt `Violation:Threshold` vi phạm trong `Violation:WindowDays` → tự tạo `Restriction`, set `User.Status = Restricted`; hết hạn → tự trả về `Active`.

**Waitlist:** khi một booking bị huỷ/từ chối, hệ thống tìm waitlist `Waiting` giao khung giờ trên cùng tài nguyên, gửi thông báo `WaitlistAvailable` cho tối đa 5 người theo thứ tự đăng ký sớm nhất; yêu cầu chờ đã qua `DesiredEnd` đánh dấu `Expired`.

**Bảo trì khoá lịch:** lập lịch bảo trì bị chặn nếu trùng booking (Pending/Approved) hoặc maintenance khác; ngược lại booking/availability cũng loại trừ maintenance `Scheduled/InProgress` khỏi khung trống.

## Kiểm thử

xUnit, không phụ thuộc cơ sở dữ liệu — dùng fakes (`FakeRepository`, `FakeSender`, `FakeUnitOfWork`, `FakeCurrentUser`) trong `LabBooking.Tests/Fakes.cs`.

```
dotnet test LabBooking.Tests
```

Coverage chính:

- `AuthTests` — đăng ký, đăng nhập, refresh rotate, logout revoke
- `BookingTests` — xung đột, khung thay thế, duyệt theo ưu tiên, huỷ theo thời hạn, check-in/out, vi phạm trả trễ
- `WaitlistTests` — join/leave, notify theo thứ tự, hết hạn
- `ViolationSweeperTests` — no-show, auto-restriction, đồng bộ trạng thái người dùng
- `SchedulingTests` — overlap, merge, free gaps, gợi ý slot theo giờ hoạt động
- `IncidentTests`, `TokenServiceTests`, `HttpResponseTests`, `ControllerTests`, `BaseEntityTests`, `UserTests`

## Tài liệu

| Tài liệu | Nội dung |
|----------|----------|
| [`docs/SRS_ProductBacklog.md`](docs/SRS_ProductBacklog.md) | Đặc tả yêu cầu, Product Backlog, kế hoạch Sprint, Definition of Done |
| [`docs/ERD.md`](docs/ERD.md) | Thiết kế cơ sở dữ liệu: 13 thực thể, quan hệ, ràng buộc toàn vẹn |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Kiến trúc Clean Architecture: luồng request, DI, background services, mô hình xung đột |
| [`docs/API.md`](docs/API.md) | Tài liệu tham khảo đầy đủ các endpoint REST |

## Ghi chú triển khai

Đổi khoá JWT (`Jwt:Key`) trước khi triển khai production. Chống chồng lấn thời gian (booking ^ maintenance) được đảm bảo ở **cả hai lớp**: kiểm tra ở tầng ứng dụng (`BookingEvaluation`) và trigger ở tầng DB (`TR_Bookings_BlockOverlap`, `TR_Maintenances_BlockOverlap`) — `SqlException` 50001/50002/51001/51002 ánh xạ thành `409 Conflict`.