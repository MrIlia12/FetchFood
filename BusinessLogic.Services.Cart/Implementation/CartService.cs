using BusinessLogic.Services.Cart.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Repositories.Abstractions;
using DataAccess.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic; 
using System.Linq;
using System.Threading.Tasks;
using System;

namespace BusinessLogic.Services.Cart.Implementation
{
    // Сервис корзины
    public class CartService : ICartService
    {
        
        private static readonly ConcurrentDictionary<long, UserOrderData> _userCarts = new();

        // --- Зависимости ---
        private readonly IMenuService _menuService;
        private readonly IUserRepository _userRepository;

        // Внедрение зависимостей через конструктор (DI)
        public CartService(IMenuService menuService, IUserRepository userRepository)
        {
            _menuService = menuService;
            _userRepository = userRepository;
        }

        // --- РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА ---

        // Получить корзину пользователя. Если ее нет - создать.
        public async Task<UserOrderData> GetCartAsync(long telegramUserId)
        {
            // 1. Пытаемся получить существующую корзину
            if (_userCarts.TryGetValue(telegramUserId, out var cart))
            {
                return cart;
            }

            // 2. Корзины нет - создаем новую
            return await CreateNewCartAsync(telegramUserId);
        }

        // Добавить товар в корзину (или увеличить количество)
        public async Task<UserOrderData> AddItemToCartAsync(long telegramUserId, int menuPositionId, int quantity)
        {
            var cart = await GetCartAsync(telegramUserId);

            // Получаем актуальные данные о товаре (цена, название)
            var menuPosition = await _menuService.GetPositionAsync(menuPositionId);

            if (menuPosition == null)
            {
                // Защита от добавления несуществующего товара
                throw new Exception($"Позиция меню с ID {menuPositionId} не найдена.");
            }

            // Ищем, есть ли уже такой товар в корзине
            var existingItem = cart.CartItems.FirstOrDefault(item => item.ProductId == menuPositionId);

            if (existingItem != null)
            {
                // Товар найден - просто увеличиваем количество
                existingItem.Quantity += quantity;
            }
            else
            {
                // Это новый товар - добавляем в список корзины
                cart.CartItems.Add(new CartItem
                {
                    ProductId = menuPosition.PositionId,
                    ProductName = menuPosition.Name,
                    Price = menuPosition.Price,
                    Quantity = quantity
                });
            }

            // Обновляем общую стоимость
            RecalculateCartTotal(cart);
            return cart;
        }

        // Полностью убрать позицию из корзины
        public async Task<UserOrderData> RemoveItemFromCartAsync(long telegramUserId, int productId)
        {
            var cart = await GetCartAsync(telegramUserId);

            // Находим товар по ID
            var itemToRemove = cart.CartItems.FirstOrDefault(item => item.ProductId == productId);

            if (itemToRemove != null)
            {
                // Удаляем и пересчитываем общую стоимость
                cart.CartItems.Remove(itemToRemove);
                RecalculateCartTotal(cart);
            }
            // Если не нашли, просто возвращаем корзину как есть
            return cart;
        }

        // Полностью очистить корзину
        public async Task<UserOrderData> ClearCartAsync(long telegramUserId)
        {
            var cart = await GetCartAsync(telegramUserId);
            cart.CartItems.Clear(); // Очищаем список
            RecalculateCartTotal(cart); // Сбрасываем цену
            return cart;
        }


        private void RecalculateCartTotal(UserOrderData cart)
        {
            // Сумма (Цена * Количество) по всем позициям
            cart.Price = cart.CartItems.Sum(item => item.Price * item.Quantity);
        }

        // (Private) Создать новую пустую корзину для пользователя
        private async Task<UserOrderData> CreateNewCartAsync(long telegramUserId)
        {
            // Находим пользователя в БД, чтобы взять его данные (имя, телефон)
            var user = await _userRepository.GetUserByIdAsync(telegramUserId);

            if (user == null)
            {
                // правило: нельзя создать корзину для незарегистрированного пользователя
                throw new Exception($"Пользователь с Telegram ID {telegramUserId} не найден. Невозможно создать корзину.");
            }

            // Создаем новый экземпляр корзины
            var newCart = new UserOrderData
            {
                UserId = user.TelegramUserId,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                CartItems = new List<CartItem>() 
            };

            // Сохраняем новую корзину в in-memory хранилище
            _userCarts[telegramUserId] = newCart;
            return newCart;
        }
    }
}