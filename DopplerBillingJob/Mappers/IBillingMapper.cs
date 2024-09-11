using CrossCutting.DopplerSapService.Entities;
using Doppler.Billing.Job.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.Billing.Job.Mappers
{
    public interface IBillingMapper
    {
        public Task<IList<BillingRequest>> MapToListOfBillingRequest(IList<UserBilling> userBillings);
    }
}
