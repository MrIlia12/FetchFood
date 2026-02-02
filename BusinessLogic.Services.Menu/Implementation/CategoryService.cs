using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Menu.Implementation
{
    public class CategoryService : ICategoryService
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly IPositionCategoryRepository _categoryRepository;

        public CategoryService(IPositionCategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _logger = logger;
            _categoryRepository = categoryRepository;
        }

        public async Task<List<PositionCategory>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            return await _categoryRepository.GetAllPositionCategoriesAsync(ct);
        }

        public async Task<PositionCategory?> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default)
        {
            return await _categoryRepository.GetPositionCategoryByIdAsync(categoryId, ct);
        }

        public async Task<bool> CreateAsync(PositionCategory category, CancellationToken ct = default)
        {
            if (category is null) throw new ArgumentNullException(nameof(category));
            category.Name = category.Name?.Trim() ?? throw new ArgumentException("Требуется имя категории!", nameof(category));
            return await _categoryRepository.AddPositionCategoryAsync(category, ct);
        }

        public async Task<bool> DeleteAsync(int categoryId, CancellationToken ct = default)
        {
            return await _categoryRepository.RemovePositionCategoryByIdAsync(categoryId, ct);
        }

        public async Task<bool> UpdateAsync(PositionCategory category, CancellationToken ct = default)
        {
            if (category is null) throw new ArgumentNullException(nameof(category));
            category.Name = category.Name?.Trim() ?? throw new ArgumentException("Требуется имя категории!", nameof(category));
            return await _categoryRepository.UpdatePositionCategoryAsync(category, ct);
        }
    }
}

