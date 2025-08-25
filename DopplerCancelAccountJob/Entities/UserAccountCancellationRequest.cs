namespace Doppler.CancelAccountWithScheduleCancellation.Job.Entities
{
    public class UserAccountCancellationRequest
    {
        public int UserId { get; set; }
        public string AccountCancellatioReason { get; set; }
    }
}
