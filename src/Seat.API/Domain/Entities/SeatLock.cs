namespace Seat.API.Domain.Entities;

public record LockSeatResult
{
    public bool IsSuccess {get; set;}
    public string LockToken {get; set;}
    public DateTime LockExpiration {get; set;}

    public LockSeatResult(bool isSuccess, string lockToken, DateTime lockExpiration)
    {
        IsSuccess = isSuccess;
        LockToken = lockToken;
        LockExpiration = lockExpiration;
    }
}