namespace CleoAssignment.ApiService.Dto;

public record GetResponse<T>(bool Success, T ResourceData, ErrorType? ErrorType)
{
    public GetResponse(bool success, ErrorType? errorType)
        : this(success, default, errorType)
    {
    }
}