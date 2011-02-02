using System;
using System.Threading;
using System.Threading.Tasks;

namespace Demos
{
    public class TreeDemo
    {
        public static BinaryTree<int> BuildTree(int depth)
        {
            int sequence = (1 << depth) - 1;

            Func<BinaryTree<int>> f = null;
            f = () =>
            {
                if (sequence <= 0)
                {
                    return null;
                }

                return new BinaryTree<int>
                {
                    Value = sequence--,
                    Left = f(),
                    Right = f(),
                };
            };

            return f();
        }

        [Ignore]
        public void Run()
        {
            BinaryTree<int> tree = BuildTree(12);
            tree.Walk(Console.WriteLine);
        }
    }

    public class BinaryTree<T>
    {
        public BinaryTree<T> Left, Right;
        public T Value;
    }

    public static class BinaryTreeExtensions
    {
        public static void Walk<T>(this BinaryTree<T> root, Action<T> action)
        {
            if (root == null)
            {
                return;
            }

            Walk(root.Left, action);
            Walk(root.Right, action);

            action(root.Value);
        }
        
        public static void WalkPar<T>(this BinaryTree<T> root, Action<T> action)
        {
            if (root == null)
            {
                return;
            }

            using (var cte = new CountdownEvent(2))
            {

                ThreadPool.QueueUserWorkItem(x =>
                {
                    WalkPar(root.Left, action);
                    cte.Signal();
                });

                ThreadPool.QueueUserWorkItem(x =>
                {
                    WalkPar(root.Right, action);
                    cte.Signal();
                });

                action(root.Value);

                //
                // The worker thread blocks waiting for the created work items, which
                // activates the thread pool's thread injection algorithm.
                //

                cte.Wait();
            }
        }

        public static void WalkParTpl<T>(this BinaryTree<T> root, Action<T> action)
        {
            if (root == null)
            {
                return;
            }

            var left = Task.Factory.StartNew(() => WalkParTpl(root.Left, action));
            var right = Task.Factory.StartNew(() => WalkParTpl(root.Right, action));

            action(root.Value);

            //
            // If the current task finds left or right in its work stealing queue, it executes
            // them inline.
            //

            Task.WaitAll(left, right);
        }
        
    }
}