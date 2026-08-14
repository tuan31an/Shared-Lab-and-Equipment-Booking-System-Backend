using LabBooking.API.Common;
using LabBooking.Application.Common;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabBooking.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            // Register API controllers + tự động bọc mọi kết quả vào ApiResponse.
            services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseWrapperFilter>();
            })
            .AddJsonOptions(options =>
            {
                // Mọi DateTime từ body chuẩn hoá về UTC trước khi vào handler,
                // tránh so sánh nhầm giữa giờ client (naive/local) với DateTime.UtcNow.
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                // Enum nhận dạng tên string ("Admin", "Active") thay vì số 0/1/2 — khớp FE gửi.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUserService>();

            // Chuyển mọi exception chưa xử lý thành envelope ApiResponse.
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            // Set up Mapster configurations for API
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());

            return services;
        }
    }

    /// <summary>Chuẩn hoá DateTime từ JSON body về UTC: Z giữ nguyên, offset/local đổi sang UTC, naive coi là UTC.</summary>
    internal sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}