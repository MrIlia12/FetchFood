using DataAccess.Entities;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Cart.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса управления корзиной пользователя.
    /// </summary>
    public interface ICartService
    {
        /// <summary>
        /// Асинхронно получает или создает корзину пользователя.
        /// </summary>
        /// <param name="telegramUserId">ID пользователя Telegram.</param>
        /// <returns>Корзина пользователя (<see cref="UserOrderData"/>).</returns>
        Task<UserOrderData> GetCartAsync(long telegramUserId);

        /// <summary>
        /// Асинхронно добавляет товар (позицию меню) в корзину.
        /// </summary>
        /// <param name="telegramUserId">ID пользователя Telegram.</param>
        /// <param name="menuPositionId">Идентификатор позиции меню.</param>
        /// <param name="quantity">Количество.</param>
        /// <returns>Обновленная корзина пользователя.</returns>
        Task<UserOrderData> AddItemToCartAsync(long telegramUserId, int menuPositionId, int quantity);

        /// <summary>
        /// Асинхронно удаляет товар из корзины по ID товара.
        /// </summary>
        /// <param name="telegramUserId">ID пользователя Telegram.</param>
        /// <param name="productId">Идентификатор продукта (товара).</param>
        /// <returns>Обновленная корзина пользователя.</returns>
        Task<UserOrderData> RemoveItemFromCartAsync(long telegramUserId, int productId);

        /// <summary>
        /// Асинхронно очищает все товары из корзины.
        /// </summary>
        /// <param name="telegramUserId">ID пользователя Telegram.</param>
        /// <returns>Пустая корзина пользователя.</returns>
        Task<UserOrderData> ClearCartAsync(long telegramUserId);
    }
}
