using System;
using System.Threading.Tasks;

namespace Demos
{
    public class Exceptions
    {
        [Ignore]
        public void Run()
        {
            unobserved_exception();
            propagate_exception();
            propagate_multiple_exceptions();
            propagate_multiple_exceptions_nested();
            handle_exceptions();
            observe_exceptions();
        }

        private static void unobserved_exception()
        {
            Task.Factory.StartNew(() => { throw new Exception("Some exception."); });

            while (true)
            {
                GC.Collect();
            }
        }

        private static void propagate_exception()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                Task.Factory.StartNew(() =>
                {
                    throw new Exception("Child faulting.");
                }, TaskCreationOptions.AttachedToParent);
            });

            try
            {
                parent.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Parent caught {0}.", e);
            }
        }

        private static void propagate_multiple_exceptions()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    int x = i;
                    Task.Factory.StartNew(() =>
                    {
                        throw new Exception(string.Format("Child {0} faulting.", x));
                    }, TaskCreationOptions.AttachedToParent);
                }

                throw new Exception("Parent faulting.");
            });

            try
            {
                parent.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Parent caught {0} exceptions.", e.InnerExceptions.Count);
                foreach (var ex in e.InnerExceptions)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void propagate_multiple_exceptions_nested()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 2; i++)
                {
                    int x = i;
                    Task.Factory.StartNew(() =>
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            int y = j;
                            Task.Factory.StartNew(() =>
                            {
                                throw new Exception(string.Format("Grandchild {0} faulting.",y));
                            }, TaskCreationOptions.AttachedToParent);
                        }

                        throw new Exception(string.Format("Child {0} faulting", x));
                    }, TaskCreationOptions.AttachedToParent);
                }
                throw new Exception("Parent faulting.");
            });

            try
            {
                //
                // Observe exception.
                //

                parent.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Parent caught {0} exceptions.", e.InnerExceptions.Count);

                var ex = e.Flatten();

                Console.WriteLine("After flattening parent caught {0} exceptions.", ex.InnerExceptions.Count);
            }
        }

        private static void handle_exceptions()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                Task.Factory.StartNew(() =>
                {
                    throw new Exception("Child faulting.");
                }, TaskCreationOptions.AttachedToParent);

                throw new Exception("Parent faulting.");
            });

            try
            {
                parent.Wait();
            }
            catch (AggregateException e)
            {
                //e.Handle(ex => ex is AggregateException);
                e.Flatten().Handle(ex => !(ex is AggregateException));
            }
        }

        private static void observe_exceptions()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                var child = Task.Factory.StartNew(() =>
                {
                    throw new Exception("Child faulting.");
                }, TaskCreationOptions.AttachedToParent);

                try { child.Wait(); }
                catch { }

                throw new Exception("Parent faulting.");
            });

            try
            {
                parent.Wait();
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.Flatten().InnerExceptions.Count);
            }
        }
    }
}