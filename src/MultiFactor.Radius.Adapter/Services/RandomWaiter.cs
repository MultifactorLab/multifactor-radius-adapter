﻿using System.Threading.Tasks;
using System;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RandomWaiterFeature;

namespace MultiFactor.Radius.Adapter.Services
{
    /// <summary>
    /// The Waiter with built-in randomizer. Randomizer uses MIN and MAX delay values defined in the service configuration.
    /// </summary>
    internal class RandomWaiter
    {
        private readonly Random _random = new();
        private readonly RandomWaiterConfig _config;

        public RandomWaiter(RandomWaiterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Performs waiting task with configured delay values.
        /// </summary>
        /// <returns>Waiting task.</returns>
        public Task WaitSomeTimeAsync()
        {
            if (_config.ZeroDelay) return Task.CompletedTask;

            var max = _config.Min == _config.Max ? _config.Max : _config.Max + 1;
            var delay = _random.Next(_config.Min, max);

            return Task.Delay(TimeSpan.FromSeconds(delay));
        }
    }
}
