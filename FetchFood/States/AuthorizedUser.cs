namespace FetchFood.States
{
    public class AuthorizedUser : UserStateBase
    {
        public override void ToNextState(UserState userState)
        {
            userState.State = new IsMakingOrder();
        }
    }
}
