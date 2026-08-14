# Thiết Kế Cơ Sở Dữ Liệu (ERD)

**Đề tài 03 — Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung**

## 1. Tổng quan mô hình dữ liệu

Mô hình gồm 12 thực thể nghiệp vụ chính, chia thành 4 nhóm:

1. **Nhóm người dùng & tổ chức**: Department, User
2. **Nhóm danh mục**: Resource, PriorityRule
3. **Nhóm đặt lịch & vận hành**: Booking, Waitlist, CheckInOut
4. **Nhóm sự cố/bảo trì/vi phạm**: Incident, Maintenance, Violation, Restriction

và Notification phục vụ nhắc lịch.

## 2. Mô tả chi tiết thực thể

### Department — Khoa/Bộ môn

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| department_id | INT/GUID | PK | Khoá chính |
| name | NVARCHAR(200) | NOT NULL, UNIQUE | Tên khoa/bộ môn |

Dùng để phân nhóm User và Resource theo khoa/bộ môn — phục vụ thống kê tỷ lệ sử dụng theo khoa/bộ môn.

### User — Người dùng

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| user_id | INT/GUID | PK | Khoá chính |
| full_name | NVARCHAR(200) | NOT NULL | Họ tên |
| email | NVARCHAR(200) | NOT NULL, UNIQUE | Dùng để đăng nhập |
| password_hash | NVARCHAR(500) | NOT NULL | Mật khẩu đã băm |
| role | ENUM | NOT NULL | Admin / LabManager / Requester |
| department_id | INT/GUID | FK → Department | Khoa/bộ môn trực thuộc |
| status | ENUM | NOT NULL | Active / Restricted / Disabled |
| created_at | DATETIME | NOT NULL | Ngày tạo tài khoản |

- `role` quyết định phân quyền theo Role/Claim khi phát hành JWT.
- `status = Restricted` được hệ thống tự set khi Restriction đang hiệu lực.

### Resource — Phòng/Thiết bị

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| resource_id | INT/GUID | PK | Khoá chính |
| name | NVARCHAR(200) | NOT NULL | Tên phòng/thiết bị |
| type | ENUM | NOT NULL | Room / Equipment |
| specifications | NVARCHAR(MAX) | NULL | Thông số kỹ thuật |
| image_url | NVARCHAR(500) | NULL | Hình ảnh minh hoạ |
| usage_rules | NVARCHAR(MAX) | NULL | Quy định sử dụng |
| department_id | INT/GUID | FK → Department | Khoa/bộ môn quản lý |
| lab_manager_id | INT/GUID | FK → User | Người phụ trách duyệt lịch |
| status | ENUM | NOT NULL | Available / UnderMaintenance / Disabled |

Thiết kế gộp Room và Equipment vào một thực thể Resource (phân biệt qua type) để dùng chung logic lịch/xung đột/bảo trì — tránh trùng lặp bảng.

### PriorityRule — Quy tắc ưu tiên

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| rule_id | INT/GUID | PK | Khoá chính |
| name | NVARCHAR(200) | NOT NULL | VD: Đề tài nghiên cứu, Môn học, Tự học |
| priority_level | INT | NOT NULL | Số càng nhỏ càng ưu tiên cao |
| description | NVARCHAR(500) | NULL | Diễn giải áp dụng |

Admin cấu hình bảng này; hệ thống tra cứu priority_level khi có tranh chấp khung giờ giữa nhiều Booking.

### Booking — Yêu cầu đặt lịch

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| booking_id | INT/GUID | PK | Khoá chính |
| resource_id | INT/GUID | FK → Resource, NOT NULL | Phòng/thiết bị được đặt |
| requester_id | INT/GUID | FK → User, NOT NULL | Người đặt lịch |
| rule_id | INT/GUID | FK → PriorityRule | Mức ưu tiên áp dụng |
| start_time | DATETIME | NOT NULL | Thời điểm bắt đầu |
| end_time | DATETIME | NOT NULL, CHECK end_time > start_time | Thời điểm kết thúc |
| purpose | NVARCHAR(500) | NOT NULL | Mục đích sử dụng |
| status | ENUM | NOT NULL | Pending/Approved/Rejected/Cancelled/Completed |
| approved_by | INT/GUID | FK → User (Lab Manager) | Người duyệt |
| approved_at | DATETIME | NULL | Thời điểm duyệt |
| created_at | DATETIME | NOT NULL | Thời điểm tạo yêu cầu |

- Ràng buộc toàn vẹn quan trọng nhất hệ thống: KHÔNG cho phép hai bản ghi Booking ở trạng thái Approved trùng khung giờ trên cùng resource_id. Hiện thực bằng EXCLUSION CONSTRAINT (PostgreSQL, dùng GiST + tsrange) hoặc trigger kiểm tra chồng lấn (SQL Server) — không chỉ kiểm tra ở tầng ứng dụng.
- Index `(resource_id, start_time, end_time)` để tối ưu truy vấn phát hiện xung đột và hiển thị calendar.

