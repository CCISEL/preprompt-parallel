using System.Threading;
using System.Threading.Tasks;

namespace Demos
{
    public static class TaskExtensions
    {
        public static void SetFromTask<TResult>(this TaskCompletionSource<TResult> toComplete, Task task)
        {
            if (task == null)
            {
                toComplete.SetResult(default(TResult));
                return;
            }

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    toComplete.SetResult(task is Task<TResult> ? ((Task<TResult>)task).Result : default(TResult));
                    break;
                case TaskStatus.Faulted:
                    toComplete.SetException(task.Exception.InnerExceptions);
                    break;
                case TaskStatus.Canceled:
                    toComplete.SetCanceled();
                    break;
            }
        }

        public static Task<T> WithTimeout<T>(this Task<T> task, int timeout)
        {
            var tcs = new TaskCompletionSource<T>();

            //
            // Create a timer that will be a completion source for the 
            // proxy task associated with the task completion source.
            //

            var timer = new Timer(_ => tcs.TrySetCanceled());
            timer.Change(timeout, Timeout.Infinite);

            //
            // The task argument will also be a completion source, racing with
            // the timer to set the final state of the proxy task.
            //

            task.ContinueWith(_ =>
            {
                timer.Dispose();

                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        tcs.TrySetResult(task.Result);
                        break;
                    case TaskStatus.Faulted:
                        tcs.TrySetException(task.Exception);
                        break;
                    case TaskStatus.Canceled:
                        tcs.TrySetCanceled();
                        break;
                }
            });

            //
            // Return the proxy task.
            //

            return tcs.Task;
        }
    }
}