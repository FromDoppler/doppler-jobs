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
        public Task<OnSitePlanUser> GetActiveOnSitePlanByIdBillingCredit(int currentOnSiteBillingCreditId);
        public Task<IList<int>> GetUserIdsByClientManagerIdAsync(int clientManagerId);
        public Task<decimal> GetCurrenyRate(int from, int to);
        public Task<User> GetUserByUserIdAsync(int userId);
    }
}
