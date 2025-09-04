using Microsoft.EntityFrameworkCore;

namespace DummyRestApiExample.Data
{
    public class DatabaseConfig
    {
        public static void Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}