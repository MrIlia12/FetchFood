using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface IPositionRepository
    {
        /// <summary>
        /// Добавление позиции в таблицу БД
        /// </summary>
        /// <param name="position">Добавляемая позиция</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> AddPositionAsync(Position position, CancellationToken ct = default);

        /// <summary>
        /// Запрос конкретной позиции из БД по Id
        /// </summary>
        /// <param name="PositionId">Id запрашиваемой позиции</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Запрашиваемая позиция</returns>
        Task<Position?> GetPositionByIdAsync(int PositionId, CancellationToken ct = default);

        /// <summary>
        /// Обновление существующей позиции в БД
        /// </summary>
        /// <param name="position">Обновляемая позиция</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> UpdatePositionAsync(Position position, CancellationToken ct = default);

        /// <summary>
        /// Удаление конкретной позиции по Id
        /// </summary>
        /// <param name="PositionId"></param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> RemovePositionByIdAsync(int PositionId, CancellationToken ct = default);

        /// <summary>
        /// Выборка всех позиций из БД
        /// </summary>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список всех позиций в БД</returns>
        Task<List<Position>> GetAllPositionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Поиск позиций по имени
        /// </summary>
        /// <param name="namePart">Имя позиции (или его часть)</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список позиций в БД, найденных по совпадению в имени</returns>
        Task<List<Position>> GetPositionsByNameAsync(string namePart, CancellationToken ct = default);

        /// <summary>
        /// Получить все позиции по категории
        /// </summary>
        /// <param name="categoryId">Id категории</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список позиций в указанной категории</returns>
        Task<List<Position>> GetPositionsByCategoryIdAsync(int categoryId, CancellationToken ct = default);
    } 
}
