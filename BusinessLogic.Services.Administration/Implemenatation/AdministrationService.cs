using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using DataAccess.Repositories.Abstractions;

namespace BusinessLogic.Services.Administration.Implemenatation
{
    /// <summary>
    /// Сервис администрирования.
    /// </summary>
    public class AdministrationService : IAdministrationService
    {
        private readonly IOrdersRepository _orderRepository;
        private readonly IUserRepository _userRepository;

        public AdministrationService(IOrdersRepository orderRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
        }

        public async Task<List<Orders>> GetOrdersAsync(string status)
        {
            List<Orders> orders;
            try
            {
                orders = await _orderRepository.GetOrdersByStatusAsync(status);
            }
            catch
            {
                throw new Exception("Ошибка в выгрузке заказов.");
            }

            return orders;
        }

        public async Task<Orders> GetOrderAsync(int orderId)
        {
            Orders order;
            try
            {
                order = await _orderRepository.GetOrderByIdAsync(orderId);
            }
            catch
            {
                throw new Exception("Ошибка в выгрузке заказов.");
            }

            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            bool result;
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                order.Status = newStatus;
                result = await _orderRepository.UpdateOrderAsync(order);
            }
            catch
            {
                throw new Exception("Ошибка при работе с заказами в БД.");
            }

            return result;
        }

        public async Task<long> GetOrdersUserIdAsync(int orderId)
        {
            long userId;
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                userId = order.IdUser;
            }
            catch
            {
                throw new Exception("Ошибка при выгрузке заказов.");
            }

            return userId;
        }
    }
}
