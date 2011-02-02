using System;
using System.Threading;
using System.Threading.Tasks;

namespace Demos
{
    public class ParallelFor
    {
        [Ignore]
        public void Run()
        {
            Action<int> body = i => Console.WriteLine("Index {0} executing in thread {1}",
                                                      i, Thread.CurrentThread.ManagedThreadId);

            StaticParallelFor(0, 100, body);

            int result = ParallelAggregate(0,
                                           100,
                                           0,
                                           i => Utils.IsPrime(i) ? 1 : 0,
                                           (x, y) => x + y);

            Console.WriteLine(result);
        }

        public static void StaticParallelFor(int inclusiveFrom, int exclusiveTo, 
                                             Action<int> body)
        {
            int size = exclusiveTo - inclusiveFrom;
            int processors = Environment.ProcessorCount;
            int range = size / processors;

            var tasks = new Task[processors];

            for (int p = 0; p < processors; ++p)
            {
                int start = p * range + inclusiveFrom;
                int end = p == processors - 1 ? exclusiveTo : start + range;

                tasks[p] = Task.Factory.StartNew(() =>
                {
                    for (int i = start; i < end; i++)
                    {
                        body(i);
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        public static void DynamicParallelFor(int inclusiveFrom, int exclusiveTo, 
                                              Action<int> body)
        {
            int processors = Environment.ProcessorCount;
            var tasks = new Task[processors];
            int index = inclusiveFrom;

            for (int p = 0; p < processors; ++p)
            {
                tasks[p] = Task.Factory.StartNew(() =>
                {
                    int i;
                    while ((i = Interlocked.Increment(ref index) - 1) < exclusiveTo)
                    {
                        body(i);
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        public static void ReplicableParallelFor(int inclusiveFrom, int exclusiveTo, 
                                                 Action<int> body)
        {
            int index = inclusiveFrom;
            var cancellationTokenSource = new CancellationTokenSource();

            Action taskReplicaDelegate = null;
            taskReplicaDelegate = delegate
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                Task.Factory.StartNew(taskReplicaDelegate,
                                      cancellationTokenSource.Token,
                                      TaskCreationOptions.AttachedToParent,
                                      TaskScheduler.Default);

                int i;
                while ((i = Interlocked.Increment(ref index) - 1) < exclusiveTo)
                {
                    body(i);
                }

                cancellationTokenSource.Cancel();
            };

            Task.Factory.StartNew(taskReplicaDelegate).Wait();
        }

        public static T ParallelAggregate<T>(int fromInclusive, 
                                             int toExclusive, 
                                             T seed,
                                             Func<int, T> selector, 
                                             Func<T, T, T> aggregator)
        {
            T result = seed;
            var aggLock = new object();

            ParallelLoopResult loopResult = Parallel.For(fromInclusive, 
                                                         toExclusive, 
                                                         () => seed, 
                                                         (i, state, local) =>
            {
                if (state.IsStopped)
                {
                    return local;
                }

                try
                {
                    //
                    // Return the current replica's new local state.
                    //

                    return aggregator(local, selector(i));
                }
                catch
                {
                    //
                    // Exceptions also break loops.
                    //

                    state.Stop();
                    return local;
                }
            }, partial => { lock (aggLock) result = aggregator(partial, result); });

            Console.WriteLine("Loop result: {0}", loopResult.IsCompleted);
            
            return result;
        }
    }
}
