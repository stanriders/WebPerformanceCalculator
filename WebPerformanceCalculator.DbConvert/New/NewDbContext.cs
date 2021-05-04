
using Microsoft.EntityFrameworkCore;
using WebPerformanceCalculator.DbConvert.New.Types;

namespace WebPerformanceCalculator.DbConvert.New
{
#nullable disable
    public class NewDbContext : DbContext
    {
        private const string connection_string = "Filename=./new.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connection_string);
        }

        public DbSet<Player> Players { get; set; }

        public DbSet<Score> Scores { get; set; }

        public DbSet<Map> Maps { get; set; }
    }
}
