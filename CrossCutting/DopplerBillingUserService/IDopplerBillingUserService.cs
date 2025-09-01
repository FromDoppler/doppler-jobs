using System;
using System.Threading.Tasks;

namespace CrossCutting.DopplerBillingUserService
{
    public interface IDopplerBillingUserService
    {
        public Task<bool> CancelAccountAsync(string accountName, string cancellationReason);
    }
}
