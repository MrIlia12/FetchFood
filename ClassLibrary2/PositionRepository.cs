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

        public async Task<Position?> GetPositionByIdAsync(int positionId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            Position position = await dbContext.Positions
                .Include(p => p.Category)
                .FirstOrDefaultAsync(x => x.PositionId == positionId, ct);
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
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var existing = await dbContext.Positions.FirstOrDefaultAsync(x => x.PositionId == positionId, ct);
            if (existing is null) return false;

            dbContext.Positions.Remove(existing);
            return (await dbContext.SaveChangesAsync(ct)) > 0;
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
                existing.Description = position.Description;
                existing.Ingredients = position.Ingredients;
                existing.Image = position.Image;
                existing.PositionCategoryId = position.PositionCategoryId;
            }
            return await dbContext.SaveChangesAsync(ct) > 0;
        }
        public async Task<List<Position>> GetAllPositionsAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            List<Position> positions = await dbContext.Positions
                .Include(p => p.Category)
                .ToListAsync(ct);
            return positions;
        }
        public async Task<List<Position>> GetPositionsByNameAsync(string namePart, CancellationToken ct = default)
        {
            namePart = namePart?.Trim() ?? string.Empty;
            if (namePart.Length == 0) return new List<Position>();

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            string pattern = $"%{namePart}%";
            return await dbContext.Positions
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Name, pattern)) 
                .OrderBy(p => p.Name)
                .ToListAsync(ct);
        }

        public async Task<List<Position>> GetPositionsByCategoryIdAsync(int categoryId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            return await dbContext.Positions
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.PositionCategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);
        }
    }
}
