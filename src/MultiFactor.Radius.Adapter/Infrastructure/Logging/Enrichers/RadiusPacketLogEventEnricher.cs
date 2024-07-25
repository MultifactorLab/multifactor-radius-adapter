using MultiFactor.Radius.Adapter.Core.Framework.Context;
using Serilog.Core;
using Serilog.Events;
using System;

namespace MultiFactor.Radius.Adapter.Infrastructure.Logging.Enrichers
{
    internal class RadiusPacketLogEventEnricher : ILogEventEnricher
    {
        private const string _callingStationIdToken = "CallingStationId";

        private readonly RadiusContext _context;

        private RadiusPacketLogEventEnricher(RadiusContext context)
        {
            _context = context;
        }

        public static RadiusPacketLogEventEnricher Create(RadiusContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            return new RadiusPacketLogEventEnricher(context);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty(_callingStationIdToken, _context.RequestPacket.CallingStationId);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
