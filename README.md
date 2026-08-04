# LabBooking-CleanArchitecture

Nền tảng Đặt lịch & Quản lý Phòng thí nghiệm / Thiết bị Dùng chung (Đề tài 03) - **skeleton empty state**.

Clean Architecture (.NET 10) đủ 4 tầng, chưa có code nghiệp vụ:

- `LabBooking.Domain` - chỉ giữ `BaseEntity` + `PagedResult<T>` (nền tảng dùng chung).
- `LabBooking.Application` - `DependencyInjection` rỗng, chờ thêm use-case.
- `LabBooking.Infrastructure.Sqlserver` - `ApplicationDbContext` trống + `DependencyInjection`.
- `LabBooking.API` - `Program` tối thiểu + `AddControllers`, chưa có controller.

Chưa có: entity, migration, seed data, handler, controller, repository.

## Chạy

```
dotnet run --project LabBooking.API
```

Hướng phát triển tiếp: thêm entity vào Domain → DbSet + configuration vào Infrastructure → migration → handler/use-case vào Application → controller vào API.
