using Doppler.Billing.Job.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.Billing.Job.Database
{
    public interface IDopplerRepository
    {
        public Task<IList<UserBilling>> GetUserBillingInformation(List<string> storedProcedures);
        public Task<IEnumerable<UserAddOn>> GetUserAddOnsByUserIdAsync(int userId);
        public Task<UserAddOn> GetUserAddOnsByUserIdAndTypeAsync(int userId, int addOnType);
        public Task<IEnumerable<LandingPlanUser>> GetLandingPlansByBillingCreditIdAsync(int billingCreditId);
        public Task<BillingCredit> GetCurrentBIllingCreditByUserIdAsync(int userId);
        public Task<ChatPlanUser> GetActiveChatPlanByIdBillingCredit(int currentChatBillingCreditId);
    }
}
