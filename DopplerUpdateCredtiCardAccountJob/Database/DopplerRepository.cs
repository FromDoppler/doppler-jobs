using System;
using System.Threading.Tasks;
using Dapper;
using Doppler.Database;
using Microsoft.Extensions.Logging;

namespace Doppler.UpdateCredtiCardAccount.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private readonly ILogger<DopplerRepository> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DopplerRepository(
            ILogger<DopplerRepository> logger,
            IDbConnectionFactory dbConnectionFactory)
        {
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task UpdateUser(int userId)
        {
            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();
                // TODO: Implement the specific update query for User table
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error updating User table for UserId: {UserId}.", userId);
                throw;
            }
        }

        public async Task UpdateBillingCredits(int billingCreditId)
        {
            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();
                // TODO: Implement the specific update query for BillingCredits table
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error updating BillingCredits table for BillingCreditId: {BillingCreditId}.", billingCreditId);
                throw;
            }
        }
    }
}
