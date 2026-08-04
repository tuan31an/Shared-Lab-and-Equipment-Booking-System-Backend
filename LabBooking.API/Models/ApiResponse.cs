using System.Net;
using System.Text.Json.Serialization;

namespace LabBooking.API.Models
{
    /// <summary>
    /// Envelope chuẩn cho MỌI response của API.
    /// Client luôn nhận được cùng một hình dạng: trạng thái, cờ thành công, danh sách lỗi và dữ liệu.
    /// </summary>
    public class ApiResponse
    {
        [JsonPropertyName("statusCode")]
        public HttpStatusCode StatusCode { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; } = true;

        [JsonPropertyName("errorMessages")]
        public List<string> ErrorMessages { get; set; } = new();

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        // ---- Factory helpers cho gọn ở controller ----

        public static ApiResponse Success(object? result, HttpStatusCode statusCode = HttpStatusCode.OK) => new()
        {
            StatusCode = statusCode,
            IsSuccess = true,
            Result = result
        };

        public static ApiResponse Fail(HttpStatusCode statusCode, params string[] errorMessages) => new()
        {
            StatusCode = statusCode,
            IsSuccess = false,
            ErrorMessages = new List<string>(errorMessages),
            Result = null
        };
    }
}
