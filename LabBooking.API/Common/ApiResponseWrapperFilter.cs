using LabBooking.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace LabBooking.API.Common
{
    /// <summary>
    /// Tự động bọc MỌI kết quả trả về của controller vào envelope ApiResponse.
    /// Nhờ đó controller chỉ cần trả về dữ liệu thô (Ok(data), Created(...), v.v.),
    /// không phải tự dựng ApiResponse ở từng action.
    /// </summary>
    public class ApiResponseWrapperFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            switch (context.Result)
            {
                // Đã là ApiResponse thì giữ nguyên (ví dụ output của GlobalExceptionHandler).
                case ObjectResult { Value: ApiResponse }:
                    break;

                // Có body: bọc lại Value, giữ nguyên loại result (để không mất header Location của Created...).
                case ObjectResult objectResult:
                    objectResult.Value = Build(objectResult.Value, objectResult.StatusCode ?? StatusCodes.Status200OK);
                    break;

                // Không có body (NoContent, StatusCode(...), NotFound() không kèm dữ liệu).
                case StatusCodeResult statusCodeResult:
                    context.Result = new ObjectResult(Build(null, statusCodeResult.StatusCode))
                    {
                        StatusCode = statusCodeResult.StatusCode
                    };
                    break;

                case EmptyResult:
                    context.Result = new ObjectResult(Build(null, StatusCodes.Status200OK))
                    {
                        StatusCode = StatusCodes.Status200OK
                    };
                    break;
            }

            await next();
        }

        private static ApiResponse Build(object? value, int statusCode)
        {
            if (statusCode is >= 200 and < 300)
            {
                return ApiResponse.Success(value, (HttpStatusCode)statusCode);
            }

            return ApiResponse.Fail((HttpStatusCode)statusCode, ExtractMessages(value, statusCode));
        }

        private static string[] ExtractMessages(object? value, int statusCode)
        {
            return value switch
            {
                // Lỗi validation tự động của [ApiController] (ModelState).
                ValidationProblemDetails vpd => vpd.Errors.SelectMany(kvp => kvp.Value).ToArray(),
                ProblemDetails pd => new[] { pd.Detail ?? pd.Title ?? DefaultMessage(statusCode) },
                string s => new[] { s },
                null => new[] { DefaultMessage(statusCode) },
                _ => new[] { value.ToString()! }
            };
        }

        private static string DefaultMessage(int statusCode) =>
            ((HttpStatusCode)statusCode).ToString();
    }
}
