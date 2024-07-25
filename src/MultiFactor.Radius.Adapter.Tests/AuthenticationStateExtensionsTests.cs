using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class AuthenticationStateExtensionsTests
    {
        [Fact]
        public void A_Big_Bull_Of_Tests()
        {
            AuthenticationState state;
            PacketCode code;

            //
            // Default state
            state = new();
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessChallenge, code);
            
            //
            // Bypass only one factor -> challenge
            state = new();
            state.SetFirstFactor(AuthenticationCode.Bypass);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessChallenge, code);
            
            state = new();
            state.SetSecondFactor(AuthenticationCode.Bypass);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessChallenge, code);

            //
            // Accept only one factor -> challenge
            state = new();
            state.SetFirstFactor(AuthenticationCode.Accept);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessChallenge, code);
            
            state = new();
            state.SetSecondFactor(AuthenticationCode.Accept);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessChallenge, code);

            //
            // Bypass all -> accept
            state = new();
            state.SetFirstFactor(AuthenticationCode.Bypass);
            state.SetSecondFactor(AuthenticationCode.Bypass);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessAccept, code);
            
            //
            // Bypass or accept -> accept
            state = new();
            state.SetFirstFactor(AuthenticationCode.Accept);
            state.SetSecondFactor(AuthenticationCode.Bypass);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessAccept, code);
            
            state = new();
            state.SetFirstFactor(AuthenticationCode.Bypass);
            state.SetSecondFactor(AuthenticationCode.Accept);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessAccept, code);

            //
            // Accept all -> accept
            state = new();
            state.SetFirstFactor(AuthenticationCode.Accept);
            state.SetSecondFactor(AuthenticationCode.Accept);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessAccept, code);
            
            //
            // Reject any -> reject
            state = new();
            state.SetFirstFactor(AuthenticationCode.Reject);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessReject, code);
            
            state = new();
            state.SetSecondFactor(AuthenticationCode.Reject);
            code = state.ToPacketCode();
            Assert.Equal(PacketCode.AccessReject, code);
        }
    }
}
