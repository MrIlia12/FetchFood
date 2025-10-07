using DataAccess.Entities;

namespace BusinessLogic.Services.Administration.Abstraction
{
    /// <summary>
    /// Сервис администрирования заказов.
    /// </summary>
    public interface IAdministrationService 
    {
        Task<string> GetOrdersIdsAsync(int count);
    }
}
