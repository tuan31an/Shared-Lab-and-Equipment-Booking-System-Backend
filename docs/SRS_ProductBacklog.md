# Tài liệu Đặc tả Yêu cầu Phần mềm (SRS) & Product Backlog

**Đề tài 03 — Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung**

## 1. Giới thiệu

### 1.1. Mục đích tài liệu

Tài liệu này đặc tả chi tiết yêu cầu chức năng và phi chức năng của hệ thống "Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm/Thiết bị Dùng chung", đồng thời cung cấp Product Backlog làm cơ sở lập kế hoạch phát triển qua 3 Sprint. Tài liệu phục vụ cho toàn bộ thành viên nhóm (BA, Frontend, Backend, QA/DevOps) và hội đồng đánh giá.

### 1.2. Phạm vi hệ thống

Hệ thống hỗ trợ ba nhóm người dùng (Admin, Lab Manager, Requester) thực hiện toàn bộ quy trình: quản lý danh mục phòng/thiết bị, đặt lịch, duyệt yêu cầu, phát hiện xung đột lịch, check-in/check-out, quản lý bảo trì, xử lý vi phạm và thống kê báo cáo. Phạm vi Sprint 1 tập trung vào phân tích, thiết kế và khởi tạo nền tảng kỹ thuật; các chức năng nghiệp vụ được hiện thực ở Sprint 2 và 3.

### 1.3. Đối tượng người dùng (Actors)

| Actor | Trách nhiệm |
|-------|-------------|
| Quản trị viên (Admin) | Quản lý danh mục phòng/thiết bị, người dùng, quy tắc ưu tiên, xem báo cáo tổng thể. |
| Quản lý phòng thí nghiệm (Lab Manager) | Duyệt/từ chối yêu cầu đặt lịch, lên lịch bảo trì, xử lý sự cố, xem dashboard theo phòng phụ trách. |
| Sinh viên/Giảng viên (Requester) | Tìm kiếm, đặt lịch, huỷ lịch, check-in/check-out, xem lịch sử sử dụng. |

## 2. Yêu cầu chức năng chi tiết

### 2.1. Danh mục phòng/thiết bị & lịch khả dụng

- Quản lý danh mục phòng thí nghiệm/thiết bị: CRUD, thông số kỹ thuật, hình ảnh minh hoạ, quy định sử dụng đi kèm.
- Hiển thị lịch khả dụng dạng calendar theo ngày/tuần/tháng, phân biệt trạng thái: trống, đã đặt, đang bảo trì.
- Tìm kiếm/lọc phòng/thiết bị theo loại, khoa/bộ môn, khung giờ trống.

### 2.2. Đặt lịch & Quy trình duyệt

- Tạo yêu cầu đặt lịch: chọn phòng/thiết bị, khung giờ, mục đích sử dụng, thời lượng.
- Tự động phát hiện xung đột lịch (cùng phòng/thiết bị, khung giờ chồng lấn) và đề xuất tối thiểu 3 khung giờ thay thế gần nhất.
- Quy trình duyệt/từ chối theo vai trò (Lab Manager phụ trách phòng); áp dụng quy tắc ưu tiên đã cấu hình (đề tài nghiên cứu > môn học > tự học) khi có tranh chấp khung giờ.
- Hàng đợi chờ (waitlist): khi khung giờ kín, yêu cầu được xếp hàng và tự động thông báo khi có chỗ trống hoặc có huỷ lịch.
- Huỷ lịch: Requester có thể huỷ lịch đã đặt trước thời điểm sử dụng theo chính sách huỷ (VD: tối thiểu trước X giờ).

### 2.3. Theo dõi sử dụng & bảo trì

- Check-in khi bắt đầu sử dụng, check-out khi kết thúc; ghi nhận thời gian sử dụng thực tế so với lịch đã đặt.
- Ghi nhận sự cố/hư hỏng thiết bị kèm mô tả, hình ảnh minh chứng (nếu có) sau khi sử dụng.
- Lập lịch bảo trì định kỳ; hệ thống tự động khoá khung giờ đặt lịch trong thời gian bảo trì.

### 2.4. Xử lý vi phạm

