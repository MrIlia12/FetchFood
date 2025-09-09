using Microsoft.EntityFrameworkCore;
using DataAccess.Entities;

namespace DataAccess.EntityFramework

{
    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DataContext() 
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
