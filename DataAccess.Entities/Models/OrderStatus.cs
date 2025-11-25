using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Entities.Models
{
    /// <summary>
    /// Статусы процесса оформления заказа (включая процесс оформления и жизненный цикл)
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Процесс оформления не начат
        /// </summary>
        None,

        /// <summary>
        /// Ожидание ввода адреса доставки (шаг оформления)
        /// </summary>
        WaitingForAddress,

        /// <summary>
        /// Ожидание выбора ввода комментария к заказу(да/нет) (шаг оформления)
        /// </summary>
        WaitingForComment,

        /// <summary>
        /// Ожидание ввода комментария к заказу (шаг оформления)
        /// </summary>
        WaitingForCommentText,

        /// <summary>
        /// Ожидание подтверждения заказа пользователем (шаг оформления)
        /// </summary>
        WaitingForConfirmation,

        /// <summary>
        /// Заказ создан, ожидает обработки
        /// </summary>
        Created,

        /// <summary>
        /// Заказ ожидает оплаты
        /// </summary>
        AwaitingPayment,

        /// <summary>
        /// Заказ оплачен
        /// </summary>
        Paid,

        /// <summary>
        /// Заказ подтвержден администратором
        /// </summary>
        Confirmed,

        /// <summary>
        /// Заказ отменен
        /// </summary>
        Cancelled,

        /// <summary>
        /// Заказ готовится
        /// </summary>
        InProgress,

        /// <summary>
        /// Заказ готов к выдаче
        /// </summary>
        Ready,

        /// <summary>
        /// Заказ передан курьеру
        /// </summary>
        AssignedToCourier,

        /// <summary>
        /// Заказ доставлен
        /// </summary>
        Delivered
    }

}
