using CrossCutting.Notificacion.Entities;
using Dapper;
using Doppler.Database;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Notifications.Job.Database
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

        public async Task<IList<UserNotification>> GetUserWithTrialExpiresInDays(int days)
        {
            _logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();
                var query = @$"SELECT U.TrialExpirationDate, U.Email, U.FirstName, U.LastName, L.Name AS Language
                               FROM [User] U
                               INNER JOIN Language L ON U.IdLanguage = L.IdLanguage
                               WHERE  (U.UpgradePending IS NULL OR U.UpgradePending = 1) AND 
							          TrialExpirationDate <= DATEADD(day,{days},GETUTCDATE()) AND TrialExpirationDate > DATEADD(day, {days} - 1,GETUTCDATE()) AND
									  (IsCancelated = 0 OR IsCancelated IS NULL)";

                _logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryAsync<UserNotification>(query);

                return result.ToList();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }

        public async Task<IList<UserNotification>> GetUserWithTrialExpired()
        {
            _logger.LogInformation("Getting database connection.");

            try
            {
                await using var conn = _dbConnectionFactory.GetConnection();
                var query = @$"SELECT U.TrialExpirationDate, U.Email, U.FirstName, U.LastName, L.Name AS Language
                               FROM [User] U
                               INNER JOIN Language L ON U.IdLanguage = L.IdLanguage
                               WHERE  (U.UpgradePending IS NULL OR U.UpgradePending = 1) AND 
							          TrialExpirationDate <= DATEADD(day, 0,GETUTCDATE()) AND TrialExpirationDate > DATEADD(day, -1,GETUTCDATE()) AND
									  (IsCancelated = 0 OR IsCancelated IS NULL)";

                _logger.LogInformation("Sending SQL sentence to database server.");

                var result = await conn.QueryAsync<UserNotification>(query);

                return result.ToList();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }
    }
}
