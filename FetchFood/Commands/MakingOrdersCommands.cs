using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FetchFood.Commands
{
    public class MakingOrdersCommand
    {
        public const string ORDER = "order";
        public const string SELECTOR_COMMAND = ":";
        public string Command;
        private MakingOrdersCommand(string command)
        {
            Command = command;
        }

        public static MakingOrdersCommand StartOrder { get { return new MakingOrdersCommand($"{ORDER}{SELECTOR_COMMAND}start_order"); } }
        public static MakingOrdersCommand AddComment { get { return new MakingOrdersCommand($"{ORDER}{SELECTOR_COMMAND}add_comment"); } }
        public static MakingOrdersCommand SkipComment { get { return new MakingOrdersCommand($"{ORDER}{SELECTOR_COMMAND}skip_comment"); } }
        public static MakingOrdersCommand ConfirmOrder { get { return new MakingOrdersCommand($"{ORDER}{SELECTOR_COMMAND}confirm_order"); } }
        public static MakingOrdersCommand CancelOrder { get { return new MakingOrdersCommand($"{ORDER}{SELECTOR_COMMAND}cancel_order"); } }

        public override string ToString()
        {
            return Command;
        }
    }
}
