using DataAccess.Entities;

namespace BusinessLogic.Services.Menu.Abstractions
{
    /// <summary>
    /// Сервис работы с меню (интерфейс).
    /// </summary>
    public interface IMenuService
    {
        /// <summary>
        /// Получить список всех активных позиций БД
        /// </summary>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список позиций в БД</returns>
        Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Получить список активных позиций по категории
        /// </summary>
        /// <param name="categoryId">Id категории (null для позиций без категории)</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Список позиций в указанной категории</returns>
        Task<List<Position>> GetActivePositionsByCategoryAsync(int? categoryId, CancellationToken ct = default);
        /// <summary>
        /// Запросить позицю по Id из БД
        /// </summary>
        /// <param name="id">Id позиции</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Запрашиваемая позиция</returns>
        Task<Position?> GetPositionAsync(int id, CancellationToken ct = default);
        /// <summary>
        /// Найти позиции по совпадению в имени
        /// </summary>
        /// <param name="namePart">Имя позиции (или его часть)</param>
        /// <param name="onlyActive">Найти только позиции со статусом Active</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Запрашиваемая позиция</returns>
        Task<List<Position>> SearchPositionsAsync(string namePart, bool onlyActive = true, CancellationToken ct = default);
        /// <summary>
        /// Создать новую позицию
        /// </summary>
        /// <param name="p">Новая позиция</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> CreateAsync(Position p, CancellationToken ct = default);
        /// <summary>
        /// Удалить позицию по Id
        /// </summary>
        /// <param name="id">Id удаляемой позиции</param>
        /// <param name="ct">Токен отмены (опционально)</param>
        /// <returns>Результат операции БД</returns>
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
