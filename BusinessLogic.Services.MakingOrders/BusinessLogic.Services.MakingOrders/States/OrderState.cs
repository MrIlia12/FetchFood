using BusinessLogic.Services.MakingOrders.Implemenatation;
using DataAccess.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.MakingOrders.States
{
    public abstract class OrderState
    {
        protected readonly MakingOrdersService _context;
        //protected readonly ILogger<OrderState> _logger;
        protected OrderState(MakingOrdersService context)
        {
            _context = context;
            //_logger = logger;
        }

        public abstract Task<OrderProcessingResult> HandleInputAsync(long userId, string message, UserOrderData orderData);
        public abstract string GetStatusMessage(UserOrderData orderData);
    }
}

