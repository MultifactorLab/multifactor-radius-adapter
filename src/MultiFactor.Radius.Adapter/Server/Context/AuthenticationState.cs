namespace MultiFactor.Radius.Adapter.Server.Context
{
    public class AuthenticationState
    {
        public AuthenticationCode FirstFactor { get; private set; } = AuthenticationCode.Awaiting;
        public AuthenticationCode SecondFactor { get; private set; } = AuthenticationCode.Awaiting;

        public void SetFirstFactor(AuthenticationCode code)
        {
            FirstFactor = code;
        }

        public void SetSecondFactor(AuthenticationCode code)
        {
            SecondFactor = code;
        }

        public void Accept()
        {
            FirstFactor = AuthenticationCode.Accept;
            SecondFactor = AuthenticationCode.Accept;
        }

        public void Reject()
        {
            FirstFactor = AuthenticationCode.Reject;
            SecondFactor = AuthenticationCode.Reject;
        }
    }
}
