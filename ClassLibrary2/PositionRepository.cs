using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Repositories.Implementations
{
    public class PositionRepository : IPositionRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PositionRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<Position> GetPositionByIdAsync(int positionId)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            Position position = await dbContext.Positions.FirstOrDefaultAsync(x => x.PositionId == positionId);
            return position;
        }

        public async Task<bool> AddPositionAsync(Position position)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Positions.AddAsync(position);
            //SaveChangesAsync() = 0 - EF не нашёл изменений, нечего сохранять (всё уже актуально).

            //SaveChangesAsync() > 0 - столько сущностей было реально вставлено, обновлено или удалено.
            return (await dbContext.SaveChangesAsync()) > 0;
        }

        public async Task<bool> RemovePositionByIdAsync(int positionId)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            Position existingPosition = dbContext.Positions.FirstOrDefault(x => x.PositionId == positionId);
            if (existingPosition != null)
            {
                dbContext.Positions.Remove(existingPosition);
                return (await dbContext.SaveChangesAsync()) > 0;
            }
            else
            {
                // Если позициис таким Id нет, ничего не меняем
            }
            return true;
        }
        public async Task<bool> UpdatePositionAsync(Position position)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            Position existingPosition = dbContext.Positions.FirstOrDefault(x => x.PositionId == position.PositionId);
            if (existingPosition != null)
            {
                dbContext.Positions.Remove(existingPosition);
            }
            else
            {
                // Если позиции с таким Id нет, просто добавляем новую
            }
            await dbContext.Positions.AddAsync(position);
            
            return (await dbContext.SaveChangesAsync()) > 0;
        }
        public async Task<List<Position>> GetAllPositionsAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            List<Position> positions = await dbContext.Positions.ToListAsync(ct);
            return positions;
        }
        public async Task<List<Position>> GetPositionsByNameAsync(string namePart, CancellationToken ct = default)
        {
            namePart = namePart.Trim(['\n', '\r', ' ']);
            if (string.IsNullOrEmpty(namePart))
            {
                return new List<Position>();
            }

            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            List<Position> positions = await dbContext.Positions.Where(x => x.Name.Contains(namePart, StringComparison.CurrentCultureIgnoreCase)).ToListAsync(ct);
            return positions;
        }
    }
}
