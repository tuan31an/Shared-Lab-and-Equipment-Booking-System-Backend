using LabBooking.API.Models;
using LabBooking.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace LabBooking.API.Common
{
    /// <summary>
    /// Bắt MỌI exception chưa được xử lý và trả về đúng envelope ApiResponse.
    /// Nhờ đó client luôn nhận cùng một hình dạng kể cả khi có lỗi.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, messages) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, new[] { exception.Message }),
                ConflictException => (HttpStatusCode.Conflict, new[] { exception.Message }),
                UnauthorizedException => (
                    httpContext.User.Identity?.IsAuthenticated == true
                        ? HttpStatusCode.Forbidden
                        : HttpStatusCode.Unauthorized,
                    new[] { exception.Message }),
                // Trigger DB chặn chồng lấn: EF bọc SqlException trong DbUpdateException khi SaveChanges.
                DbUpdateException { InnerException: SqlException { Number: 50001 or 50002 or 51001 or 51002 } sqlEx } => (HttpStatusCode.Conflict, new[] { sqlEx.Message }),
                // Unique/index violation → xung đột dữ liệu thật (2601 = unique index, 2627 = unique constraint).
                DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (HttpStatusCode.Conflict, new[] { "The request conflicts with existing database data." }),
                // DbUpdateException khác (truncation, null-violation, ...) không phải xung đột — đừng ngụy trang.
                DbUpdateException => (HttpStatusCode.InternalServerError, new[] { "An unexpected error occurred." }),
                // Domain ném ArgumentException khi dữ liệu đầu vào không hợp lệ.
                ArgumentException => (HttpStatusCode.BadRequest, new[] { exception.Message }),
                // Trigger DB trong truy vấn trực tiếp (không qua SaveChanges).
                SqlException { Number: 50001 or 50002 or 51001 or 51002 } sqlEx => (HttpStatusCode.Conflict, new[] { sqlEx.Message }),
                _ => (HttpStatusCode.InternalServerError, new[] { "An unexpected error occurred." })
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                // Chỉ log chi tiết cho lỗi ngoài dự kiến; không rò rỉ thông tin ra client.
                _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
            }
            else if (exception is DbUpdateException)
            {
                _logger.LogWarning(exception, "Database update conflict while processing {Path}", httpContext.Request.Path);
            }

            var response = ApiResponse.Fail(statusCode, messages);
            if (exception is ConflictException conflict)
                response.Result = conflict.Payload;
            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
