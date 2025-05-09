namespace Doppler.Billing.Job.Entities
{
    public class AddOnPlanUser
    {
        public int IdUser { get; set; }
        public int IdBillingCredit { get; set; }
        public int IdAddOnPlan {  get; set; }
        public decimal Additional {  get; set; }
        public decimal Fee { get; set; }
        public int Quantity {  get; set; }
        public bool IsCustom { get; set; }
    }
}
