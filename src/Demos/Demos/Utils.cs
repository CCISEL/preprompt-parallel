using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Demos
{
    public static class Utils
    {
        public static void Measure(string name, Action action, Action setup = null)
        {
            const int repeats = 2;
            long total = 0L;

            //
            // Warm-up by bringing the required data into the cache.
            //

            if (setup != null)
            {
                setup();
            }

            action();

            var watch = new Stopwatch();
            for (int i = 0; i < repeats; ++i)
            {
                if (setup != null)
                {
                    setup();
                }

                watch.Start();
                action();
                watch.Stop();
                total += watch.ElapsedMilliseconds;
                watch.Reset();
            }

            Console.WriteLine("{0} took about {1} ms", name, total / repeats);
        }

        public static bool IsPrime(int number)
        {
            return number == 2 || 2.To(number - 1).All(i => number % i != 0);
        }

        public static IEnumerable<int> To(this int first, int last)
        {
            if (first == last)
            {
                yield return first;
                yield break;
            }

            if (first < last)
            {
                for (var l = first; l <= last; l++)
                {
                    yield return l;
                }
                yield break;
            }

            for (var l = first; l >= last; l--)
            {
                yield return l;
            }
        }
    }
}