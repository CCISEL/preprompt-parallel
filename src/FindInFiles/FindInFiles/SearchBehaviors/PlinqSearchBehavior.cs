using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FindInFiles.SearchBehaviors
{
    internal sealed class PlinqSearchBehavior : ISearchBehavior
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

            var stopwatch = new Stopwatch();

            Task.Factory
            .StartNew(() =>
            {
                stopwatch.Start();

                var files = wildcards.SelectMany(wc => Directory.EnumerateFiles(directory, wc, SearchOption.AllDirectories));

                var query = from file in files.AsParallel().WithCancellation(token)
                            from match in File.ReadLines(file).AsParallel()
                                            .Zip(Enumerable.Range(1, int.MaxValue).AsParallel(),
                                            (s, i) => new Match
                                            {
                                                File = file,
                                                Line = i,
                                                Text = s,
                                            })
                            where regex.IsMatch(match.Text)
                            group match by match.File into g
                            select g;
                try
                {
                    //
                    // Evaluate the query in the context of the current task. We could also 
                    // use deferred execution and show the matches chunk by chunk.
                    //

                    foreach (var matches in query)
                    {
                        var matchesArray = matches.ToArray();
                        context.Post(_ => onMatched(matchesArray), null);
                    }
                }
                catch (OperationCanceledException)
                { }

                stopwatch.Stop();
                context.Post(_ => onComplete(stopwatch.ElapsedMilliseconds), null);
            });
        }
    }
}