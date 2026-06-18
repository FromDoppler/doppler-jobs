using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Doppler.Database;
using Doppler.UpdateCredtiCardAccount.Job.Entities;
using Microsoft.Extensions.Logging;

namespace Doppler.UpdateCredtiCardAccount.Job.Database
{
    public class DopplerRepository : IDopplerRepository
    {
        private const int CreditCardPaymentMethodId = 1;

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

        public async Task<IEnumerable<CreditCardData>> GetCreditCardsForComericaUpdate()
        {
            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();
                return await conn.QueryAsync<CreditCardData>(
                    @"
SELECT TOP (5)
    bc.WorldPayToken AS Token,
    RIGHT(CAST(bc.CCExpYear AS VARCHAR(4)), 2)
        + RIGHT('0' + CAST(bc.CCExpMonth AS VARCHAR(2)), 2) AS ExpiryDate
FROM [User] u
INNER JOIN [BillingCredits] bc ON bc.IdBillingCredit = u.IdCurrentBillingCredit
WHERE bc.IdPaymentMethod = @CreditCardPaymentMethodId
  AND bc.WorldPayToken IS NOT NULL
  AND bc.WorldPayToken <> ''
  AND bc.CCExpMonth IS NOT NULL
  AND bc.CCExpYear IS NOT NULL;",
                    new { CreditCardPaymentMethodId });
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error retrieving credit card data for Comerica update.");
                throw;
            }
        }
    }
}
