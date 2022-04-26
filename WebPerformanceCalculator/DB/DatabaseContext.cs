
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebPerformanceCalculator.DB.Types;

namespace WebPerformanceCalculator.DB
{
    public class DatabaseContext : DbContext
    {
        #nullable disable
        private const string connection_string = "Filename=./db/top.db";

#if DEBUG
        private readonly ILoggerFactory loggerFactory;
#endif

        public DatabaseContext()
        {
#if DEBUG
            loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
#endif
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connection_string);

#if DEBUG
            optionsBuilder.UseLoggerFactory(loggerFactory);
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlayerSearchQuery>().ToView(null);

            modelBuilder.Entity<Score>().HasIndex(x=> new {x.UpdateTime, x.LocalPp});
        }

        public DbSet<Player> Players { get; set; }

        public DbSet<Score> Scores { get; set; }

        public DbSet<Map> Maps { get; set; }

        public DbSet<PlayerSearchQuery> PlayerSearchQuery { get; set; }
    }
}
