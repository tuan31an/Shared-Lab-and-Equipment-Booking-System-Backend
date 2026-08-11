# API Reference — LabBooking

**Đề tài 03 — Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung**

Base URL: `https://localhost:<port>`. Swagger UI (Development): `/swagger/`.

## Conventions

### Envelope phản hồi
Mọi endpoint trả về chung một hình dạng `ApiResponse` (ÁP DỤNG bởi `ApiResponseWrapperFilter` + `GlobalExceptionHandler`):

```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": { }
}
```

Trong trường hợp lỗi: `statusCode` = HTTP status, `isSuccess = false`, `errorMessages` = mảng thông báo, `result` = null (riêng `409 Conflict` của `check-conflict`/`create` có `result` chứa `BookingConflictResponse`).

### Xác thực
Hầu hết endpoint yêu cầu `Authorization: Bearer <accessToken>`, lấy từ `POST /api/auth/login` hoặc `POST /api/auth/register`. Token hết hạn sau `Jwt:ExpiryMinutes` (60).

### Lỗi HTTP chuẩn
| Mã | Ý nghĩa |
|----|---------|
| 400 | Dữ liệu vào không hợp lệ / vi phạm nghiệp vụ (`ArgumentException`, ModelState) |
| 401 | Chưa đăng nhập / token sai / quá hạn |
| 404 | Tài nguyên không tồn tại |
| 409 | Xung đột (email trùng, lịch trùng giờ, maintenance trùng, waitlist trùng...) |
| 500 | Lỗi ngoài dự kiến (chi tiết bị giấu, chỉ log server) |

### Phân trang
Các endpoint list trả `PaginationResponse`:

```json
{
  "items": [], "pageNumber": 1, "pageSize": 20,
  "totalCount": 0, "totalPages": 1,
  "hasPrevious": false, "hasNext": false
}
```

Query: `?page=1&pageSize=20` (tối đa `pageSize=100`).

---

## 1. Xác thực — `/api/auth`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| POST | `/register` | Mọi người | Tạo tài khoản `Requester` (giấu mật khẩu bằng BCrypt) |
| POST | `/login` | Mọi người | Đăng nhập, cấp cặp access + refresh token |
| POST | `/refresh` | Mọi người | Làm mới access token; **rotate** (thu hồi) token cũ |
| POST | `/logout` | Xác thực | Thu hồi refresh token |

**register** body:
```json
{ "fullName": "Nguyen Van A", "email": "a@example.com", "password": "secret123", "departmentId": null }
```
→ `201` trả `result: UserDto { id, fullName, email, role, status, createdAt }`. Lỗi: email trùng → `409`.

**login** body: `{ "email": "...", "password": "..." }`
→ `200` trả `result: AuthResponse { accessToken, refreshToken, expiresIn, user }`. Sai tài khoản → `401`.

**refresh** body: `{ "refreshToken": "..." }`
→ `200` trả cặp token mới (`AuthResponse`). Token sai/hết hạn/đã revoke → `401`.

**logout** body (kèm Bearer token): `{ "refreshToken": "..." }` → `204`.

---

## 2. Khoa/Bộ môn — `/api/departments`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Mọi người | Danh sách khoa/bộ môn |

→ `result: DepartmentDto[] { id, name }`.

---

## 3. Danh mục phòng/thiết bị — `/api/resources`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Mọi người | Danh sách tài nguyên (phân trang, lọc, tìm kiếm theo từ khoá tên) |
| GET | `/{id}` | Mọi người | Chi tiết một tài nguyên |
| GET | `/{id}/availability` | Mọi người | Lịch khả dụng trong khoảng thời gian |
| POST | `/` | Admin | Tạo tài nguyên |
| PUT | `/{id}` | Admin | Cập nhật tài nguyên |
| DELETE | `/{id}` | Admin | Xoá mềm (soft-delete) |

**GET `/` query:** `page`, `pageSize`, `type` (`Room`/`Equipment`), `departmentId`, `status` (`Available`/`UnderMaintenance`/`Disabled`), `keyword`.
→ `result: PaginationResponse<ResourceDto>`.

