using Doppler.SurplusAddOn.Job.Entities;

namespace Doppler.SurplusAddOn.Job.Database
{
    public interface IDopplerRepository
    {
        Task<IList<OnSiteAddon>> GetUsersWithActiveOnsitePlanAsync();
        Task<OnSitePlan> GetActiveOnsitePlanByUserIdAsync(int userId);
        Task InsertSurplusAddOnAsync(int userId, int addOnTypeId, DateTime date, string period, int quantity, decimal additionalPrice, decimal total);
    }
}
