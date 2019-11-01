
using Microsoft.EntityFrameworkCore;

namespace WebPerformanceCalculator.DB
{
    public class DatabaseContext : DbContext
    {
        private const string connection_string = "Filename=./top.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connection_string);
        }

        public DbSet<Player> Players { get; set; }

    }
}