`ResourceDto`: `{ id, name, type, specifications, imageUrl, usageRules, departmentId, departmentName, labManagerId, labManagerName, status, createdAt }`.

**GET `/{id}/availability` query:** `from` (bắt buộc), `to` (bắt buộc).
→ `result: AvailabilitySlotDto[]` sắp theo `startTime`:

```json
{ "startTime": "...", "endTime": "...", "status": "Booked|UnderMaintenance|Free", "bookingId": "..." }
```

Chỉ trả Free slot trong giờ hoạt động **07:00–22:00**. Booking ở trạng thái `Pending`/`Approved` tính là `Booked`; maintenance trạng thái `Scheduled`/`InProgress` tính là `UnderMaintenance`. Lỗi `to <= from` → `400`, tài nguyên không tồn tại → `404`.

**POST** body:
```json
{
  "name": "CS Lab B",
  "type": "Room",
  "specifications": null, "imageUrl": null, "usageRules": null,
  "departmentId": null, "labManagerId": null
}
```
→ `201` trả `ResourceDto`. Department/LabManager không tồn tại → `404`.

**PUT `/{id}`** body: như POST + `status`.

**DELETE `/{id}`** → `204`, set `IsDeleted = true`.

---

## 4. Quy tắc ưu tiên — `/api/priority-rules`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Mọi người | Danh sách quy tắc ưu tiên |
| POST | `/` | Admin | Tạo quy tắc |
| PUT | `/{id}` | Admin | Cập nhật quy tắc |
| DELETE | `/{id}` | Admin | Xoá quy tắc (chặn nếu đang được dùng) |

`PriorityRuleDto`: `{ id, name, priorityLevel, description }`. `priorityLevel` càng nhỏ càng ưu tiên cao (≥1).

POST body: `{ "name": "Course", "priorityLevel": 2, "description": "..." }`. Tên/level trùng → `409`.
DELETE: quy tắc đang gắn vào booking → `409`.

---

## 5. Đặt lịch — `/api/bookings`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách booking theo vai trò (phân trang + lọc) |
| POST | `/` | Xác thực | Tạo booking (tự phát hiện xung đột) |
| POST | `/check-conflict` | Xác thực | Kiểm tra xung đột + khung thay thế, không tạo lịch |
| POST | `/{id}/approve` | Admin, LabManager | Duyệt booking + xử lý tranh chấp ưu tiên |
| POST | `/{id}/reject` | Admin, LabManager | Từ chối booking, trả giờ cho waitlist |
| POST | `/{id}/cancel` | Xác thực | Huỷ lịch (chủ sở hữu), trả giờ cho waitlist |
| POST | `/{id}/checkin` | Xác thực | Check-in (requester/manager/admin) |
| POST | `/{id}/checkout` | Xác thực | Check-out, ghi thời lượng, vi phạm trả trễ |
| GET | `/{id}` | Xác thực | Chi tiết booking |

`BookingDto`: `{ id, resourceId, resourceName, requesterId, requesterName, priorityRuleId, priorityRuleName, startTime, endTime, purpose, status, approvedBy, approvedAt, checkInTime, checkOutTime, actualDuration, createdAt }`.

**GET `/` query:** `page`, `pageSize`, `status` (`Pending|Approved|Rejected|Cancelled|Completed`), `resourceId`, `requesterId`, `from`, `to`.
**Scope theo vai trò:** Admin → tất cả; LabManager → booking trên resource mình phụ trách; Requester → lịch của mình.

**POST `/` body:**
```json
{
  "resourceId": "guid", "startTime": "2026-08-12T09:00:00Z", "endTime": "2026-08-12T11:00:00Z",
  "purpose": "Thí nghiệm", "priorityRuleId": null
}
```
→ `201` trả `BookingDto` (trạng thái `Pending`).
Lỗi: `endTime <= startTime` | `startTime` quá khứ | đang bị Restriction | tài nguyên/rule không tồn tại → 400/404; **xung đột lịch hoặc maintenance → `409`** với `result`:

```json
{
  "hasConflict": true,
  "conflictingBookings": [BookingDto...],
  "suggestedSlots": [AvailabilitySlotDto (status "Free")...]   // tối đa 3 khung thay thế
}
```

