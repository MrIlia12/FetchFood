using DataAccess.Entities.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities
{
    public class Order
    {
        /// <summary>
        /// Id заказа.
        /// </summary>
        [Key]
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Id пользователя.
        /// </summary>
        [Required]
        [ForeignKey(nameof(User.TelegramUserId))]
        public long UserId { get; set; }

        /// <summary>
        /// Id курьера.
        /// </summary>
        public long CourierId { get; set; }

        /// <summary>
        /// Статус заказа.
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Цена заказа.
        /// </summary>
        public float Price { get; set; }

        /// <summary>
        /// Дата оформления.
        /// </summary>
        public DateTime DateOrder { get; set; }
    }
}
