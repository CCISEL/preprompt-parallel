using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindInFiles.SearchBehaviors
{
    internal sealed class ThreadPoolSearchBehavior : ISearchBehavior
    {
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

            ThreadPool.QueueUserWorkItem(delegate
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
                // find the matches in the file and call the onMatched delegate with one batch at 
                // a time to minimize SynchronizationContext calls.
                //

                foreach (string file in files)
                {
                    string localFile = file;
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        var matches = localFile.FindMatches(regex, token);
                        context.Post(_ => onMatched(matches), null);

                        //
                        // Synchronize with the worker threads and with the parent work item.
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