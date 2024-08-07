﻿using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Logging.Enrichers
{
    /// <summary>
    /// Log enricher. Adds CorrelationId property to log event.
    /// </summary>
    public class CorrelationIdLogEventEnricher : ILogEventEnricher
    {
        private static readonly object Locker = new();
        private static DateTime ResetPoint;
        private static long RequestChainCounter;

        private readonly string _correlationId;

        private CorrelationIdLogEventEnricher(string correlationId)
        {
            _correlationId = correlationId;
        }

        /// <summary>
        /// Creates new instance of enricher for the specified client configuration.
        /// </summary>
        /// <param name="clientConfiguration">Client configuration.</param>
        /// <returns></returns>
        public static CorrelationIdLogEventEnricher Create(IClientConfiguration clientConfiguration)
        {
            if (clientConfiguration is null) throw new ArgumentNullException(nameof(clientConfiguration));

            lock (Locker)
            {
                var cid = $"{clientConfiguration.Name}-{GetCounterValue()}";
                return new CorrelationIdLogEventEnricher(cid);
            }
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty("CorrelationId", _correlationId);
            logEvent.AddOrUpdateProperty(property);
        }

        private static long GetCounterValue()
        {
            if (DateTime.Now < ResetPoint)
            {
                RequestChainCounter++;
            }
            else
            {
                ResetPoint = DateTime.Parse("23:59:59.999");
                RequestChainCounter = 1;
            }

            return RequestChainCounter;
        }
    }
}