### Waitlist — Hàng đợi chờ

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| waitlist_id | INT/GUID | PK | Khoá chính |
| resource_id | INT/GUID | FK → Resource, NOT NULL | Phòng/thiết bị mong muốn |
| requester_id | INT/GUID | FK → User, NOT NULL | Người chờ |
| desired_start | DATETIME | NOT NULL | Khung giờ mong muốn — bắt đầu |
| desired_end | DATETIME | NOT NULL | Khung giờ mong muốn — kết thúc |
| status | ENUM | NOT NULL | Waiting / Notified / Expired / Converted |
| notified_at | DATETIME | NULL | Thời điểm hệ thống thông báo có chỗ trống |

Khi một Booking bị huỷ (status = Cancelled) hoặc hết hạn giữ chỗ, hệ thống quét Waitlist theo resource_id + khung giờ giao nhau để thông báo theo thứ tự đăng ký trước.

### CheckInOut — Check-in/Check-out

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| checkinout_id | INT/GUID | PK | Khoá chính |
| booking_id | INT/GUID | FK → Booking, UNIQUE, NOT NULL | Booking tương ứng (1-1) |
| check_in_time | DATETIME | NULL | Thời điểm check-in thực tế |
| check_out_time | DATETIME | NULL | Thời điểm check-out thực tế |
| actual_duration | INT (phút) | NULL, tính toán | Thời lượng sử dụng thực tế |

- `booking_id` là UNIQUE để đảm bảo quan hệ 1-1 với Booking.
- `check_out_time` trễ hơn `end_time` của Booking quá ngưỡng cấu hình sẽ tự sinh một bản ghi Violation (type = Late).

### Incident — Sự cố/Hư hỏng

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| incident_id | INT/GUID | PK | Khoá chính |
| booking_id | INT/GUID | FK → Booking | Booking liên quan (nếu có) |
| resource_id | INT/GUID | FK → Resource, NOT NULL | Phòng/thiết bị bị sự cố |
| reported_by | INT/GUID | FK → User, NOT NULL | Người ghi nhận |
| description | NVARCHAR(MAX) | NOT NULL | Mô tả sự cố |
| image_url | NVARCHAR(500) | NULL | Hình ảnh minh chứng |
| status | ENUM | NOT NULL | Open / InReview / Resolved |
| reported_at | DATETIME | NOT NULL | Thời điểm ghi nhận |

### Maintenance — Lịch bảo trì

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| maintenance_id | INT/GUID | PK | Khoá chính |
| resource_id | INT/GUID | FK → Resource, NOT NULL | Phòng/thiết bị bảo trì |
| start_time | DATETIME | NOT NULL | Bắt đầu bảo trì |
| end_time | DATETIME | NOT NULL, CHECK end_time > start_time | Kết thúc bảo trì |
| description | NVARCHAR(MAX) | NULL | Nội dung bảo trì |
| cost | DECIMAL(12,2) | NULL | Chi phí phát sinh |
| status | ENUM | NOT NULL | Scheduled / InProgress / Completed |
| created_by | INT/GUID | FK → User (Lab Manager) | Người lập lịch |

Khung thời gian Maintenance áp dụng cùng ràng buộc chống chồng lấn như Booking trên cùng resource_id — dùng chung cơ chế exclusion constraint để tự động khoá lịch đặt.

### Violation — Vi phạm

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| violation_id | INT/GUID | PK | Khoá chính |
| user_id | INT/GUID | FK → User, NOT NULL | Người vi phạm |
| booking_id | INT/GUID | FK → Booking | Booking liên quan (nếu có) |
| type | ENUM | NOT NULL | Late / NoShow |
| recorded_at | DATETIME | NOT NULL | Thời điểm ghi nhận |
| note | NVARCHAR(500) | NULL | Ghi chú |

### Restriction — Hạn chế quyền đặt lịch

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| restriction_id | INT/GUID | PK | Khoá chính |
| user_id | INT/GUID | FK → User, NOT NULL | Người bị hạn chế |
| start_date | DATE | NOT NULL | Ngày bắt đầu hạn chế |
| end_date | DATE | NOT NULL | Ngày kết thúc hạn chế |
| reason | NVARCHAR(500) | NOT NULL | Lý do (số lần vi phạm...) |
| created_by | INT/GUID | FK → User (Admin) | Người áp dụng |

Khi tồn tại Restriction đang hiệu lực (ngày hiện tại nằm trong [start_date, end_date]), hệ thống chặn User tạo Booking mới và cập nhật User.status = Restricted.

### Notification — Thông báo