- Ghi nhận vi phạm: trả trễ (check-out muộn so với lịch), không đến nhận phòng (no-show).
- Cơ chế hạn chế quyền đặt lịch tạm thời khi người dùng vi phạm vượt ngưỡng cho phép trong khoảng thời gian xác định.

### 2.5. Báo cáo & Thống kê

- Dashboard tỷ lệ sử dụng theo phòng/thiết bị, theo khoa/bộ môn, theo khoảng thời gian.
- Báo cáo lịch sử bảo trì và chi phí phát sinh (nếu có) phục vụ ra quyết định đầu tư.

## 3. Yêu cầu phi chức năng

- **Toàn vẹn dữ liệu**: không cho phép hai lịch trùng khung giờ trên cùng một phòng/thiết bị — ràng buộc ở tầng dữ liệu (database constraint), không chỉ ở tầng ứng dụng.
- **Hiệu năng**: giao diện calendar phản hồi nhanh (< 1s cho thao tác xem lịch thông thường), hoạt động tốt trên cả desktop và mobile.
- **Thông báo**: nhắc lịch tự động trước giờ sử dụng (email/in-app).
- **Bảo mật & phân quyền**: JWT access + refresh token, phân quyền rõ ràng theo Role/Claim giữa Admin, Lab Manager, Requester.
- **Khả năng mở rộng**: kiến trúc N-layer/Clean Architecture cho phép bổ sung tính năng nâng cao (đồng bộ Google Calendar, real-time notification) ở Sprint 3.

## 4. Product Backlog — Toàn dự án

SP = Story Point (ước lượng độ phức tạp tương đối).

| ID | User Story | Vai trò | Ưu tiên | Sprint | SP |
|----|------------|---------|---------|--------|-----|
| US-01 | Là Admin, tôi muốn quản lý danh mục phòng/thiết bị (thêm/sửa/xoá, thông số, hình ảnh, quy định sử dụng) để dữ liệu phòng/thiết bị luôn cập nhật. | Admin | Cao | 2 | 5 |
| US-02 | Là Requester, tôi muốn xem lịch khả dụng của phòng/thiết bị theo ngày/tuần/tháng để chọn thời điểm phù hợp. | Requester | Cao | 2 | 5 |
| US-03 | Là Requester, tôi muốn tạo yêu cầu đặt lịch kèm mục đích sử dụng và thời lượng để gửi cho Lab Manager duyệt. | Requester | Cao | 2 | 5 |
| US-04 | Là hệ thống, tôi muốn tự động phát hiện xung đột lịch và đề xuất khung giờ thay thế để tránh trùng lịch. | Hệ thống | Cao | 2 | 8 |
| US-05 | Là Lab Manager, tôi muốn duyệt/từ chối yêu cầu đặt lịch theo quy tắc ưu tiên đã cấu hình. | Lab Manager | Cao | 2 | 5 |
| US-06 | Là Admin, tôi muốn cấu hình quy tắc ưu tiên đặt lịch (đề tài nghiên cứu > môn học > tự học) để hệ thống tự áp dụng khi duyệt. | Admin | Trung bình | 2 | 3 |
| US-07 | Là Requester, tôi muốn được xếp vào hàng đợi (waitlist) khi khung giờ đã kín và nhận thông báo khi có chỗ trống. | Requester | Trung bình | 3 | 5 |
| US-08 | Là Requester, tôi muốn check-in/check-out khi bắt đầu và kết thúc sử dụng để hệ thống ghi nhận thời gian sử dụng thực tế. | Requester | Cao | 2 | 5 |
| US-09 | Là Requester, tôi muốn ghi nhận sự cố/hư hỏng thiết bị sau khi sử dụng để Lab Manager xử lý kịp thời. | Requester | Trung bình | 2 | 3 |
| US-10 | Là Lab Manager, tôi muốn lên lịch bảo trì định kỳ và hệ thống tự động khoá lịch đặt trong thời gian đó. | Lab Manager | Cao | 2 | 5 |
| US-11 | Là hệ thống, tôi muốn ghi nhận vi phạm (trả trễ, no-show) để làm căn cứ hạn chế quyền đặt lịch. | Hệ thống | Trung bình | 3 | 3 |
| US-12 | Là Admin, tôi muốn hạn chế quyền đặt lịch tạm thời với người dùng vi phạm nhiều lần. | Admin | Trung bình | 3 | 3 |
| US-13 | Là Lab Manager, tôi muốn xem dashboard tỷ lệ sử dụng theo phòng/thiết bị, theo khoa/bộ môn để đánh giá hiệu quả. | Lab Manager | Trung bình | 3 | 5 |
| US-14 | Là Admin, tôi muốn xem báo cáo lịch sử bảo trì và chi phí phát sinh để hỗ trợ ra quyết định đầu tư. | Admin | Thấp | 3 | 3 |
| US-15 | Là người dùng bất kỳ, tôi muốn đăng nhập/đăng ký với phân quyền theo vai trò (JWT) để đảm bảo an toàn hệ thống. | Tất cả | Cao | 1 | 5 |
| US-16 | Là người dùng, tôi muốn nhận thông báo nhắc lịch trước giờ sử dụng để không bỏ lỡ lịch đã đặt. | Requester | Trung bình | 3 | 3 |

