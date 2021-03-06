﻿using System;
using System.Collections.Concurrent;

namespace Resiliency
{
    public class CircuitBreaker
    {
        private readonly ICircuitBreakerStrategy Strategy;

        private readonly CircuitBreakerOptions Options;

        internal readonly object SyncRoot = new object();

        public CircuitBreaker(ICircuitBreakerStrategy strategy, CircuitBreakerOptions options = null)
        {
            Options = options ?? new CircuitBreakerOptions();
            State = Options.InitialState;
            StateLastChangedAt = DateTimeOffset.UtcNow;
            Strategy = strategy;            
        }

        public CircuitState State { get; private set; }

        public Exception LastException { get; private set; }

        public DateTimeOffset StateLastChangedAt { get; private set; }

        public DateTimeOffset CooldownCompleteAt { get; private set; }

        public int HalfOpenSuccessCount { get; private set; }

        public void Trip(Exception ex, TimeSpan? cooldownPeriod = null)
        {
            LastException = ex;

            if (State == CircuitState.Open)
                throw new CircuitBrokenException(ex);

            lock (SyncRoot)
            {
                if (State == CircuitState.Open)
                    throw new CircuitBrokenException(ex);

                State = CircuitState.Open;
                StateLastChangedAt = DateTimeOffset.UtcNow;
                CooldownCompleteAt = StateLastChangedAt + (cooldownPeriod ?? Options.DefaultCooldownPeriod);

                throw new CircuitBrokenException(ex, (CooldownCompleteAt - DateTimeOffset.Now).Duration());
            }
        }

        internal void OnFailure(Exception ex)
        {
            LastException = ex;

            if (Strategy.ShouldTrip(ex))
                Trip(ex, Options.DefaultCooldownPeriod);
        }

        public void Reset()
        {
            lock (SyncRoot)
            {
                ResetState();
            }
        }

        internal void OnHalfOpenSuccess()
        {
            if (++HalfOpenSuccessCount > Options.HalfOpenSuccessCountBeforeClose)
            {
                // don't call Reset, because that would be double locking, so deadlock.
                ResetState();
            }
        }

        private void ResetState()
        {
            StateLastChangedAt = DateTimeOffset.UtcNow;
            State = CircuitState.Closed;
            HalfOpenSuccessCount = 0;
            LastException = null;
        }

        public void TestCircuit()
        {
            StateLastChangedAt = DateTimeOffset.UtcNow;
            State = CircuitState.Closed;
            HalfOpenSuccessCount = 0;
            LastException = null;
        }

        public static CircuitBreaker GetCircuitBreaker(string key)
        {
            if(CircuitBreakers.TryGetValue(key, out var circuitBreaker))
                return circuitBreaker;

            throw new ArgumentException($"No circuit breaker registered with key: {key}.", nameof(key));
        }

        public static CircuitBreaker GetOrAddCircuitBreaker(string key, Func<CircuitBreaker> factory)
        {
            return CircuitBreakers.GetOrAdd(key, (opKey) => factory());
        }

        public static void RegisterCircuitBreaker(string key, CircuitBreaker circuitBreaker)
        {
            if (CircuitBreakers.TryAdd(key, circuitBreaker))
                return;

            throw new InvalidOperationException($"A circuit breaker with key: {key} already registered.");
        }

        private static ConcurrentDictionary<string, CircuitBreaker> CircuitBreakers = new ConcurrentDictionary<string, CircuitBreaker>();
    }
}
