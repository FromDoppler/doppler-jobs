using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.Billing.Job.Entities
{
    public class BillingCredit
    {
        public int IdBillingCredit { get; set; }
        public decimal PlanFee { get; set; }
	    public int? TotalMonthPlan { get; set; }
    }
}
