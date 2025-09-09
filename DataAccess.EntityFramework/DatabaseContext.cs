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
            optionsBuilder.UseNpgsql("Server=localhost;port=9432;database=FetchFood;User ID=postgres;password=1882320;");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
