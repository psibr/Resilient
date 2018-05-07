﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace REtry
{
    public class RetryOperation 
        : IRetryOperation
    {
        public RetryOperation(RetryHandlerInfo handler, RetryTotalInfo total)
        {
            Handler = handler;
            Total = total;
        }

        public RetryHandlerInfo Handler { get; }

        public RetryTotalInfo Total { get; }

        public CancellationToken CancellationToken { get; }

        public Task Wait(TimeSpan period, CancellationToken cancellationToken= default)
        {
            return Task.Delay(period);
        }

        public Task Wait(int milliseconds, CancellationToken cancellationToken = default)
        {
            return Task.Delay(milliseconds);
        }
    }
}
