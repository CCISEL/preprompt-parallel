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
    internal sealed class TplSearchBehavior : ISearchBehavior
    {
        public void Start(string directory, IEnumerable<string> wildcards, Regex regex, CancellationToken token,
                          Action<Match[]> onMatched, Action<long> onComplete)
        {           
            //
            // Move execution to another thread.
            //

            var stopwatch = new Stopwatch();
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task.Factory.StartNew(() =>
            {
                stopwatch.Start();
                return wildcards.SelectMany(wc => Directory.EnumerateFiles(directory, wc, SearchOption.AllDirectories));
            })
            .ContinueWith(filesTask => filesTask.Result.ForEach(file =>
                Task.Factory
                    .StartNew(() => file.FindMatches(regex, token),
                              token,
                              TaskCreationOptions.None,
                              TaskScheduler.Default) /* Use the ThreadPoolTaskScheduler. */
                    .ContinueWith(t => onMatched(t.Result),
                                  token,
                                  TaskContinuationOptions.AttachedToParent, /* Delay the completion of the containing continuation. */
                                  scheduler))) /* Required only because of the overload definition. */
            .ContinueWith(_ =>
            {
                stopwatch.Stop();
                onComplete(stopwatch.ElapsedMilliseconds);
            }, scheduler);
        }
    }
}