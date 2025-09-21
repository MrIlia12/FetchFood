using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace FetchFood.Services
{
    // Хранение состояние заказа
    public enum OrderState
    {
        None,
        WaitingForAddress,
        WaitingForFullName,
        Confirmation
    }

    public class UserOrderData
    {
        public long ChatId { get; set; }
        public OrderState CurrentState { get; set; }
        public string? Address { get; set; }
        public string? FullName { get; set; }
    }
}
