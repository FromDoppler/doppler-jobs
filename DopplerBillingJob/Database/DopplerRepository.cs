using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;
using CrossCutting.DopplerSapService.Enum;
using Dapper;
using Doppler.Billing.Job.Entities;
using Doppler.Billing.Job.Enums;
using Doppler.Database;
using Microsoft.Extensions.Logging;

namespace Doppler.Billing.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly ILogger<DopplerRepository> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DopplerRepository(
            ILogger<DopplerRepository> dopplerRepositoryLogger,
            IDbConnectionFactory dbConnectionFactory)
        {
            _logger = dopplerRepositoryLogger;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IList<UserBilling>> GetUserBillingInformation(List<string> storedProcedures)
        {
            _logger.LogInformation("Getting database connection.");
            var result = new List<UserBilling>();

            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();

                var query = string.Join("\n", storedProcedures);

                _logger.LogInformation("Sending SQL sentence to database server.");
                using var multiResult = await conn.QueryMultipleAsync(query, commandTimeout: 90);

                while (!multiResult.IsConsumed)
                {
                    result.AddRange(multiResult.Read<UserBilling>().ToList());
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        public async Task<IEnumerable<UserAddOn>> GetUserAddOnsByUserIdAsync(int userId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT [IdUserAddOn]
                                ,[IdUser]
                                ,[IdAddOnType]
                                ,[IdCurrentBillingCredit]
                            FROM [dbo].[UserAddOn]
                            WHERE [IdUser] = {userId}";

            var userAddOns = await conn.QueryAsync<UserAddOn>(query, commandTimeout: 90);

            return userAddOns;
        }

        public async Task<UserAddOn> GetUserAddOnsByUserIdAndTypeAsync(int userId, int addOnType)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT [IdUserAddOn]
                                ,[IdUser]
                                ,[IdAddOnType]
                                ,[IdCurrentBillingCredit]
                            FROM [dbo].[UserAddOn]
                            WHERE [IdUser] = {userId} AND [IdAddOnType] = {addOnType}";

            var userAddOns = await conn.QueryFirstOrDefaultAsync<UserAddOn>(query, commandTimeout: 90);

            return userAddOns;
        }

        public async Task<IEnumerable<LandingPlanUser>> GetLandingPlansByBillingCreditIdAsync(int billingCreditId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT [IdLandingPlanUser]
                            ,[IdUser]
                            ,[IdLandingPlan]
                            ,[IdBillingCredit]
                            ,[PackQty]
                            ,[Fee]
                            ,[CreatedAt]
                        FROM [dbo].[LandingPlanUser]
                        WHERE IdBillingCredit = {billingCreditId}";

            var landingPlans = await conn.QueryAsync<LandingPlanUser>(query, commandTimeout: 90);

            return landingPlans;
        }

        public async Task<BillingCredit> GetCurrentBIllingCreditByUserIdAsync(int userId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT IdBillingCredit,
                                PlanFee,
	                            TotalMonthPlan
                            FROM [dbo].[User] U
                            INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = U.IdCurrentBillingCredit
                            WHERE U.[IdUser] = {userId} ";

            var billingCredit = await conn.QueryFirstOrDefaultAsync<BillingCredit>(query, commandTimeout: 90);

            return billingCredit;
        }

        public async Task<ChatPlanUser> GetActiveChatPlanByIdBillingCredit(int currentChatBillingCreditId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT CPU.IdUser, CPU.IdBillingCredit, CP.IdChatPlan, CP.AdditionalConversation, CP.Fee, CP.ConversationQty, CP.Custom AS IsCustom
                            FROM [ChatPlanUsers] CPU
                            INNER JOIN [ChatPlans] CP ON CP.IdChatPlan = CPU.IdChatPlan
                            WHERE CPU.IdBillingCredit = {currentChatBillingCreditId}";

            var userAddOns = await conn.QueryFirstOrDefaultAsync<ChatPlanUser>(query, commandTimeout: 90);

            return userAddOns;
        }

        public async Task<IList<int>> GetUserIdsByClientManagerIdAsync(int clientManagerId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT IdUser FROM [User] WHERE IdClientManager =  {clientManagerId}";

            var userIds = await conn.QueryAsync<int>(query, commandTimeout: 90);

            return userIds.ToList();
        }


        public async Task<decimal> GetCurrenyRate(int from, int to)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT TOP 1 Rate FROM dbo.CurrencyRate WHERE active = 1 AND IdCurrencyTypeFrom = {from} AND IdCurrencyTypeTo = {to} ORDER BY UTCFromDate DESC";

            var currency = await conn.QueryFirstOrDefaultAsync<decimal>(query, commandTimeout: 90);

            return currency;
        }
    }
}
