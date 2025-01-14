namespace Doppler.Billing.Job.Entities
{
    public class OnSitePlanUser
    {
        public int IdUser { get; set; }
        public int IdBillingCredit { get; set; }
        public int IdOnSitePlan {  get; set; }
        public decimal AdditionalPrint {  get; set; }
        public decimal Fee { get; set; }
        public int PrintQty {  get; set; }
        public bool IsCustom { get; set; }
    }
}
