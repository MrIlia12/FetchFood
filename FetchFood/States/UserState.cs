namespace FetchFood.States
{
    /// <summary>
    /// Класс состояния пользователя.
    /// </summary>
    public class UserState
    {
        /// <summary>
        /// Состояние.
        /// </summary>
        public UserStateBase State;

        /// <summary>
        /// Ctor.
        /// </summary>
        public UserState(UserStateBase userState)
        {
            this.State = userState;
        }
    }
}
