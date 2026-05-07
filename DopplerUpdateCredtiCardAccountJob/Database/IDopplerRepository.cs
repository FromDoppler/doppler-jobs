using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.UpdateCredtiCardAccount.Job.Entities;

namespace Doppler.UpdateCredtiCardAccount.Job.Database
{
    public interface IDopplerRepository
    {
        Task UpdateUser(int userId);
        Task UpdateBillingCredits(int billingCreditId);
        Task<IEnumerable<CreditCardData>> GetCreditCardsForComericaUpdate();
    }
}
