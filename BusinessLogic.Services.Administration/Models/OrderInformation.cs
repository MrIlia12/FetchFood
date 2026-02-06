namespace BusinessLogic.Services.Administration.Models
{
    public class OrderInformation
    {
        /// <summary>
        /// Id заказа.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Id пользователя.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Id курьера.
        /// </summary>
        public string CourierId { get; set; }

        /// <summary>
        /// Статус заказа.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Цена заказа.
        /// </summary>
        public string Price { get; set; }

        /// <summary>
        /// Дата оформления.
        /// </summary>
        public DateTime DateOrder { get; set; }

        /// <summary>
        /// Позиция заказа в списке.
        /// </summary>
        public OrderPosition OrderPosition { get; set; }
    }
}
