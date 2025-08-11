namespace domain.Common;

/// <summary>
/// Generic API response wrapper for all API endpoints
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The actual data being returned (null for error responses)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// List of error messages (null or empty for successful responses)
    /// </summary>
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata (optional)
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    // Constructors
    public ApiResponse()
    {
    }

    public ApiResponse(T data, string message = "Success", int statusCode = 200)
    {
        Success = true;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }

    public ApiResponse(string message, int statusCode, IEnumerable<string>? errors = null)
    {
        Success = false;
        Message = message;
        StatusCode = statusCode;
        Errors = errors;
        Data = default;
    }

    // Static factory methods for common responses
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>(data, message, 200);
    }

    public static ApiResponse<T> CreatedResponse(T data, string message = "Resource created successfully")
    {
        return new ApiResponse<T>(data, message, 201);
    }

    public static ApiResponse<T> NotFoundResponse(string message = "Resource not found")
    {
        return new ApiResponse<T>(message, 404);
    }

    public static ApiResponse<T> BadRequestResponse(string message = "Bad request", IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>(message, 400, errors);
    }

    public static ApiResponse<T> UnauthorizedResponse(string message = "Unauthorized access")
    {
        return new ApiResponse<T>(message, 401);
    }

    public static ApiResponse<T> InternalServerErrorResponse(string message = "An internal server error occurred")
    {
        return new ApiResponse<T>(message, 500);
    }

    public static ApiResponse<T> ValidationErrorResponse(IEnumerable<string> errors)
    {
        return new ApiResponse<T>("Validation failed", 400, errors);
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public ApiResponse() : base()
    {
    }

    public ApiResponse(string message, int statusCode, IEnumerable<string>? errors = null)
        : base(message, statusCode, errors)
    {
    }

    // Static factory methods for non-generic responses
    public static ApiResponse SuccessResponse(string message = "Operation completed successfully")
    {
        return new ApiResponse { Success = true, Message = message, StatusCode = 200 };
    }

    public static ApiResponse CreatedResponse(string message = "Resource created successfully")
    {
        return new ApiResponse { Success = true, Message = message, StatusCode = 201 };
    }

    public static ApiResponse NotFoundResponse(string message = "Resource not found")
    {
        return new ApiResponse(message, 404);
    }

    public static ApiResponse BadRequestResponse(string message = "Bad request", IEnumerable<string>? errors = null)
    {
        return new ApiResponse(message, 400, errors);
    }

    public static ApiResponse UnauthorizedResponse(string message = "Unauthorized access")
    {
        return new ApiResponse(message, 401);
    }

    public static ApiResponse InternalServerErrorResponse(string message = "An internal server error occurred")
    {
        return new ApiResponse(message, 500);
    }

    public static ApiResponse ValidationErrorResponse(IEnumerable<string> errors)
    {
        return new ApiResponse("Validation failed", 400, errors);
    }
}

/// <summary>
/// Paginated API response for list operations
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    public PaginatedApiResponse() : base()
    {
    }

    public PaginatedApiResponse(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalCount,
        string message = "Data retrieved successfully") : base(data, message, 200)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasNextPage = pageNumber < TotalPages;
        HasPreviousPage = pageNumber > 1;
    }

    public static PaginatedApiResponse<T> SuccessResponse(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalCount,
        string message = "Data retrieved successfully")
    {
        return new PaginatedApiResponse<T>(data, pageNumber, pageSize, totalCount, message);
    }
}