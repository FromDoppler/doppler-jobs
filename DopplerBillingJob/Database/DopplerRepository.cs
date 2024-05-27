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

                foreach(var billing in result)
                {
                    if (billing.AddOnsAmount > 0)
                    {
                        //Get Addon details
                        var billingCredit = await GetCurrentBIllingCreditByUserIdAsync(billing.Id);
                        var addOns = await GetUserAddOnsByUserIdAsync(billing.Id);
                        var additionalServices = new List<AdditionalService>();

                        foreach (UserAddOn addOn in addOns)
                        {
                            if (addOn.IdAddOnType == (int)AddOnTypeEnum.Landing)
                            {
                                var landings = (await GetLandingPlansByBillingCreditIdAsync(addOn.IdCurrentBillingCredit)).ToList();
                                var additionalService = new AdditionalService
                                {
                                    Type = AdditionalServiceTypeEnum.Landing,
                                    Charge = (double)0,
                                    Packs = landings.Select(l => new Pack
                                    {
                                        Amount = Convert.ToDecimal(l.Fee) * (billingCredit.TotalMonthPlan ?? 1),
                                        PackId = l.IdLandingPlan,
                                        Quantity = l.PackQty
                                    }).ToList()
                                };

                                additionalServices.Add(additionalService);
                            }
                        }

                        billing.AdditionalServices = additionalServices;
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        private async Task<IEnumerable<UserAddOn>> GetUserAddOnsByUserIdAsync(int userId)
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

        private async Task<IEnumerable<LandingPlanUser>> GetLandingPlansByBillingCreditIdAsync(int billingCreditId)
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

        private async Task<BillingCredit> GetCurrentBIllingCreditByUserIdAsync(int userId)
        {
            await using var conn = _dbConnectionFactory.GetConnection();
            var query = $@"SELECT IdBillingCredit,
                                PlanFee,
	                            TotalMonthPlan
                            FROM [Doppler2011].[dbo].[User] U
                            INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = U.IdCurrentBillingCredit
                            WHERE U.[IdUser] = {userId} ";

            var billingCredit = await conn.QueryFirstOrDefaultAsync<BillingCredit>(query, commandTimeout: 90);

            return billingCredit;
        }
    }
}
