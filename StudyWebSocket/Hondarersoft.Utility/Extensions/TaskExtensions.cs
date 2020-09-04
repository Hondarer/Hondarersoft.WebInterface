// http://neue.cc/2013/10/10_429.html

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hondarersoft.Utility.Extensions
{
    public static class TaskExtensions
    {
        public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

        /// <summary>
        /// <see cref="Task"/> を待ち合わせないことを明示的に宣言します。
        /// このメソッドを呼ぶことでコンパイラの警告の抑制と、例外発生時のロギングを行います。
        /// </summary>
        public static void NoWaitAndWatchException(this Task task, LogLevel logLevel = LogLevel.Critical)
        {
            task.ContinueWith(x =>
            {
                if (UnobservedTaskException != null)
                {
                    UnobservedTaskException(null, new UnobservedTaskExceptionEventArgs(x.Exception)); // TODO: ログレベルに応じた異常時のふるまいができるようにする。
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
