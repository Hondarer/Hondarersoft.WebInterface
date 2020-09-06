using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hondarersoft.Utility
{
    public class NoWaitTaskExceptionEventArgs : UnobservedTaskExceptionEventArgs
    {
        public LogLevel LogLevel { get; }

        public NoWaitTaskExceptionEventArgs(AggregateException exception, LogLevel logLevel) : base(exception)
        {
            LogLevel = logLevel;
        }
    }
}
