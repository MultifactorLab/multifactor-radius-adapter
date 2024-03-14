using MultiFactor.Radius.Adapter.Server.Context;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class AuthenticationStateTests
    {
        public void DefaultValues()
        {
            var state = new AuthenticationState();
            Assert.Equal(AuthenticationCode.Awaiting, state.FirstFactor);
            Assert.Equal(AuthenticationCode.Awaiting, state.SecondFactor);
        }
        
        public void Accept_ShoultAcceptAll()
        {
            var state = new AuthenticationState();
            state.Accept();

            Assert.Equal(AuthenticationCode.Accept, state.FirstFactor);
            Assert.Equal(AuthenticationCode.Accept, state.SecondFactor);
        }
        
        public void Reject_ShoultRejectAll()
        {
            var state = new AuthenticationState();
            state.Reject();

            Assert.Equal(AuthenticationCode.Reject, state.FirstFactor);
            Assert.Equal(AuthenticationCode.Reject, state.SecondFactor);
        }
    }
}
