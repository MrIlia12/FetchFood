using DataAccess.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Entities
{
    /// <summary>
    /// Временные данные заказа during оформления
    /// </summary>
    public class UserOrderData
    {
        public long UserId { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Comment { get; set; }
        // Текущий шаг процесса оформления заказа
        public OrderStatus CurrentState { get; set; }
        // Список товаров в корзине
        public List<CartItem> CartItems { get; set; } = new();
        public decimal Price { get; set; }
    }
}
