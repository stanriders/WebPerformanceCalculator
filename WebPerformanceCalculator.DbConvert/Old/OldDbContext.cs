using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebPerformanceCalculator.DbConvert.Old.Types;

namespace WebPerformanceCalculator.DbConvert.Old
{
#nullable disable
    public class OldDbContext : DbContext
    {
        private const string connection_string = "Filename=./old.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connection_string);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Score>().HasKey(p => new { p.PP, p.Map, p.Player, p.CalcTime });
        }

        public DbSet<Player> Players { get; set; }

        public DbSet<Score> Scores { get; set; }
    }
}
