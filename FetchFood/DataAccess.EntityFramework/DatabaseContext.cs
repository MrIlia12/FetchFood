using Microsoft.EntityFrameworkCore;
using DataAccess.Entities;

namespace DataAccess.EntityFramework
{
	public class DataContext : DbContext
	{
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; } 
        public DbSet<Position> Positions { get; set; }
        public DbSet<PositionsOrder> PositionsOrders { get; set; }

        public DataContext() 
		{
			Database.EnsureCreated();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>()
				.HasMany(u => u.Orders)
				.WithOne(o => o.User)
				.HasForeignKey(o => o.UserId);
			
			modelBuilder.Entity<Order>()
				.HasMany(o => o.PositionsOrders)
				.WithOne(p => p.Order)
				.HasForeignKey(p => p.OrderId);

            modelBuilder.Entity<Position>()
				.HasMany(p => p.PositionsOrders)
				.WithOne(po => po.Position)
				.HasForeignKey(po => po.PositionId);

            modelBuilder.Entity<PositionsOrder>()
				.HasKey(po => new { po.OrderId, po.PositionId });
        }


		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql("Server=localhost;port=5432;database=FetchFood;Username=postgres;password=root;");
			base.OnConfiguring(optionsBuilder);
		}
	}
}
