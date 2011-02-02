using System;
using System.Threading;
using System.Threading.Tasks;

namespace Demos
{
    public class Cancellation
    {
        [Ignore]
        public void Run()
        {
            var cts1 = new CancellationTokenSource();

            var cts = CancellationTokenSource
                      .CreateLinkedTokenSource(cts1.Token, new CancellationTokenSource().Token);

            var t1 = Task.Factory.StartNew(() =>
            {
                var token = cts.Token;

                //
                // The callback is executed in the thread activating cancellation.
                //

                using (token.Register(() => Console.WriteLine("Thread {0}: cancellation requested.",
                                                              Thread.CurrentThread.ManagedThreadId)))
                {
                    do
                    {
                        Thread.Yield();
                    } while (!token.IsCancellationRequested);

                    Console.WriteLine("Thread {0}: Task cancelled.", Thread.CurrentThread.ManagedThreadId);

                    throw new OperationCanceledException(token);
                }
            }, cts.Token);

            Console.ReadKey();

            Console.WriteLine("Thread {0} cancelling", Thread.CurrentThread.ManagedThreadId);
            cts.Cancel();

            try
            {
                t1.Wait();
            }
            catch (AggregateException aggregateException)
            {
                var ex = (TaskCanceledException)aggregateException.InnerException;
                
                Console.WriteLine(ex);
            }

            Console.WriteLine("Task state: {0}", t1.Status);
        }
    }
}
