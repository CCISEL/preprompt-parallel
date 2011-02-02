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
    }
}