using Client.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Client.WinForms.Data
{
    public class ReplayDbContext : DbContext
    {
        public DbSet<ReplayGame> ReplayGames => Set<ReplayGame>();
        public DbSet<ReplayMove> ReplayMoves => Set<ReplayMove>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // DB file next to the EXE:
            var dbPath = Path.Combine(AppContext.BaseDirectory, "replays.sqlite");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReplayGame>()
                .HasMany(g => g.Moves)
                .WithOne(m => m.ReplayGame!)
                .HasForeignKey(m => m.ReplayGameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReplayMove>()
                .HasIndex(m => new { m.ReplayGameId, m.TurnIndex })
                .IsUnique(false);
        }
    }
}
