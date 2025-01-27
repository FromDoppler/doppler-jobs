using Dapper;
using Doppler.Database;
using Doppler.SurplusAddOn.Job.Entities;
using Microsoft.Extensions.Logging;

namespace Doppler.SurplusAddOn.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly ILogger<DopplerRepository> logger;
        private readonly IDbConnectionFactory dbConnectionFactory;

        public DopplerRepository(
            ILogger<DopplerRepository> dopplerRepositoryLogger,
            IDbConnectionFactory dbConnectionFactory)
        {
            this.logger = dopplerRepositoryLogger;
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IList<OnSiteAddon>> GetUsersWithActiveOnsitePlanAsync()
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();
                var query = @$"SELECT UA.IdUser AS UserId, U.Email
                                FROM [dbo].[UserAddOn] UA
                                INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
                                INNER JOIN [User] U ON U.IdUser = UA.IdUser
                                WHERE UA.IdAddOnType = 3 AND BC.IdBillingCreditType != 36";

                logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryAsync<OnSiteAddon>(query);

                return result.ToList();
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        public async Task<OnSitePlan> GetActiveOnsitePlanByUserIdAsync(int userId)
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();
                var query = @$"SELECT OSP.PrintQty, OSP.AdditionalPrint
                                FROM [dbo].[UserAddOn] UA
                                INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
                                INNER JOIN [User] U ON U.IdUser = UA.IdUser
                                INNER JOIN [OnSitePlanUser] OSPU ON OSPU.IdBillingCredit = UA.IdCurrentBillingCredit
                                INNER JOIN [OnSitePlan] OSP ON OSP.IdOnSitePlan = OSPU.IdOnSitePlan
                                WHERE UA.IdAddOnType = 3 AND BC.IdBillingCreditType != 36 AND UA.IdUser = {userId}";

                logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryFirstOrDefaultAsync<OnSitePlan>(query);

                return result;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        public async Task InsertSurplusAddOnAsync(int userId, int addOnTypeId, DateTime date, string period, int quantity, decimal additionalPrice, decimal total)
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();
                var query = @$"INSERT INTO [dbo].[SurplusAddOn] ([IdUser],[IdAddOnType],[Date], [Period],[Quantity],[AdditionalPrice],[Total])
                               VALUES ({userId},{addOnTypeId},'{date:yyyy-MM-dd}','{period}',{quantity},{additionalPrice.ToString().Replace(",",".")},{total.ToString().Replace(",", ".")})";

                logger.LogInformation("Sending SQL sentence to database server.");

                await conn.ExecuteAsync(query);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }
    }
}
