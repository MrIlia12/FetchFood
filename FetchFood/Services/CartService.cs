using System;
using System.Collections.Generic;
using System.Linq;

namespace FetchFood.Services
{
    /// <summary>
    /// Представляет товар в корзине пользователя.
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// ID товара в рамках одной корзины.
        /// </summary>
        public int FoodId { get; set; }

        /// <summary>
        /// Название товара.
        /// </summary>
        public string FoodName { get; set; } = string.Empty;

        /// <summary>
        /// Количество единиц данного товара.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Цена за одну единицу товара.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Вычисляемое свойство для получения общей стоимости товара (Количество * Цена).
        /// </summary>
        public decimal TotalPrice => Quantity * Price;
    }

    /// <summary>
    /// Сервис для управления логикой корзины покупок.
    /// </summary>
    public class CartService
    {
        // для хранения корзин всех пользователей. Ключ - ID пользователя, значение - список товаров.
        private readonly Dictionary<long, List<CartItem>> _userCarts = new();
        // для генерации уникальных ID для новых товаров в корзине каждого пользователя.
        private readonly Dictionary<long, int> _nextFoodIds = new();

        /// <summary>
        /// Добавляет товар в корзину пользователя. Если товар уже существует, увеличивает его количество.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <param name="foodName">Название товара.</param>
        /// <param name="quantity">Количество.</param>
        /// <param name="price">Цена за единицу.</param>
        public void AddToCart(long userId, string foodName, int quantity, decimal price)
        {
            // Если у пользователя еще нет корзины, создаем ее.
            if (!_userCarts.ContainsKey(userId))
            {
                _userCarts[userId] = new List<CartItem>();
                _nextFoodIds[userId] = 1; // Начинаем счетчик ID товаров для этого пользователя с 1.
            }

            // Проверяем, есть ли уже такой товар в корзине (по названию).
            var existing = _userCarts[userId].FirstOrDefault(i => i.FoodName.Equals(foodName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Если товар найден, просто увеличиваем его количество.
                existing.Quantity += quantity;
            }
            else
            {
                // Если товара нет, создаем новый и добавляем в корзину.
                int newFoodId = _nextFoodIds[userId]++; // Получаем следующий доступный ID и увеличиваем счетчик.
                _userCarts[userId].Add(new CartItem
                {
                    FoodId = newFoodId,
                    FoodName = foodName,
                    Quantity = quantity,
                    Price = price
                });
            }
        }

        /// <summary>
        /// Удаляет товар из корзины пользователя по его ID.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <param name="foodId">ID товара для удаления.</param>
        /// <returns>Возвращает <c>true</c>, если товар был успешно удален, иначе <c>false</c>.</returns>
        public bool RemoveFromCart(long userId, int foodId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                var item = _userCarts[userId].FirstOrDefault(i => i.FoodId == foodId);
                if (item != null)
                {
                    // Если товар найден, удаляем его из списка.
                    _userCarts[userId].Remove(item);
                    return true;
                }
            }
            // Возвращаем false, если корзина не найдена или товара с таким ID в ней нет.
            return false;
        }

        /// <summary>
        /// Возвращает содержимое корзины пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Список товаров в корзине. Если корзины нет, возвращает пустой список.</returns>
        public List<CartItem> GetCart(long userId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                return _userCarts[userId];
            }
            // Возвращаем пустой список, чтобы избежать ошибок NullReferenceException.
            return new List<CartItem>();
        }

        /// <summary>
        /// Рассчитывает общую стоимость всех товаров в корзине.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Общая стоимость товаров.</returns>
        public decimal GetTotal(long userId)
        {
            // Используем LINQ для суммирования TotalPrice каждого элемента в корзине.
            return GetCart(userId).Sum(i => i.TotalPrice);
        }

        /// <summary>
        /// Полностью очищает корзину пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        public void ClearCart(long userId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                _userCarts[userId].Clear();
            }
        }
    }
    // ====================================================================
    // ТЕСТОВЫЕ ДАННЫЕ
    // ЭТОТ БЛОК УДАЛИТЬ ПЕРЕД СЛИЯНИЕМ С ОСНОВНОЙ ВЕТКОЙ
    // ====================================================================
    public static class CartTestDataInitializer
    {
        public static void InitializeTestData(CartService cartService, long userId)
        {
            // Очищаем возможные старые данные
            cartService.ClearCart(userId);

            // Добавляем тестовые товары
            cartService.AddToCart(userId, "🍔 Классический бургер", 2, 299.99m);
            cartService.AddToCart(userId, "🍟 Картофель фри", 1, 149.50m);
            cartService.AddToCart(userId, "🥤 Кола", 1, 99.00m);
            cartService.AddToCart(userId, "🍕 Пицца Маргарита", 1, 450.00m);
            cartService.AddToCart(userId, "🥗 Цезарь с курицей", 1, 280.00m);
        }
    }
}
