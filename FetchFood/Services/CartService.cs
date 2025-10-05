using System;
using System.Collections.Generic;
using System.Linq;

namespace FetchFood.Services
{
    public class CartItem
    {
        public int FoodId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }

    public class CartService
    {
        private readonly Dictionary<long, List<CartItem>> _userCarts = new();
        private readonly Dictionary<long, int> _nextFoodIds = new();

        public void AddToCart(long userId, string foodName, int quantity, decimal price)
        {
            if (!_userCarts.ContainsKey(userId))
            {
                _userCarts[userId] = new List<CartItem>();
                _nextFoodIds[userId] = 1;
            }

            var existing = _userCarts[userId].FirstOrDefault(i => i.FoodName.Equals(foodName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                int newFoodId = _nextFoodIds[userId]++;
                _userCarts[userId].Add(new CartItem
                {
                    FoodId = newFoodId,
                    FoodName = foodName,
                    Quantity = quantity,
                    Price = price
                });
            }
        }

        public bool RemoveFromCart(long userId, int foodId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                var item = _userCarts[userId].FirstOrDefault(i => i.FoodId == foodId);
                if (item != null)
                {
                    _userCarts[userId].Remove(item);
                    return true;
                }
            }
            return false;
        }

        public List<CartItem> GetCart(long userId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                return _userCarts[userId];
            }
            return new List<CartItem>();
        }

        public decimal GetTotal(long userId)
        {
            return GetCart(userId).Sum(i => i.TotalPrice);
        }

        public void ClearCart(long userId)
        {
            if (_userCarts.ContainsKey(userId))
            {
                _userCarts[userId].Clear();
            }
        }
    }
}