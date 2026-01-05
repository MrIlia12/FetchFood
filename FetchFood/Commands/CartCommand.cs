namespace FetchFood.Commands
{
    /// <summary>
    /// Команды сервиса корзины
    /// </summary>
    public class CartCommand : CommandsBase
    {
        public const string CART = "cart";

        private CartCommand(string command) : base(command)
        { }

        /// <summary>
        /// Команда показа корзины
        /// </summary>
        public static CartCommand ShowCart => new CartCommand($"{CART}{Separator}cart_show");

        /// <summary>
        /// Команда добавления товара в корзину
        /// </summary>
        public static CartCommand AddItem => new CartCommand($"{CART}{Separator}cart_add");

        /// <summary>
        /// Команда удаления товара из корзины
        /// </summary>
        public static CartCommand RemoveItem => new CartCommand($"{CART}{Separator}cart_remove");

        /// <summary>
        /// Команда очистки корзины
        /// </summary>
        public static CartCommand ClearCart => new CartCommand($"{CART}{Separator}cart_clear");

        public override string ToString() => Command;
    }
}
