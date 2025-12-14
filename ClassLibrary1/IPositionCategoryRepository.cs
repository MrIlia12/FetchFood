using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface IPositionCategoryRepository
    {
        /// <summary>
        /// Добавление категории в таблицу БД
        /// </summary>
        /// <param name="_positionCategory">Добавляемая категория</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> AddPositionCategoryAsync(PositionCategory _positionCategory, CancellationToken ct = default);

        /// <summary>
        /// Запрос конкретной категории из БД по Id
        /// </summary>
        /// <param name="_positionCategoryId">Id запрашиваемой категории</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Запрашиваемая категория</returns>
        Task<PositionCategory?> GetPositionCategoryByIdAsync(int _positionCategoryId, CancellationToken ct = default);

        /// <summary>
        /// Обновление существующей категории в БД
        /// </summary>
        /// <param name="_positionCategory">Обновляемая категория</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> UpdatePositionCategoryAsync(PositionCategory _positionCategory, CancellationToken ct = default);

        /// <summary>
        /// Удаление конкретной категории по Id
        /// </summary>
        /// <param name="_positionCategoryId"></param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> RemovePositionCategoryByIdAsync(int _positionCategoryId, CancellationToken ct = default);

        /// <summary>
        /// Выборка всех категорий из БД
        /// </summary>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список всех категорий в БД</returns>
        Task<List<PositionCategory>> GetAllPositionCategoriesAsync(CancellationToken ct = default);

        /// <summary>
        /// Поиск категорий по имени
        /// </summary>
        /// <param name="_namePart">Имя категории (или его часть)</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список категорий в БД, найденных по совпадению в имени</returns>
        Task<List<PositionCategory>> GetPositionCategoriesByNameAsync(string _namePart, CancellationToken ct = default);
    }
}