**POST `/check-conflict` body:** `{ resourceId, startTime, endTime }`.
→ `200` trả `BookingConflictResponse` (tương tự trên), `hasConflict = false` khi khung trống.

**POST `/{id}/approve`:** chỉ booking `Pending`. Xung đột với booking `Approved` cùng giờ:
- Lịch mới có ưu tiên cao hơn (level nhỏ hơn) → các lịch Approved đang giữ chỗ tự bị **Rejected** rồi duyệt lịch mới.
- Ngược lại → `409`.
- Maintenance overlap → `409`.
→ `200` trả `BookingDto` (`Approved`).

**POST `/{id}/reject`** body (tuỳ chọn): `{ "reason": "..." }`. → `200` trả `BookingDto` (`Rejected`); notify waitlist.

**POST `/{id}/cancel`:** chỉ chủ booking; phải còn hơn `Booking:CancellationDeadlineHours` (2) giờ so với `startTime`; không thể huỷ `Completed/Cancelled/Rejected`. → `200` trả `BookingDto` (`Cancelled`); notify waitlist theo thứ tự đăng ký trước.

**POST `/{id}/checkin`:** booking `Approved`; thời điểm trong `[startTime - 2h, endTime]`; chỉ requester/manager/admin; chống check-in trùng. → `200` trả `BookingDto` kèm `checkInTime`.

**POST `/{id}/checkout`:** bắt buộc đã check-in; chưa checkout. Set `Completed`, tính `actualDuration`; nếu checkout muộn hơn `endTime + Violation:LateGraceMinutes` → tự ghi vi phạm `Late`. → `200` trả `BookingDto`.

**GET `/{id}`:** → `200` trả `BookingDto`; không tồn tại → `404`.

---

## 6. Sự cố — `/api/incidents`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách sự cố theo phạm vi vai trò |
| POST | `/` | Xác thực | Báo cáo sự cố; tự thông báo Lab Manager phụ trách |

`IncidentDto`: `{ id, resourceId, resourceName, bookingId, reportedBy, reportedByName, description, imageUrl, status, reportedAt }`.

GET query: `status` (`Open|InReview|Resolved`), `resourceId`. Scope: Admin → tất cả; LabManager → resource mình quản lý; Requester → sự cố mình báo.

POST body:
```json
{
  "resourceId": "guid", "bookingId": null,
  "description": "Máy chiếu hư", "imageUrl": null
}
```
→ `201` trả `IncidentDto` (`status: Open`). Tài nguyên không tồn tại → `404`.

---

## 7. Bảo trì — `/api/maintenances`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách bảo trì |
| POST | `/` | Xác thực | Lập lịch bảo trì (chỉ Admin hoặc LabManager của resource; khoá khung giờ) |
| POST | `/{id}/resolve` | Xác thực | Hoàn tất bảo trì (Admin/LabManager) |

`MaintenanceDto`: `{ id, resourceId, resourceName, startTime, endTime, description, cost, status, createdBy }`.

GET query: `resourceId`, `status` (`Scheduled|InProgress|Completed`).

POST body: `{ "resourceId": "...", "startTime": "...", "endTime": "...", "description": "...", "cost": 150.00 }`.
→ `201`. Lỗi: trùng booking đang giữ giờ hoặc maintenance khác → `409`; không đủ quyền → `401`; `endTime <= startTime` → `400`.

POST `/{id}/resolve` body (tuỳ chọn): `{ "cost": 200 }`. → `200` trả `MaintenanceDto` (`Completed`); đã completed → `400`.

---

## 8. Hàng đợi chờ — `/api/waitlists`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách waitlist (Admin: tất cả; còn lại: lịch của mình) |
| POST | `/` | Xác thực | Đăng ký chờ khung giờ |
| DELETE | `/{id}` | Xác thực | Rút khỏi waitlist (chủ sở hữu/Admin) |

`WaitlistDto`: `{ id, resourceId, resourceName, requesterId, desiredStart, desiredEnd, status, notifiedAt, createdAt }`.

GET query: `activeOnly` (`bool`) — chỉ lịch `Waiting`.

