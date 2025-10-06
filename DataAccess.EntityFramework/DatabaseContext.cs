using Microsoft.EntityFrameworkCore;
using DataAccess.Entities;

namespace DataAccess.EntityFramework

{
    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Position> Positions { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { }
    }
}