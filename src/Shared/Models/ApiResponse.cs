namespace Shared.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? new() };
    }
}
