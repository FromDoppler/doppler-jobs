namespace Doppler.SurplusAddOn.Job.Entities
{
    public class UserAddOn
    {
        public int UserId {  get; set; }
        public string Email { get; set; }
        public int Qty { get; set; }
        public decimal AdditionalPrice { get; set; }
    }
}
