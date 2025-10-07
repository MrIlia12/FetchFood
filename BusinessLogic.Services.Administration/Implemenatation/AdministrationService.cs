using BusinessLogic.Services.Administration.Abstraction;
using DataAccess.Entities;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Administration.Implemenatation
{
    /// <summary>
    /// Сервис администрирования.
    /// </summary>
    public class AdministrationService : IAdministrationService
    {
        private readonly IOrderRepository _orderRepository;

        public AdministrationService(IOrderRepository orderRepository) 
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Получает список из id заказов указанной длины.
        /// </summary>
        /// <param name="count">Число заказов для вывода.</param>
        /// <returns></returns>
        public async Task<string> GetOrdersIdsAsync(int count)
        {
            var orders = await _orderRepository.GetOrdersAsync(0, count);

            var result = "";
            foreach (var order in orders)
            {
                result += order.Id.ToString() + "\n";
            }

            return result;
        }
    }
}
