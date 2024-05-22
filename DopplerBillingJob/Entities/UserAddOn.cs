namespace Doppler.Billing.Job.Entities
{
    public class UserAddOn
    {
        public int IdUserAddOn { get; set; }
        public int IdUser { get; set; }
        public int IdAddOnType { get; set; }
        public int IdCurrentBillingCredit { get; set; }
    }
}
