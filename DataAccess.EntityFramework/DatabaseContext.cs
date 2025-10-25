using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace DataAccess.EntityFramework

{
    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        
        public DbSet<Position> Positions { get; set; }

        public DbSet<Orders> Orders { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Конвертируем enum (OrdersStatus) в строку,
            // Чтобы в бд писал статус словами, а не числовым значением
            modelBuilder.Entity<Orders>()
                .Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Настройка связи User -> Orders (пользователь как заказчик)
            modelBuilder.Entity<Orders>()               // Настраиваем конфигурацию для сущности Orders
                .HasOne(o => o.User)                    // Каждый заказ имеет одного пользователя
                .WithMany(u => u.Orders)                // Один пользователь может иметь много заказов
                .HasForeignKey(o => o.IdUser)           // Связь осуществляется через поле IdUser в таблице orders
                .HasPrincipalKey(u => u.TelegramUserId) // Ссылаемся на TelegramUserId
                .OnDelete(DeleteBehavior.Restrict);     // Запрещает удаление пользователя, если у него есть заказы

            // Настройка связи User -> CouriersOrders (пользователь как курьер)
            //modelBuilder.Entity<Orders>()
            //    .HasOne(o => o.Courier)
            //    .WithMany(u => u.CourierOrders)
            //    .HasForeignKey(o => o.IdCourier)
            //    .HasPrincipalKey(u => u.TelegramUserId)
            //    .OnDelete(DeleteBehavior.Restrict);

            // Если нужно указать точное имя таблицы
            //modelBuilder.Entity<Orders>().ToTable("orders");
            //modelBuilder.Entity<User>().ToTable("user");
        }
    }
}