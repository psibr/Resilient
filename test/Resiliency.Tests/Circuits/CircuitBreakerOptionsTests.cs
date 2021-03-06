using System;
using Xunit;

namespace Resiliency.Tests.Circuits
{
    public class CircuitBreakerOptionsTests
    {
        [Fact]
        public void SetDefaultValues()
        {
            var options = new CircuitBreakerOptions();

            Assert.True(options.InitialState == CircuitState.Closed);
            Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultCooldownPeriod);
            Assert.Equal(1, options.HalfOpenSuccessCountBeforeClose);
        }
    }
}