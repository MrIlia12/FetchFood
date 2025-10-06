using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Menu.Implementation
{
    internal class MenuService : IMenuService
    {
        private readonly ILogger<MenuService> Logger;
        private readonly IPositionRepository PositionRepository;

        public MenuService(
        IPositionRepository positionRepository,
        ILogger<MenuService> logger)
        {
            Logger = logger;
            PositionRepository = positionRepository;
        }
    }
}
