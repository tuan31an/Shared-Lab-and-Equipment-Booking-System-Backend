using LabBooking.API;
using LabBooking.API.Common;
using LabBooking.API.Models;
using LabBooking.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LabBooking.Tests;

public class ApiResponseWrapperFilterTests
{
    private static async Task<IReadOnlyList<(ApiResponse? Wrapped, int StatusCode)>> Run(params (object? Value, int? StatusCode)[] results)
    {
        var filter = new ApiResponseWrapperFilter();
        var outputs = new List<(ApiResponse?, int)>();
        foreach (var (value, statusCode) in results)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = statusCode.HasValue
                ? new ObjectResult(value) { StatusCode = statusCode }
                : new ObjectResult(value);
            var filters = new List<IFilterMetadata>();
            var executed = new ResultExecutedContext(actionContext, filters, result, null!);
            var context = new ResultExecutingContext(actionContext, filters, result, null!);
            await filter.OnResultExecutionAsync(context, () => Task.FromResult(executed));
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(context.Result);
            outputs.Add((objectResult.Value as ApiResponse, objectResult.StatusCode ?? 200));
        }
        return outputs;
    }

    [Fact]
    public async Task Success_Result_Is_Wrapped_Into_ApiResponse()
    {
        var responses = await Run(("hello", 200));
        var (wrapped, status) = responses[0];

        Assert.NotNull(wrapped);
        Assert.True(wrapped!.IsSuccess);
        Assert.Equal("hello", wrapped.Result);
        Assert.Equal(HttpStatusCode.OK, wrapped.StatusCode);
        Assert.Equal(200, status);
    }

    [Fact]
    public async Task Created_Keeps_Its_StatusCode_And_Wraps_Value()
    {
        var responses = await Run(("resource", (int)HttpStatusCode.Created));
        var (wrapped, status) = responses[0];

        Assert.True(wrapped!.IsSuccess);
        Assert.Equal("resource", wrapped.Result);
        Assert.Equal(201, status);
        Assert.Equal(HttpStatusCode.Created, wrapped.StatusCode);
    }

    [Fact]
    public async Task ObjectResult_With_ApiResponse_Is_Left_Untouched()
    {
        var original = ApiResponse.Fail(HttpStatusCode.Conflict, "boom");
        var responses = await Run((original, 409));
        var (wrapped, status) = responses[0];

        Assert.Same(original, wrapped);
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Failed_Validation_ProblemDetails_Messages_Are_Extracted()
    {
        var problem = new ValidationProblemDetails
        {
            Errors = { ["name"] = new[] { "Name is required." }, ["age"] = new[] { "Too old." } }
        };
        var responses = await Run((problem, (int)HttpStatusCode.BadRequest));
        var (wrapped, _) = responses[0];

        Assert.False(wrapped!.IsSuccess);
        Assert.Equal(new[] { "Name is required.", "Too old." }, wrapped.ErrorMessages);
    }

    [Fact]
    public async Task Failed_ProblemDetails_Message_Is_Derived_From_Detail()
    {
        var problem = new ProblemDetails { Detail = "Something went wrong", Status = 400 };
        var responses = await Run((problem, 400));
        var (wrapped, _) = responses[0];

        Assert.False(wrapped!.IsSuccess);
        Assert.Equal(new[] { "Something went wrong" }, wrapped.ErrorMessages);
    }

    [Fact]
    public async Task Failed_String_Result_Becomes_Error_Message()
    {
        var responses = await Run(("oops", 400));
        var (wrapped, _) = responses[0];

        Assert.False(wrapped!.IsSuccess);
        Assert.Equal(new[] { "oops" }, wrapped.ErrorMessages);
    }

    [Fact]
    public async Task StatusCode_204_Result_Is_Not_Wrapped()
    {
        var response = await RunNoContent(new NoContentResult());

        Assert.Equal(204, response.StatusCode);
        Assert.Null(response.Wrapped);
    }

    private static async Task<(ApiResponse? Wrapped, int StatusCode)> RunNoContent(StatusCodeResult result)
    {
        var filter = new ApiResponseWrapperFilter();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var executed = new ResultExecutedContext(actionContext, filters, result, null!);
        var context = new ResultExecutingContext(actionContext, filters, result, null!);
        await filter.OnResultExecutionAsync(context, () => Task.FromResult(executed));

        if (context.Result is ObjectResult objectResult)
            return (objectResult.Value as ApiResponse, objectResult.StatusCode ?? 200);

        var status = context.Result is StatusCodeResult statusCodeResult ? statusCodeResult.StatusCode : 200;
        return (null, status);
    }
}

public class UtcDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = new();

    static UtcDateTimeConverterTests()
    {
        Options.Converters.Add(new UtcDateTimeConverter());
    }

    [Fact]
    public void Read_Offset_Converts_To_Utc()
    {
        var expected = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(7)).UtcDateTime;
        var result = JsonSerializer.Deserialize<DateTime>("\"2026-01-01T12:00:00+07:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Read_Naive_Treated_As_Utc_Not_Local()
    {
        var result = JsonSerializer.Deserialize<DateTime>("\"2026-01-01T12:00:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(12, result.Hour);
    }

    [Fact]
    public void Write_Unspecified_Does_Not_Shift()
    {
        var dbValue = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal("\"2026-01-01T10:00:00Z\"", JsonSerializer.Serialize(dbValue, Options));
    }

    [Fact]
    public void Write_Local_Converts_To_Utc()
    {
        var local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Equal(
            "\"" + local.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"",
            JsonSerializer.Serialize(local, Options));
    }
}

public class GlobalExceptionHandlerTests
{
    private static async Task<(int StatusCode, ApiResponse? Api)> Handle(Exception exception)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        return (httpContext.Response.StatusCode, JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
    }

    [Fact]
    public async Task NotFound_Maps_To_404_With_Message()
    {
        var (status, api) = await Handle(new NotFoundException("missing"));

        Assert.Equal(404, status);
        Assert.False(api!.IsSuccess);
        Assert.Equal(new[] { "missing" }, api.ErrorMessages);
    }

    [Fact]
    public async Task Unauthorized_Maps_To_401()
    {
        var (status, api) = await Handle(new UnauthorizedException("no auth"));

        Assert.Equal(401, status);
        Assert.Equal(new[] { "no auth" }, api!.ErrorMessages);
    }

    [Fact]
    public async Task Argument_Maps_To_400()
    {
        var (status, api) = await Handle(new ArgumentException("bad input"));

        Assert.Equal(400, status);
        Assert.Equal(new[] { "bad input" }, api!.ErrorMessages);
    }

    [Fact]
    public async Task Conflict_Carries_Payload()
    {
        var payload = new { suggested = new[] { "slot" } };
        var (status, api) = await Handle(new ConflictException("conflict", payload));

        Assert.Equal(409, status);
        Assert.Equal(new[] { "conflict" }, api!.ErrorMessages);
        var resultElement = Assert.IsAssignableFrom<JsonElement>(api.Result);
        Assert.Equal(payload.suggested[0], resultElement.GetProperty("suggested")[0].GetString());
    }

    [Fact]
    public async Task Unknown_Exception_Is_500_Without_Leaking_Details()
    {
        var (status, api) = await Handle(new InvalidOperationException("secret internal detail"));

        Assert.Equal(500, status);
        Assert.Equal(new[] { "An unexpected error occurred." }, api!.ErrorMessages);
    }
}