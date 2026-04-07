namespace Doppler.UpdateCredtiCardAccount.Job.Entities;

public enum EchoValidationStatus
{
    NotFound,
    Success,
    Failed,
    InvalidFormat
}

public class EchoValidationResult
{
    public EchoValidationStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}
