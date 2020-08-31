// http://neue.cc/2013/10/10_429.html

using System;
using System.Threading.Tasks;

namespace Hondarersoft.Utility
{
    public static class TaskExtensions
    {
        public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

        // ToaruAsyncMethod().NoWait();

        // ASP.NET で本クラスを使用する場合は、待ち合せないタスクの中の await は、ConfigureAwait(false) すること。
        // レスポンスを返し終わった後に await が戻ろうとすると、null 参照となる。

        //public async Task ToaruAsyncMethod()
        //{
        //    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        //    throw new Exception();
        //}

        /// <summary>
        /// <see cref="Task"/> を待ち合わせないことを明示的に宣言します。
        /// このメソッドを呼ぶことでコンパイラの警告の抑制と、例外発生時のロギングを行います。
        /// </summary>
        public static void NoWait(this Task task)
        {
            task.ContinueWith(x =>
            {
                if (UnobservedTaskException != null)
                {
                    UnobservedTaskException(null, new UnobservedTaskExceptionEventArgs(x.Exception));
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
