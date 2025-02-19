using CheckersLone.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckersLone.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }

        public DbSet<GameStats> GameStats { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CheckersLoneDb;Trusted_Connection=True;"); }


        public class ApplicationDbContext : DbContext
        {
            public DbSet<Player> Players { get; set; }
            public DbSet<Game> Games { get; set; }

        }
    }
}
