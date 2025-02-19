using Dapper;
using Doppler.Database;
using Doppler.SurplusAddOn.Job.Entities;
using Doppler.SurplusAddOn.Job.Enums;
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

        public async Task<IList<UserAddOn>> GetUsersWithActiveAddOnByAddOnTypeAsync(int addOnTypeId)
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();
                var query = string.Empty;

                switch ((AddOnTypeEnum)addOnTypeId)
                {
                    case AddOnTypeEnum.OnSite:
                        query = @$"SELECT UA.IdUser AS UserId, U.Email, OSP.PrintQty AS Qty, OSP.AdditionalPrint AS AdditionalPrice
                                FROM [dbo].[UserAddOn] UA
                                INNER JOIN [User] U ON U.IdUser = UA.IdUser
                                INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
                                INNER JOIN [OnSitePlanUser] OSPU ON OSPU.IdBillingCredit = BC.IdBillingCredit
                                INNER JOIN [OnSitePlan] OSP ON OSP.IdOnSitePlan = OSPU.IdOnSitePlan
                                WHERE UA.IdAddOnType = 3 AND BC.IdBillingCreditType != 36";
                        break;
                    case AddOnTypeEnum.Chat:
                        query = @$"SELECT UA.IdUser AS UserId, U.Email, CP.ConversationQty AS Qty, CP.AdditionalConversation AS AdditionalPrice
                                FROM [dbo].[UserAddOn] UA
                                INNER JOIN [User] U ON U.IdUser = UA.IdUser
                                INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
                                INNER JOIN [ChatPlanUsers] CPU ON CPU.IdBillingCredit = BC.IdBillingCredit
                                INNER JOIN [ChatPlans] CP ON CP.IdChatPlan = CPU.IdChatPlan
                                WHERE UA.IdAddOnType = 2 AND BC.IdBillingCreditType != 30";
                        break;
                    case AddOnTypeEnum.Landing:
                    default:
                        query = string.Empty;
                        break;
                }

                logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryAsync<UserAddOn>(query);

                return result.ToList();
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        public async Task<Entities.SurplusAddOn> GetByUserIdAddOnTypeIdAndPeridoAsync(int userId, int addOnTypeId, string period)
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();
                var query = @$"SELECT [IdSurplusAddOn],[IdUser],[IdAddOnType],[Date],[Period],[Quantity],[AdditionalPrice],[Total]
                               FROM [dbo].[SurplusAddOn]
                               WHERE IdAddOnType = {addOnTypeId} AND IdUser = {userId} AND Period = '{period}'";

                logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryFirstOrDefaultAsync<Entities.SurplusAddOn>(query);

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

        public async Task UpdateSurplusAddOnAsync(int userId, int addOnTypeId, DateTime date, string period, int quantity, decimal additionalPrice, decimal total)
        {
            logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = dbConnectionFactory.GetConnection();

                var query = $@"UPDATE [dbo].[SurplusAddOn]
                               SET [Date] = '{date:yyyy-MM-dd}'
                                  ,[Quantity] = {quantity}
                                  ,[AdditionalPrice] = {additionalPrice.ToString().Replace(",", ".")}
                                  ,[Total] = {total.ToString().Replace(",", ".")}
                               WHERE IdUser = {userId} AND [IdAddOnType] = {addOnTypeId} AND [Period] = '{period}'";

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
