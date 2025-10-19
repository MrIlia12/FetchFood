using DataAccess.Entities;

namespace BusinessLogic.Services.Menu.Abstractions
{
    /// <summary>
    /// Сервис работы с меню (интерфейс).
    /// </summary>
    public interface IMenuService
    {
        Task<List<Position>> GetActivePositionsAsync(CancellationToken ct = default);
        Task<Position?> GetPositionAsync(int id, CancellationToken ct = default);
        Task<List<Position>> SearchPositionsAsync(string namePart, CancellationToken ct = default);
        Task<Position> CreateAsync(Position p, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
