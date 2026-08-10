# Shared Lab & Equipment Booking System — Backend

Backend ASP.NET Core Web API cho hệ thống đặt lịch phòng thí nghiệm và thiết bị dùng chung. Dự án dùng Clean Architecture, EF Core/SQL Server, MediatR, JWT + refresh token và Swagger/OpenAPI.

## Yêu cầu

- .NET 10 SDK
- SQL Server. Cấu hình mặc định dùng SQL Server LocalDB trên Windows.
- Chạy lệnh trong đúng thư mục chứa `LabBooking.slnx`.

## Chạy dự án

```powershell
cd Shared-Lab-and-Equipment-Booking-System-Backend
dotnet restore LabBooking.slnx
dotnet build LabBooking.slnx
dotnet run --project .\LabBooking.API\LabBooking.API.csproj
```

Swagger Development: `http://localhost:5103/swagger` hoặc `https://localhost:7191/swagger`.

Nếu dùng Linux/macOS hoặc SQL Server khác, đặt connection string bằng biến môi trường trước khi chạy:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=LabBookingDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True'
dotnet run --project ./LabBooking.API/LabBooking.API.csproj
```

Không chạy lệnh từ thư mục cha của repository. Nếu gặp lỗi “project file does not exist”, kiểm tra lại bằng:

```powershell
Get-ChildItem LabBooking.slnx, .\LabBooking.API\LabBooking.API.csproj
```

## Database và dữ liệu mẫu

Khi API khởi động, EF Core tự áp dụng migration. Dữ liệu mẫu chỉ bật trong môi trường `Development`.

Tài khoản mẫu:

- Admin: `admin@example.com`
- Lab Manager: `alice.manager@example.com`
- Requester không bị hạn chế đặt lịch: `diana.researcher@example.com`
- Mật khẩu chung: `ChangeMe123!`

Dữ liệu mẫu chỉ phục vụ phát triển. Hãy tắt `SeedData:Enabled` và cấu hình `Jwt__Key` riêng khi triển khai.

## Kiểm tra

```powershell
dotnet test LabBooking.slnx
```

## Cấu trúc

- `LabBooking.Domain`: entity, enum, interface và logic lịch thuần.
- `LabBooking.Application`: command/query, validation và DTO.
- `LabBooking.Infrastructure.Sqlserver`: EF Core, migration, repository, JWT và seed.
- `LabBooking.API`: controller, authentication, exception handling, CORS và Swagger.
- `LabBooking.Tests`: unit test cho logic xếp lịch.

## Cấu hình triển khai

Không lưu khóa JWT hoặc mật khẩu database thật vào Git. Cấu hình tối thiểu qua biến môi trường:

```text
ConnectionStrings__DefaultConnection=...
Jwt__Key=at-least-32-bytes-of-random-secret-data
Jwt__Issuer=LabBooking.API
Jwt__Audience=LabBookingClients
Cors__AllowedOrigins__0=https://your-frontend.example.com
SeedData__Enabled=false
```
