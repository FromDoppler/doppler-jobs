using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;
using Dapper;
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
    }
}
