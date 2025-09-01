using Doppler.CancelAccountWithScheduleCancellation.Job.Entities;

namespace Doppler.CancelAccountWithScheduleCancellation.Job.Database
{
    public interface IDopplerRepository
    {
        public Task<IList<User>> GetUserWithScheduleCancellationAsync();
        public Task<UserAccountCancellationRequest> GetLastAccountCancellationRequestByUserIdAsync(int userId);
        Task UnsetSetScheduleCancellationAsync(int userId);
    }
}
