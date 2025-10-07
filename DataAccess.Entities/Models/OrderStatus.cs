namespace DataAccess.Entities.Models
{
    /// <summary>
    /// Статусы заказа.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Рассматривается.
        /// </summary>
        Pending,

        /// <summary>
        /// Подтверждён
        /// </summary>
        Approved,

        /// <summary>
        /// Заказ в работе
        /// </summary>
        InProgress,

        /// <summary>
        /// Доставляется,
        /// </summary>
        Delivered,

        /// <summary>
        /// Завершён
        /// </summary>
        Complete
    }
}
