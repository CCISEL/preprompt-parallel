using System;
using System.Net;
using System.Threading.Tasks;

namespace Demos
{
    public class TaskCompletionSource
    {
        [Ignore]
        public void Run()
        {
            var webReq = WebRequest.Create("http://www.google.com");

            var task = FromAsync(webReq.BeginGetResponse, webReq.EndGetResponse)
                       .WithTimeout(100);

            var cont1 = task.ContinueWith(t => Console.WriteLine(t.Result.Headers),
                        TaskContinuationOptions.OnlyOnRanToCompletion);
            
            var cont2 = task.ContinueWith(t => Console.WriteLine("Task cancelled"), 
                        TaskContinuationOptions.OnlyOnCanceled);

            Task.WaitAny(cont1, cont2);
            Console.ReadKey();
        }

        //
        // Already implemented by Task.Factory.FromAsync().
        //

        public static Task<T> FromAsync<T>(Func<AsyncCallback, object, IAsyncResult> begin,
                                           Func<IAsyncResult, T> end)
        {
            var tcs = new TaskCompletionSource<T>();

            //
            // The callback of the APM begin method reflects the state of the async call
            // in the proxy task associated with the task completion source.
            //

            begin(iar =>
            {
                try
                {
                    T result = end(iar);
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }, null);

            //
            // Return the proxy task.
            //

            return tcs.Task;
        }
    }
}