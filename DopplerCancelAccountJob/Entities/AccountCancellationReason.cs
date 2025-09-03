namespace Doppler.CancelAccountWithScheduleCancellation.Job.Entities
{
    public class AccountCancellationReason
    {
        public int AccountCancellationReasonId { get; set; }
        public string Description { get; set; }
        public bool SendEmailToUser { get; set; }
        public bool Active { get; set; }
    }
}
