namespace VendingStock.Api.Common;
public sealed record ApiResponse(int Code, string Message, object? Data)
{
    public static ApiResponse Ok(object? data = null, string message = "success") => new(200, message, data);
    public static ApiResponse Fail(string message, int code) => new(code, message, null);
}
public sealed record PagedResult<T>(IReadOnlyCollection<T> List, int Total, int Page, int PageSize);
public sealed class BusinessException(string message, int code = 4000) : Exception(message) { public int Code { get; } = code; }
