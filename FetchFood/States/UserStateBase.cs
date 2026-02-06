namespace FetchFood.States
{
    public abstract class UserStateBase
    {
        public abstract void ToNextState(UserState userState);
    }
}
