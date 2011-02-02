using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindInFiles.SearchBehaviors
{
    internal class MySynchronizationContext : SynchronizationContext
    {
        private const int INITIAL_WAIT_TIMEOUT = 20;
        private const int WAIT_TIMED_OUT = 0x102;

        private readonly Func<bool> _shouldCreate;
        private readonly ThreadStart _threadStart;

        public MySynchronizationContext(Func<bool> shouldCreate, ThreadStart threadStart)
        {
            _shouldCreate = shouldCreate;
            _threadStart = threadStart;
            SetWaitNotificationRequired();
        }

        //
        // This function intercepts a blocking call on a Win32 object. It replaces the timeout
        // value with a smaller one, and when it expires checks if a new thread should be created.
        //

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            int effectiveTimeout = millisecondsTimeout == Timeout.Infinite ||
                                   millisecondsTimeout > INITIAL_WAIT_TIMEOUT
                                 ? INITIAL_WAIT_TIMEOUT
                                 : millisecondsTimeout;

            int result = WaitHelper(waitHandles, waitAll, effectiveTimeout);
            if (result == WAIT_TIMED_OUT && (millisecondsTimeout == Timeout.Infinite ||
                                             millisecondsTimeout > INITIAL_WAIT_TIMEOUT))
            {
                if (_shouldCreate())
                {
                    new Thread(_threadStart) { IsBackground = true }.Start();
                }
                result = WaitHelper(waitHandles, waitAll, millisecondsTimeout);
            }

            return result;
        }
    }

    internal sealed class ThreadsSearchBehavior : ISearchBehavior
    {
        private readonly BlockingCollection<Action> _actions;

        public ThreadsSearchBehavior()
        {
            _actions = new BlockingCollection<Action>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                new Thread(() => thread_start(Timeout.Infinite)) { IsBackground = true }.Start();
            }
        }

        private void thread_start(int timeout)
        {
            const int oversubscribeTimeout = 100;

            //
            // Set the current synchronization context. The thread start function is
            // a call to this function with a timeout != Timeout.Infinite as argument.
            //

            SynchronizationContext.SetSynchronizationContext(
                new MySynchronizationContext(() => _actions.Count > 0,
                                             () => thread_start(oversubscribeTimeout)));
            do
            {
                Action action;
                if (!_actions.TryTake(out action, timeout))
                {
                    return;
                }
                action();
            } while (true);
        }

        public void Start(string directory, IEnumerable<string> wildcards, Regex regex, CancellationToken token,
                          Action<Match[]> onMatched, Action<long> onComplete)
        {
            //
            // Capture the GUI (the caller) thread's synchronization context.
            //

            var context = SynchronizationContext.Current;

            //
            // Move execution to another thread.
            //

            _actions.Add(() =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var files = wildcards.SelectMany(wildcard => Directory.EnumerateFiles(directory,
                                                                                      wildcard,
                                                                                      SearchOption.AllDirectories));

                int totalFiles = 0;
                int localTotalFiles = 0;

                //
                // For each wildcard select all the files that match the wildcard; for each file
                // find the matches in the file and send them to the OnMatches subscribers (one
                // batch at a time to minimize SynchronizationContext calls).
                //

                foreach (string file in files)
                {
                    string localFile = file;
                    _actions.Add(() =>
                    {
                        var matches = localFile.FindMatches(regex, token);
                        context.Post(_ => onMatched(matches), null);

                        //
                        // Synchronize with the worker threads and with the parent action.
                        //

                        if (Interlocked.Decrement(ref totalFiles) == 0)
                        {
                            stopwatch.Stop();
                            context.Post(_ => onComplete(stopwatch.ElapsedMilliseconds), null);
                        }
                    });

                    localTotalFiles += 1;
                }

                //
                // Synchronize with the worker threads.
                //

                if (Interlocked.Add(ref totalFiles, localTotalFiles) == 0)
                {
                    stopwatch.Stop();
                    context.Post(_ => onComplete(stopwatch.ElapsedMilliseconds), null);
                }
            });
        }
    }
}