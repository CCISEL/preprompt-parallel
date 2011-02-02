using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Demos
{
    public static class AsyncEnumerator
    {
        //
        // Executes asynchronously - but sequentially - all tasks returned by the specified enumerator.
        //

        public static Task Run(this IEnumerator<Task> source)
        {
            return run_internal<object>(source);
        }

        //
        // Same as above but allows the caller to fetch the result of the last task.
        //

        public static Task<T> Run<T>(this IEnumerator<Task> source)
        {
            return run_internal<T>(source);
        }

        //
        // Same as above but allows for type inference.
        //

        public static Task<T> Run<T>(this IEnumerator<Task<T>> source)
        {
            return run_internal<T>(source);
        }

        private static Task<T> run_internal<T>(IEnumerator<Task> source)
        {
            var proxy = new TaskCompletionSource<T>();

            Action<Task> cont = null;
            cont = t =>
            {
                if (!source.MoveNext())
                {
                    proxy.SetFromTask(t);
                    return;
                }

                Task current = source.Current;
                if (current.Status == TaskStatus.Created)
                {
                    current.Start();
                }
                current.ContinueWith(cont);
            };

            cont(null);

            return proxy.Task;
        }
    }
}