using Doppler.CancelAccountWithScheduleCancellation.Job.Entities;

namespace Doppler.CancelAccountWithScheduleCancellation.Job.Database
{
    public interface IDopplerRepository
    {
        public Task<IList<User>> GetUserWithScheduleCancellationAsync();
        public Task<UserAccountCancellationRequest> GetLastAccountCancellationRequestByUserIdAsync(int userId);
        Task UnsetSetScheduleCancellationAsync(int userId);
        Task<UserAccountCancellationReason> GetByIdAsync(int userAccountCancellationReasonId);
        Task<AccountCancellationReason> GetAccountCancellationReasonByIdAsync(int accountCancellationReasonId);
    }
}
