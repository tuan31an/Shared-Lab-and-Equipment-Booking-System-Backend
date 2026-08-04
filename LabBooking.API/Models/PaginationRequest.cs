using System.ComponentModel.DataAnnotations;

namespace LabBooking.API.Models
{
    /// <summary>
    /// Tham số phân trang Client gửi lên (thường qua query string: ?pageNumber=1&pageSize=10).
    /// </summary>
    public class PaginationRequest
    {
        private const int MaxPageSize = 100;

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
        public int PageNumber { get; set; } = 1;

        // Chặn trên để tránh Client yêu cầu trang quá lớn: vượt ngưỡng sẽ trả về 400 (validation),
        // thay vì âm thầm cắt giá trị (khiến [Range] vô nghĩa và Client không biết yêu cầu bị đổi).
        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    }
}
