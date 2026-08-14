using System.Security.Claims;
using LabBooking.Application.Common;

namespace LabBooking.API.Common
{
    /// <summary>Đọc claim Sub/NameIdentifier và Role từ principal của yêu cầu hiện tại.</summary>
    public class CurrentUserService : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? principal?.FindFirst("sub")?.Value;
                return value != null && Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Role =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}