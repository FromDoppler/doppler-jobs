using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.Database
{
    public class DopplerDatabaseSettings
    {
        public string ConnectionString { get; set; }

        public string Password { get; set; }

        public int CommandTimeOut { get; set; } = 1200;

        public int MaxRetryAttempts { get; set; } = 10;

        public string GetSqlConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentException("The string argument 'connectionString' cannot be empty.");
            }

            var builder = new SqlConnectionStringBuilder(ConnectionString);

            if (!string.IsNullOrWhiteSpace(Password))
            {
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }
    }
}
