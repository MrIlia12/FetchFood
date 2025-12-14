using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities.Models;
using DataAccess.Repositories.Abstractions;

namespace BusinessLogic.Services.Administration.Implemenatation
{
    /// <summary>
    /// Сервис администрирования.
    /// </summary>
    public class AdministrationService : IAdministrationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;

        public AdministrationService(IOrderRepository orderRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Получает информацию по заказу по порядковому номеру.
        /// </summary>
        /// <param name="number">Порядковый номер.</param>
        /// <returns></returns>
        public async Task<OrderInformation> GetOrderInformationAsync(int number)
        {
            var orders = await _orderRepository.GetOrdersAsync();
            var user = await _userRepository.GetUserByIdAsync(orders[number].IdUser);
            var orderPosition = number == 0
                ? orders.Length == 1
                    ? OrderPosition.Lonely
                    : OrderPosition.First
                : number == orders.Length - 1
                    ? OrderPosition.Last
                    : OrderPosition.Middle;


            var result = new OrderInformation
            {
                Id = orders[number].OrderId.ToString(),
                ////CourierId = orders[number].CourierId.ToString(),
                UserName = user.Name,
                Price = orders[number].Price.ToString(),
                Status = orders[number].Status.ToString(),
                DateOrder = orders[number].DateOrder,
                OrderPosition = orderPosition
            };


            return result;
        }

        public async Task<bool> ChangeOrderStatus(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            
            var statusNumber = (int)order.Status;
            order.Status = (OrderStatus)(statusNumber + 1);

            return await _orderRepository.UpdateOrderAsync(order);
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            return await _orderRepository.RemoveOrderByIdAsync(orderId);
        }
    }
}