| Trường | Kiểu dữ liệu | Khoá/Ràng buộc | Mô tả |
|--------|-------------|----------------|-------|
| notification_id | INT/GUID | PK | Khoá chính |
| user_id | INT/GUID | FK → User, NOT NULL | Người nhận |
| type | ENUM | NOT NULL | BookingReminder / WaitlistAvailable / BookingApproved... |
| content | NVARCHAR(MAX) | NOT NULL | Nội dung thông báo |
| is_read | BIT | NOT NULL DEFAULT 0 | Trạng thái đã đọc |
| created_at | DATETIME | NOT NULL | Thời điểm tạo |

## 3. Bảng tổng hợp quan hệ giữa các thực thể

| Quan hệ | Cardinality | Mô tả |
|---------|-------------|-------|
| Department — User | 1 — N | Một khoa/bộ môn có nhiều người dùng |
| Department — Resource | 1 — N | Một khoa/bộ môn quản lý nhiều phòng/thiết bị |
| User (LabManager) — Resource | 1 — N | Một Lab Manager phụ trách nhiều phòng/thiết bị |
| Resource — Booking | 1 — N | Một phòng/thiết bị có nhiều yêu cầu đặt lịch |
| User (Requester) — Booking | 1 — N | Một người dùng tạo nhiều yêu cầu đặt lịch |
| User (LabManager) — Booking | 1 — N | Một Lab Manager duyệt nhiều Booking (approved_by) |
| PriorityRule — Booking | 1 — N | Một quy tắc ưu tiên áp dụng cho nhiều Booking |
| Booking — CheckInOut | 1 — 1 | Mỗi Booking có tối đa một bản ghi check-in/out |
| Resource — Waitlist | 1 — N | Một phòng/thiết bị có nhiều người chờ |
| User — Waitlist | 1 — N | Một người dùng có thể chờ nhiều khung giờ |
| Booking / Resource — Incident | 1 — N | Một Booking hoặc Resource có thể phát sinh nhiều sự cố |
| Resource — Maintenance | 1 — N | Một phòng/thiết bị có nhiều đợt bảo trì |
| User — Violation | 1 — N | Một người dùng có thể có nhiều vi phạm |
| Booking — Violation | 0 — 1 | Một vi phạm có thể gắn với một Booking cụ thể |
| User — Restriction | 1 — N | Một người dùng có thể bị hạn chế nhiều lần |
| User — Notification | 1 — N | Một người dùng nhận nhiều thông báo |

## 4. Ghi chú thiết kế chung

- Tất cả khoá chính dùng kiểu GUID (hoặc BIGINT identity) tuỳ lựa chọn của nhóm Backend.
- Áp dụng soft-delete (cột `is_deleted`) cho Resource và User thay vì xoá cứng, để giữ toàn vẹn lịch sử Booking/Incident/Maintenance.
- Toàn bộ cột thời gian lưu theo UTC ở tầng CSDL; quy đổi múi giờ hiển thị ở tầng Frontend.
- Chỉ mục (index) khuyến nghị: `Booking(resource_id, start_time, end_time)`, `Booking(requester_id)`, `Waitlist(resource_id, desired_start)`, `User(email)`.

## 5. Hiện thực trong mã nguồn (LabBooking)

- 13 thực thể nằm ở `LabBooking.Domain/Entities` (12 thực thể nghiệp vụ trên + `RefreshToken` phục vụ xác thực), enum nằm ở `LabBooking.Domain/Enums/*.cs` (mỗi enum một file).
- Ánh xạ bảng, index, check constraint, quan hệ và query filter trong `LabBooking.Infrastructure.Sqlserver/Configurations/*.cs` (mỗi entity một file cấu hình).
- Core logic chống chồng lấn: `LabBooking.Domain/Scheduling/Scheduling.cs` là logic thuần (overlap, gộp khoảng, tính khung trống, đề xuất slot); `LabBooking.Application/Features/Bookings/BookingEvaluation.cs` phối hợp kiểm tra Approved/Pending-booking và Maintenance overlap trước khi tạo/duyệt booking, cũng như trong `CreateMaintenanceCommand`.
- Các ràng buộc cứng ở tầng DB đã có trong migration: check constraint `EndTime > StartTime` (Booking, Maintenance), `DesiredEnd > DesiredStart` (Waitlist), `EndDate >= StartDate` (Restriction), unique index `(BookingId, Type)` (Violation), unique `(Email)` (User), unique `(BookingId)` (CheckInOut).
- Chống chồng lấn thời gian được đảm bảo **2 lớp**: kiểm tra ở tầng ứng dụng (`BookingEvaluation`) và trigger ở tầng DB — `TR_Bookings_BlockOverlap` chặn booking^booking (`THROW 50001`) và booking^maintenance (`THROW 50002`); `TR_Maintenances_BlockOverlap` chặn maintenance^maintenance (`THROW 51001`) và maintenance^booking (`THROW 51002`) — nằm trong migration `20260811021109_AddOverlapTriggers`; `SqlException` 50001/50002/51001/51002 được ánh xạ thành `409 Conflict`.
