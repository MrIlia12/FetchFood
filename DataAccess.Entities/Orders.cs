using DataAccess.Entities;
using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot.Types.ReplyMarkups;

namespace DataAccess.Entities
{
    /// <summary>
    /// Таблица оформленных заказов
    /// </summary>
    public class Orders
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrderId { get; set; }

        [Required]
        [ForeignKey("User")]
        public long IdUser { get; set; }

        //[ForeignKey("Courier")]
        //public long? IdCourier { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public DateTime DateOrder { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string? Status { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        // Навигационные свойства
        public virtual User User { get; set; }
        //public virtual User Courier { get; set; }
    }

    /// <summary>
    /// Результат обработки ввода пользователя
    /// </summary>
    public class OrderProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NextState { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasInlineKeyboard { get; set; }
        public InlineKeyboardMarkup InlineKeyboard { get; set; }
    }
}
