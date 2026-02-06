using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Courier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CourierId { get; set; }

        [Required]
        [ForeignKey("User")]
        public long IdUser { get; set; }

        // Навигационные свойства для заказов
        public virtual ICollection<Orders> Orders { get; set; }
        //public virtual ICollection<Orders> CourierOrders { get; set; }
    }
}