POST body: `{ "resourceId": "...", "desiredStart": "...", "desiredEnd": "..." }`.
→ `201`. Lỗi: khung đã qua → `400`; trùng yêu cầu đang chờ → `409`.

DELETE `/{id}`: không rút được khi entry đang `Notified`/`Converted` → `400`. Set status `Expired`.

---

## 9. Vi phạm — `/api/violations`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách vi phạm theo phạm vi vai trò |

`ViolationDto`: `{ id, userId, userName, bookingId, type, recordedAt, note }`.

GET query: `userId`, `type` (`Late|NoShow`).
Scope: Admin → tất cả; LabManager → vi phạm của booking trên resource mình quản lý; Requester → vi phạm của mình.

---

## 10. Hạn chế quyền đặt lịch — `/api/restrictions`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách restriction |
| POST | `/` | Admin | Tạo restriction thủ công |
| DELETE | `/{id}` | Admin | Gỡ restriction, đồng bộ lại User.Status |

`RestrictionDto`: `{ id, userId, userName, startDate, endDate, reason, createdBy }`.

GET query: `userId`, `activeOnly` (`bool` — chỉ còn hiệu lực hôm nay).
Scope: Admin → tất cả; còn lại → restriction của mình.

POST body: `{ "userId": "...", "startDate": "...", "endDate": "...", "reason": "..." }`.
→ `201`, set `User.Status = Restricted`. `endDate < startDate` → `400`.

DELETE `/{id}`: → `200`, nếu không còn restriction hiệu lực nào thì đưa user về `Active`.

---

## 11. Thông báo — `/api/notifications`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/` | Xác thực | Danh sách thông báo của chính mình |
| PUT | `/{id}/read` | Xác thực | Đánh dấu đã đọc (chỉ thông báo của mình) |

`NotificationDto`: `{ id, type, content, isRead, createdAt }`.

GET query: `unreadOnly` (`bool`).

Types: `BookingReminder`, `WaitlistAvailable`, `BookingApproved`, `BookingRejected`, `IncidentReported`.

---

## 12. Dashboard — `/api/dashboard`

| Method | Endpoint | Vai trò | Mô tả |
|--------|----------|---------|-------|
| GET | `/usage` | Admin, LabManager | Tỷ lệ sử dụng theo tài nguyên & khoa/bộ môn |
| GET | `/maintenance-report` | Admin | Báo cáo bảo trì & chi phí |

**GET `/usage` query:** `from`, `to`, `resourceId`, `departmentId` (mặc định: 30 ngày về trước).
Scope: Admin → tất cả resource; LabManager → resource mình phụ trách.
→ `result`:

```json
{
  "from": "...", "to": "...", "overallUsagePercent": 12.5,
  "totalBookedMinutes": 120, "totalActualMinutes": 90,
  "byResource": [ { "resourceId": "...", "resourceName": "...", "departmentName": "...",
                    "bookedMinutes": 0, "actualMinutes": 0, "usagePercent": 0 } ],
  "byDepartment": [ { "departmentId": "...", "departmentName": "...",
                       "bookedMinutes": 0, "actualMinutes": 0, "usagePercent": 0 } ]
}
```

Tiêu chí: công suất tối đa 15h/ngày (07:00–22:00); `usagePercent = actualMinutes / (capacityDays × 15h × 60)`.

**GET `/maintenance-report` query:** `from`, `to`, `resourceId`.
→ `result`:

```json
{
  "from": "...", "to": "...", "totalCount": 2, "totalCost": 650.00,
  "items": [MaintenanceDto...],
  "byResource": [ { "resourceId": "...", "resourceName": "...",
                    "maintenanceCount": 1, "totalCost": 150.00 } ]
}
```

Requester gọi → `401`.

## Ghi chú

- ID dạng GUID trên mọi đường dẫn `{id:guid}`/`{bookingId:guid}`/...
- Mọi thời gian lưu theo UTC (`DateTime.UtcNow`); client tự quy đổi múi giờ hiển thị.
- Tài khoản seed trong `DataSeeder` có `PasswordHash` rỗng — không đăng nhập được; đăng ký tài khoản mới để test luồng auth và chỉnh role trong DB nếu cần.