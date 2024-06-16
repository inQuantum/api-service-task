namespace CleoAssignment.ApiService.Dto;

public enum ErrorType
{
    None,
    ResourceUpdateFailed,
    IpBanned,
    ThrottleLimitExceeded,
    SomethingWrong,
}
