
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // lol
            modelBuilder.Entity<Score>().HasKey(p => new {p.PP, p.Map, p.Player, p.CalcTime});

            //modelBuilder.Entity<PlayerSearchQuery>(eb => { eb.HasNoKey(); });
        }

        public DbSet<Player> Players { get; set; }

        public DbSet<Score> Scores { get; set; }

        public DbQuery<PlayerSearchQuery> PlayerSearchQuery { get; set; }

    }
}
