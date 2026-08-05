namespace LabBooking.Domain.Enums;

/// <summary>Vai trò người dùng — quyết định phân quyền theo Role/Claim khi phát hành JWT.</summary>
public enum UserRole
{
    Admin,
    LabManager,
    Requester
}
