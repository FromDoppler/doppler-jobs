using System;

namespace Doppler.Billing.Job.Entities
{
    public class SurplusAddOn
    {
        public int IdUser { get; set; }
        public int IdAddOnType { get; set; }
        public DateTime Date { get; set; }
        public string Period { get; set; }
        public int Quantity { get; set; }
        public decimal AdditionalPrice { get; set; }
        public decimal Total { get; set; }

    }
}
