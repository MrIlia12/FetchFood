using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Repositories.Implementations
{
    public class PositionCategoryCategoryRepository : IPositionCategoryRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PositionCategoryCategoryRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        public async Task<PositionCategory?> GetPositionCategoryByIdAsync(int PositionCategoryId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            PositionCategory PositionCategory = await dbContext.PositionCategories.FirstOrDefaultAsync(x => x.PositionCategoryId == PositionCategoryId, ct);
            return PositionCategory;
        }

        public async Task<bool> AddPositionCategoryAsync(PositionCategory PositionCategory, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.PositionCategories.AddAsync(PositionCategory, ct);

            return (await dbContext.SaveChangesAsync(ct)) > 0;
        }

        public async Task<bool> RemovePositionCategoryByIdAsync(int PositionCategoryId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var existing = await dbContext.PositionCategories.FirstOrDefaultAsync(x => x.PositionCategoryId == PositionCategoryId, ct);
            if (existing is null) return false;

            dbContext.PositionCategories.Remove(existing);
            return (await dbContext.SaveChangesAsync(ct)) > 0;
        }
        public async Task<bool> UpdatePositionCategoryAsync(PositionCategory PositionCategory, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            PositionCategory existing = await dbContext.PositionCategories.FirstOrDefaultAsync(x => x.PositionCategoryId == PositionCategory.PositionCategoryId, ct);
            if (existing is null)
            {
                // Если такой позиции нет — добавляем
                await dbContext.PositionCategories.AddAsync(PositionCategory);
            }
            else
            {
                // Если есть — обновляем нужные поля
                existing.Name = PositionCategory.Name;
                existing.Description = PositionCategory.Description;
            }
            return await dbContext.SaveChangesAsync(ct) > 0;
        }
        public async Task<List<PositionCategory>> GetAllPositionCategoriesAsync(CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            List<PositionCategory> PositionCategories = await dbContext.PositionCategories.ToListAsync(ct);
            return PositionCategories;
        }
        public async Task<List<PositionCategory>> GetPositionCategoriesByNameAsync(string namePart, CancellationToken ct = default)
        {
            namePart = namePart?.Trim() ?? string.Empty;
            if (namePart.Length == 0) return new List<PositionCategory>();

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            string pattern = $"%{namePart}%";
            return await dbContext.PositionCategories
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Name, pattern))
                .OrderBy(p => p.Name)
                .ToListAsync(ct);
        }
    }
}
