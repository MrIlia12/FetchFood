using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FetchFood.Commands
{
    /// <summary>
    /// Команды сервиса оформления заказов
    /// </summary>
    public class MakingOrdersCommand : CommandsBase
    {
        public const string ORDER = "order";
        private MakingOrdersCommand(string command) : base(command)
        { }

        /// <summary>
        /// Команда начала оформления заказа
        /// </summary>
        public static MakingOrdersCommand StartOrder { get { return new MakingOrdersCommand($"{ORDER}{Separator}start_order"); } }
        /// <summary>
        /// Команда добавления комментария к заказу
        /// </summary>
        public static MakingOrdersCommand AddComment { get { return new MakingOrdersCommand($"{ORDER}{Separator}add_comment"); } }
        /// <summary>
        /// Команда отказа от комментария к заказу
        /// </summary>
        public static MakingOrdersCommand SkipComment { get { return new MakingOrdersCommand($"{ORDER}{Separator}skip_comment"); } }
        /// <summary>
        /// Команда оформления заказа (создания в бд)
        /// </summary>
        public static MakingOrdersCommand ConfirmOrder { get { return new MakingOrdersCommand($"{ORDER}{Separator}confirm_order"); } }
        /// <summary>
        /// Команда отмена оформления заказа
        /// </summary>
        public static MakingOrdersCommand CancelOrder { get { return new MakingOrdersCommand($"{ORDER}{Separator}cancel_order"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
