namespace MultiFactor.Radius.Adapter.Framework.Context
{
    /// <summary>
    /// Radius request first and second factors authentiction.
    /// </summary>
    public class AuthenticationState
    {
        /// <summary>
        /// First factor authentication component.
        /// </summary>
        public AuthenticationCode FirstFactor { get; private set; } = AuthenticationCode.Awaiting;

        /// <summary>
        /// Second factor authentication component.
        /// </summary>
        public AuthenticationCode SecondFactor { get; private set; } = AuthenticationCode.Awaiting;

        /// <summary>
        /// Sets first factor authentication state.
        /// </summary>
        /// <param name="code">Authentication code.</param>
        public void SetFirstFactor(AuthenticationCode code)
        {
            FirstFactor = code;
        }

        /// <summary>
        /// Sets second factor authentication state.
        /// </summary>
        /// <param name="code">Authentication code.</param>
        public void SetSecondFactor(AuthenticationCode code)
        {
            SecondFactor = code;
        }

        /// <summary>
        /// Accepts both first and second factors.
        /// </summary>
        public void Accept()
        {
            FirstFactor = AuthenticationCode.Accept;
            SecondFactor = AuthenticationCode.Accept;
        }

        /// <summary>
        /// Rejects first and second factors.
        /// </summary>
        public void Reject()
        {
            FirstFactor = AuthenticationCode.Reject;
            SecondFactor = AuthenticationCode.Reject;
        }
    }
}
