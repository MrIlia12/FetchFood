namespace FetchFood.States
{
    public class IsMakingOrder : UserStateBase
    {
        public override void ToNextState(UserState userState)
        {
            userState.State = new NonAuthorizedUser();
        }
    }
}
