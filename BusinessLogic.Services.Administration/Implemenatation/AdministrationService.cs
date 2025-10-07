using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
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
            var user = await _userRepository.GetUserByIdAsync(0);
            var orderPosition = number == 0
                ? OrderPosition.First
                : number == orders.Length
                ? OrderPosition.Last
                : OrderPosition.Middle;


            var result = new OrderInformation
            {
                Id = orders[number].Id.ToString(),
                CourierId = orders[number].CourierId.ToString(),
                UserName = user.Name,
                Price = orders[number].Price.ToString(),
                Status = orders[number].Status.ToString(),
                DateOrder = orders[number].DateOrder.ToString(),
                OrderPosition = orderPosition
            };


            return result;
        }
    }
}
