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
    }
}
