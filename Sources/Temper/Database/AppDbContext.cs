using Microsoft.EntityFrameworkCore;

namespace Temper.Database
{
    internal class AppDbContext : DbContext
    {
        public DbSet<FileRecord> FileRecords { get; set; }

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=database.sqlite");
        }
    }
}