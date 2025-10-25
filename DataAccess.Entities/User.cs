using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Entities
{
    public class User
    {
        [Key]
        [Required]
        public long TelegramUserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        public UserRole Role { get; set; }

        // Навигационные свойства для заказов
        public virtual ICollection<Orders> Orders { get; set; }
        //public virtual ICollection<Orders> CourierOrders { get; set; }
    }
}
