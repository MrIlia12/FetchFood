using DataAccess.Entities;
using DataAccess.Entities.Models;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.MakingOrders.States
{
    /// <summary>
    /// Контектс для управления состояниями
    /// </summary>
    public class OrderStateContext
    {
        private OrderState currentState;

        public void SetState(OrderState state)
        {
            currentState = state;
        }

        public async Task<OrderProcessingResult> HandleInputAsync(long userId, string message, UserOrderData orderData)
        {
            if (currentState == null)
            {
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Ошибка обработки состояния"
                };
            }

            return await currentState.HandleInputAsync(userId, message, orderData);
        }

        public string GetStatusMessage(UserOrderData orderData)
        {
            return currentState?.GetStatusMessage(orderData) ?? "Состояние не определено";
        }
    }
}
