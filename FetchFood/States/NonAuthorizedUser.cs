namespace FetchFood.States
{
    public class NonAuthorizedUser : UserStateBase
    {
        public override void ToNextState(UserState userState)
        {
            userState.State = new AuthorizedUser();
        }
    }
}
