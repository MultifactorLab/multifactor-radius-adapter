using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;


namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class LdapFirstFactorProcessor : IFirstFactorProcessor
{
    private readonly ILdapAuthProvider _authProvider;
    private readonly ILogger<LdapFirstFactorProcessor> _logger;

    public AuthenticationSource AuthenticationSource => AuthenticationSource.Ldap;

    public LdapFirstFactorProcessor(ILdapAuthProvider authProvider, ILogger<LdapFirstFactorProcessor> logger)
    {
        _authProvider = authProvider;
        _logger = logger;
    }

    public async Task ProcessFirstFactor(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var radiusPacket = context.RequestPacket;
        Throw.IfNull(radiusPacket, nameof(radiusPacket));

        if (context.LdapServerConfiguration is null)
            throw new InvalidOperationException("No Ldap servers configured.");

        var authProcessor = _authProvider.GetLdapAuthProcessor(radiusPacket.AuthenticationType);
        
        if (authProcessor is null)
            throw new InvalidOperationException("No Ldap auth processors configured.");
        
        var isValid = await authProcessor.Auth(context);
        
        if (!isValid.IsSuccess)
        {
            Reject(context);
            return;
        }

        _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", radiusPacket.UserName, context.LdapServerConfiguration.ConnectionString);
        Accept(context);
    }

    private void Reject(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Reject;
    }

    private void Accept(IRadiusPipelineExecutionContext context)
    {
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
    }
}