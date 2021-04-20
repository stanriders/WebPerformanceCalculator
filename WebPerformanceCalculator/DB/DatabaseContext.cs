
using Microsoft.EntityFrameworkCore;
using WebPerformanceCalculator.DB.Types;

namespace WebPerformanceCalculator.DB
{
    public class DatabaseContext : DbContext
    {
        #nullable disable
        private const string connection_string = "Filename=./top.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connection_string);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerSearchQuery>().ToView(null);
        }

        public DbSet<Player> Players { get; set; }

        public DbSet<Score> Scores { get; set; }

        public DbSet<Map> Maps { get; set; }

        public DbSet<PlayerSearchQuery> PlayerSearchQuery { get; set; }

    }
}
