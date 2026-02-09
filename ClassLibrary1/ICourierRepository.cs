using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface ICourierRepository
    {
        Task<bool> AddCourierAsync(Courier courier);

        Task<Courier> GetCourierByUserIdAsync(long userId);
    }
}