## 5. Sprint 1 Backlog — Tuần 1 & 2

Mục tiêu Sprint 1: hoàn thiện phân tích, thiết kế nền tảng và khởi tạo mã nguồn. Các hạng mục là Task/Story kỹ thuật.

| ID | User Story | Vai trò | Ưu tiên | Sprint | SP |
|----|------------|---------|---------|--------|-----|
| S1-01 | Hoàn thiện tài liệu SRS & Product Backlog để thống nhất phạm vi dự án. | Cả nhóm (BA) | Cao | 1 | 3 |
| S1-02 | Thiết kế ERD mô tả toàn bộ thực thể nghiệp vụ để làm nền tảng cơ sở dữ liệu. | Backend Dev | Cao | 1 | 5 |
| S1-03 | Thiết kế API contract (OpenAPI/Swagger) cho các module cốt lõi để Frontend/Backend phát triển song song. | Backend Dev | Cao | 1 | 5 |
| S1-04 | Wireframe/mockup UI cho các màn hình chính (đăng nhập, danh mục phòng/thiết bị, calendar, đặt lịch, duyệt yêu cầu, dashboard). | BA/UI-UX | Cao | 1 | 5 |
| S1-05 | Khởi tạo cấu trúc dự án Backend (.NET, N-layer/Clean Architecture) chạy được ở trạng thái rỗng. | Backend Dev | Cao | 1 | 3 |
| S1-06 | Khởi tạo cấu trúc dự án Frontend (Vue + TypeScript) chạy được ở trạng thái rỗng. | Frontend Dev | Cao | 1 | 3 |
| S1-07 | Thiết lập xác thực JWT cơ bản (đăng ký/đăng nhập, access + refresh token) ở cả hai tầng. | Backend Dev | Cao | 1 | 5 |
| S1-08 | Thiết lập repository Git với quy ước nhánh (main/dev/feature) và cấu hình CI cơ bản. | QA/DevOps | Trung bình | 1 | 2 |

## 6. Định nghĩa hoàn thành (Definition of Done) — Sprint 1

- Tài liệu SRS & Product Backlog được toàn nhóm thống nhất và lưu trong repository.
- ERD được review bởi Backend Dev, không còn xung đột logic giữa các thực thể.
- API contract phủ đủ các endpoint cốt lõi của module Danh mục, Đặt lịch, Xác thực; định dạng OpenAPI hợp lệ.
- Mockup UI được BA/UI-UX trình bày và nhóm thống nhất trước khi chuyển sang Sprint 2.
- Repository Backend và Frontend khởi tạo thành công, build/run được ở trạng thái rỗng, có README hướng dẫn chạy.
- Xác thực JWT cơ bản hoạt động: đăng ký, đăng nhập, cấp access token + refresh token.
- Quy ước nhánh Git (main/dev/feature) được thiết lập và tài liệu hoá.

## 7. Sản phẩm bàn giao Sprint 1

- Tài liệu đặc tả yêu cầu (SRS) & Product Backlog — tài liệu này.
- Sơ đồ ERD, API contract nháp.
- Bộ mockup UI cho các màn hình chính.
- Repository khởi tạo, cấu trúc project chạy được (empty-state).
