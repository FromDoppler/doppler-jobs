using Doppler.SurplusAddOn.Job.Entities;

namespace Doppler.SurplusAddOn.Job.Database
{
    public interface IDopplerRepository
    {
        Task<IList<UserAddOn>> GetUsersWithActiveAddOnByAddOnTypeAsync(int addOnTypeId);
        Task<Entities.SurplusAddOn> GetByUserIdAddOnTypeIdAndPeridoAsync(int userId, int addOnTypeId, string period);
        Task InsertSurplusAddOnAsync(int userId, int addOnTypeId, DateTime date, string period, int quantity, decimal additionalPrice, decimal total);
        Task UpdateSurplusAddOnAsync(int userId, int addOnTypeId, DateTime date, string period, int quantity, decimal additionalPrice, decimal total);
    }
}
