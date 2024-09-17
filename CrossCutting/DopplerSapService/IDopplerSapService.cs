using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;

namespace CrossCutting.DopplerSapService
{
    public interface IDopplerSapService
    {
        public Task<HttpResponseMessage> SendCurrency(IList<CurrencyResponse> currencyList);
        public Task<HttpResponseMessage> SendUserBillings(IList<BillingRequest> userBillingList);

    }
}
