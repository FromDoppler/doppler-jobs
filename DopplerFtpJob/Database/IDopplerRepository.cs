using System.Threading.Tasks;

namespace Doppler.UpdateCredtiCardAccount.Job.Database
{
    public interface IDopplerRepository
    {
        Task UpdateUser(int userId);
        Task UpdateBillingCredits(int billingCreditId);
    }
}
