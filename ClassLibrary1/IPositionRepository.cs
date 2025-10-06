using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface IPositionRepository
    {
        // добавить новую позицию
        Task<bool> AddPositionAsync(Position user);

        // запросить конкретную позицию
        Task<Position?> GetPositionByIdAsync(int PositionId);

        // обновить существующую позицию
        Task<bool> UpdatePositionAsync(Position position);

        // удалить конкретную позицию
        Task<bool> RemovePositionByIdAsync(int PositionId);

        // выбираем из базы все позиции
        Task<List<Position>> GetAllPositionsAsync(CancellationToken ct = default);

        // поиск позиций по имени
        Task<List<Position>> GetPositionsByNameAsync(string namePart, CancellationToken ct = default);
    } 
}
