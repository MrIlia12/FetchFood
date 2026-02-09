namespace FetchFood.States
{
    public class CompletedMakingOrder : UserStateBase
    {
        public override void ToNextState(UserState userState)
        {
            userState.State = new AuthorizedUser();
        }
    }
}
