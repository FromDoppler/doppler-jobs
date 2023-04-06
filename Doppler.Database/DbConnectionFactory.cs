using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.Database
{
    [ExcludeFromCodeCoverage]
    public class DbConnectionFactory: IDbConnectionFactory
    {
        private readonly ILogger<DbConnectionFactory> _logger;
        private readonly string _connectionString;

        public DbConnectionFactory(ILogger<DbConnectionFactory> logger, IOptions<DopplerDatabaseSettings> dopplerDataBaseSettings)
        {
            _logger = logger;
            _connectionString = dopplerDataBaseSettings.Value.GetSqlConnectionString();
        }

        public DbConnection GetConnection()
        {
            _logger.LogInformation("GetConnection()");

            var connection = new SqlConnection(_connectionString);

            _logger.LogInformation(
                "Connection DataSource: {DataSource}, Database: {Database}, ConnectionTimeout: {ConnectionTimeout}",
                connection.DataSource,
                connection.Database,
                connection.ConnectionTimeout);

            return connection;
        }
    }
}
