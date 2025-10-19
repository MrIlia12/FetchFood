using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities.Models;
using DataAccess.Entities;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Menu.Implementation
{
    public class MenuService : IMenuService
    {
        private readonly ILogger<MenuService> _logger;
        private readonly IPositionRepository _positionRepository;

        public MenuService(IPositionRepository positionRepository, ILogger<MenuService> logger)
        {
            _logger = logger;
            _positionRepository = positionRepository;
        }
        public async Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default)
        {
            List<Position> all = await _positionRepository.GetAllPositionsAsync(ct);
            return all
                .Where(p => p.Status == PositionStatus.Active)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public async Task<List<Position>> SearchPositionsAsync(string namePart, CancellationToken ct = default)
        {
            namePart = namePart.Trim();
            if (string.IsNullOrEmpty(namePart))
            {
                return new List<Position>();
            }

            List<Position> matches = await _positionRepository.GetPositionsByNameAsync(namePart, ct);
            return matches
                .Where(p => p.Status == PositionStatus.Active)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public async Task<Position?> GetPositionAsync(int positionId, CancellationToken ct = default)
        {
            Position pos = await _positionRepository.GetPositionByIdAsync(positionId);
            return pos?.Status == PositionStatus.Active ? pos : null;
        }
        public async Task<Position> CreateAsync(Position p, CancellationToken ct = default)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            p.Status = PositionStatus.Active;
            p.Name = p.Name?.Trim() ?? throw new ArgumentException("Требуется имя!", nameof(p));
            await _positionRepository.AddPositionAsync(p, ct);
            return p;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            return await _positionRepository.RemovePositionByIdAsync(id, ct);
        }
    }
}
