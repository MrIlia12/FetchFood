using DataAccess.Entities;

namespace BusinessLogic.Services.Menu.Abstractions
{
    /// <summary>
    /// Сервис работы с категориями позиций (интерфейс).
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Получить все категории
        /// </summary>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список всех категорий</returns>
        Task<List<PositionCategory>> GetAllCategoriesAsync(CancellationToken ct = default);

        /// <summary>
        /// Получить категорию по ID
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Категория или null</returns>
        Task<PositionCategory?> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default);

        /// <summary>
        /// Создать новую категорию
        /// </summary>
        /// <param name="category">Новая категория</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> CreateAsync(PositionCategory category, CancellationToken ct = default);

        /// <summary>
        /// Удалить категорию по ID
        /// </summary>
        /// <param name="categoryId">Id удаляемой категории</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> DeleteAsync(int categoryId, CancellationToken ct = default);

        /// <summary>
        /// Обновить категорию
        /// </summary>
        /// <param name="category">Обновляемая категория</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> UpdateAsync(PositionCategory category, CancellationToken ct = default);
    }
}

