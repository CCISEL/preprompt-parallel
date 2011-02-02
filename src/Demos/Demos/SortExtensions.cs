using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 420

namespace Demos
{
    public class QuicksortDemo
    {
        [Ignore]
        public static void Run()
        {
            const int size = 1000000;

            var rand = new Random();

            int[] a = null;

            Utils.Measure("Sequential",
                () => a.QuickSort(0, size - 1),
                () => a = 0.To(size).Select(i => rand.Next(int.MaxValue)).ToArray());

            Utils.Measure("Parallel",
                () => a.QuickSortPar(0, size - 1),
                () => a = 0.To(size).Select(i => rand.Next(int.MaxValue)).ToArray());

            Utils.Measure("Parallel 2",
                () => a.QuickSortPar2(0, size - 1),
                () => a = 0.To(size).Select(i => rand.Next(int.MaxValue)).ToArray());

            Utils.Measure("Parallel 3",
                () => a.QuickSortPar3(0, size - 1),
                () => a = 0.To(size).Select(i => rand.Next(int.MaxValue)).ToArray());
        }
    }

    public static class SortExtensions
    {
        private const int THRESHOLD = 16;

        #region Details

        private static void exch<T>(T[] a, int i, int j)
        {
            T aux = a[i];
            a[i] = a[j];
            a[j] = aux;
        }

        private static int partition<T>(T[] a, int l, int r)
            where T : IComparable<T>
        {
            int i = l, j = r - 1;
            T v = a[r];

            while (true)
            {
                while (i <= j && a[i].CompareTo(v) <= 0)
                {
                    ++i;
                }

                while (j >= l && v.CompareTo(a[j]) < 0)
                {
                    --j;
                }

                if (i >= j)
                {
                    break;
                }

                exch(a, i, j);
            }

            exch(a, i, r);
            return i;
        }

        public static void InsertionSort<T>(this T[] a, int l, int r)
            where T : IComparable<T>
        {
            for (int i = l + 1; i <= r; ++i)
            {
                T v = a[i];
                int j = i;
                while (j >= l + 1 && v.CompareTo(a[j - 1]) < 0)
                {
                    a[j] = a[j - 1];
                    --j;
                }

                a[j] = v;
            }
        }

        #endregion

        #region Quicksort Sequential
        public static void QuickSort<T>(this T[] a, int l, int r)
            where T : IComparable<T>
        {
            if (r <= l)
            {
                return;
            }

            if (r - l <= THRESHOLD)
            {
                InsertionSort(a, l, r);
                return;
            }

            int m = partition(a, l, r);

            QuickSort(a, l, m - 1);
            QuickSort(a, m + 1, r);
        }
        #endregion

        #region QuickSortPar

        public static void QuickSortPar<T>(this T[] a, int l, int r)
            where T : IComparable<T>
        {
            if (r <= l)
            {
                return;
            }

            if (r - l <= THRESHOLD)
            {
                InsertionSort(a, l, r);
                return;
            }

            int m = partition(a, l, r);

            var left = Task.Factory.StartNew(() => QuickSortPar(a, l, m - 1));

            QuickSortPar(a, m + 1, r);

            left.Wait();
        }

        #endregion

        #region QuickSortPar2

        public static void QuickSortPar2<T>(this T[] a, int l, int r)
            where T : IComparable<T>
        {
            quicksort_internal2(a, l, r, (int)Math.Log(Environment.ProcessorCount, 2) + 1);
        }

        private static void quicksort_internal2<T>(T[] a, int l, int r, int depth)
            where T : IComparable<T>
        {
            if (r <= l)
            {
                return;
            }

            if (r - l <= THRESHOLD)
            {
                InsertionSort(a, l, r);
                return;
            }

            int m = partition(a, l, r);

            if (depth > 0)
            {
                //
                // Create as many tasks as those needed to occupy all CPUs.
                //

                var left = Task.Factory.StartNew(() => quicksort_internal2(a, l, m - 1, depth - 1));

                quicksort_internal2(a, m + 1, r, depth - 1);

                left.Wait();
            }
            else
            {
                QuickSort(a, l, m - 1);
                QuickSort(a, m + 1, r);
            }
        }
        #endregion

        #region QuickSortPar3

        private static readonly int CONC_LIMIT = Environment.ProcessorCount + 1;

        internal class State
        {
            public volatile int ScheduledTasks;
        }

        public static void QuickSortPar3<T>(this T[] a, int l, int r)
            where T : IComparable<T>
        {
            quicksort_internal3(a, l, r, new State { ScheduledTasks = 0 });
        }

        private static void quicksort_internal3<T>(T[] a, int l, int r, State state)
            where T : IComparable<T>
        {
            if (r <= l)
            {
                return;
            }

            if (r - l <= THRESHOLD)
            {
                InsertionSort(a, l, r);
                return;
            }

            int m = partition(a, l, r);

            if (state.ScheduledTasks < CONC_LIMIT)
            {
                //
                // Strive to keep all CPUs busy.
                //

                Interlocked.Increment(ref state.ScheduledTasks);      
                var left = Task.Factory.StartNew(() => quicksort_internal3(a, l, m - 1, state));

                quicksort_internal3(a, m + 1, r, state);

                left.Wait();                                   
                Interlocked.Decrement(ref state.ScheduledTasks);
            }
            else
            {
                quicksort_internal3(a, l, m - 1, state);
                quicksort_internal3(a, m + 1, r, state);
            }
        }

        #endregion
    }
}