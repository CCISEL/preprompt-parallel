using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindInFiles.SearchBehaviors
{
    internal sealed class SequentialSearchBehavior : ISearchBehavior
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

                //
                // For each wildcard select all the files that match the wildcard; for each file
                // find the matches in the file and call the onMatched delegate with one batch at 
                // a time to minimize SynchronizationContext calls.
                //

                wildcards
                .SelectMany(wildcard => Directory.EnumerateFiles(directory, wildcard, SearchOption.AllDirectories))
                .Select(file => file.FindMatches(regex, token))
                .ForEach(matches =>
                {
                    context.Post(x => onMatched(matches), null);

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                });

                stopwatch.Stop();
                context.Post(x => onComplete(stopwatch.ElapsedMilliseconds), null);
            });
        }
    }
}