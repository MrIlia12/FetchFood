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

        public async Task<Position> GetPositionByIdAsync(int positionId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            Position position = await dbContext.Positions.FirstOrDefaultAsync(x => x.PositionId == positionId, ct);
            return position;
        }

        public async Task<bool> AddPositionAsync(Position position, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Positions.AddAsync(position, ct);
            //SaveChangesAsync() = 0 - EF не нашёл изменений, нечего сохранять (всё уже актуально).

            //SaveChangesAsync() > 0 - столько сущностей было реально вставлено, обновлено или удалено.
            return (await dbContext.SaveChangesAsync(ct)) > 0;
        }

        public async Task<bool> RemovePositionByIdAsync(int positionId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            Position existingPosition = await dbContext.Positions.FirstOrDefaultAsync(x => x.PositionId == positionId, ct);
            if (existingPosition != null)
            {
                dbContext.Positions.Remove(existingPosition);
                return (await dbContext.SaveChangesAsync(ct)) > 0;
            }
            else
            {
                // Если позиции с таким Id нет, ничего не меняем
            }
            return true;
        }
        public async Task<bool> UpdatePositionAsync(Position position, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            Position existing = await dbContext.Positions.FirstOrDefaultAsync(x => x.PositionId == position.PositionId, ct);
            if (existing is null)
            {
                // Если такой позиции нет — добавляем
                await dbContext.Positions.AddAsync(position);
            }
            else
            {
                // Если есть — обновляем нужные поля
                existing.Name = position.Name;
                existing.Price = position.Price;
                existing.Status = position.Status;
                existing.Image = position.Image;
            }
            return await dbContext.SaveChangesAsync(ct) > 0;
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
            namePart = namePart.Trim();
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
