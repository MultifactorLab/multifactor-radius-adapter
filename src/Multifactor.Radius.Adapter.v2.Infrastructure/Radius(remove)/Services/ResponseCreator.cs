using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Builders;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Services;

internal interface IResponseCreator
{
    RadiusPacket Execute(RadiusPacket request, PacketCode responseCode);
}
internal sealed class ResponseCreator : IResponseCreator
{
    private readonly IRadiusPacketBuilder _builder;
    private readonly ILogger<ResponseCreator> _logger;

    public ResponseCreator(
        IRadiusPacketBuilder builder,
        ILogger<ResponseCreator> logger)
    {
        _builder = builder;
        _logger = logger;
    }
    public RadiusPacket Execute(RadiusPacket request, PacketCode responseCode)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Creating response packet for request Id={Id}, ResponseCode={ResponseCode}", 
                request.Identifier, responseCode);
            
            var response = _builder.CreateResponse(request, responseCode);
            
            _logger.LogDebug("Successfully created response packet: Code={Code}, Id={Id}", 
                response.Code, response.Identifier);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create response packet for request Id={Id}", request.Identifier);
            throw new Exception("Failed to create response packet", ex);
        }
    }
}