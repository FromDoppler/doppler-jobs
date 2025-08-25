using Dapper;
using Doppler.CancelAccountWithScheduleCancellation.Job.Entities;
using Doppler.Database;
using Microsoft.Extensions.Logging;

namespace Doppler.CancelAccountWithScheduleCancellation.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly ILogger<DopplerRepository> logger;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public DopplerRepository(
            ILogger<DopplerRepository> logger,
            IDbConnectionFactory dbConnectionFactory)
        {
            this.logger = logger;
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<UserAccountCancellationRequest> GetLastAccountCancellationRequestByUserIdAsync(int userId)
        {
            logger.LogInformation($@"Getting account cancellation request for the user: {userId}");

            await using var conn = dbConnectionFactory.GetConnection();
            var query = $@"SELECT [IdUser] AS UserId, [AccountCancellatioReason]
                            FROM [dbo].[UserAccountCancellationRequest]
                            WHERE IdUser = {userId}
                            ORDER BY [CreatedAt] DESC";

            var userAccountCancellationRequest = await conn.QueryFirstOrDefaultAsync<UserAccountCancellationRequest>(query, commandTimeout: 90);

            return userAccountCancellationRequest;
        }

        public async Task<IList<User>> GetUserWithScheduleCancellationAsync()
        {
            logger.LogInformation("Getting users to cancel");

            await using var conn = dbConnectionFactory.GetConnection();
            var query = $@"SELECT IdUser AS UserId, Email, FirstName, LastName, IdUserAccountCancellationReason AS UserAccountCancellationReasonId
                            FROM [dbo].[User]
                            WHERE HasScheduledCancellation = 1";

            var usersToCancel = await conn.QueryAsync<User>(query, commandTimeout: 90);

            return [.. usersToCancel];
        }

        public async Task UnsetSetScheduleCancellationAsync(int userId)
        {
            logger.LogInformation("Getting users to cancel");

            await using var conn = dbConnectionFactory.GetConnection();
            var query = $@"UPDATE [dbo].[User]
                           SET HasScheduledCancellation = 0
                           WHERE IdUser = {userId}";

            await conn.ExecuteAsync(query, commandTimeout: 90);
        }
    }
}
