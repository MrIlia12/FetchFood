using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities;

namespace BusinessLogic.Services.Administration.Abstraction
{
    /// <summary>
    /// Сервис администрирования заказов.
    /// </summary>
    public interface IAdministrationService 
    {
        Task<OrderInformation> GetOrderInformationAsync(int number);
    }
}
