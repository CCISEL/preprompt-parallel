using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindInFiles.SearchBehaviors
{
    public interface ISearchBehavior
    {
        void Start(string directory, IEnumerable<string> wildcards, Regex regex, CancellationToken token,
                   Action<Match[]> onMatched, Action<long> onComplete);
    }
}