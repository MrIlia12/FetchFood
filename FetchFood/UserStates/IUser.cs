namespace FetchFood.UserStates
{
    /// <summary>
    /// Базовый интерфейс для пользователей.
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Вызов логики, связанной с пользователем.
        /// </summary>
        Task InvokeUserLogicAsync();
    }
}
