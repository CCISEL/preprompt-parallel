using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindInFiles.SearchBehaviors
{
    internal static class Utils
    {
        //
        // Finds all the lines in the specified file that match the regex.
        //

        public static Match[] FindMatches(this string file, Regex regex, CancellationToken token)
        {
            //
            // Read all the lines from the file and assign them a line number.
            // When evaluating the "matches" query, an instance of Match will
            // be created for each line. Filter the lines for the ones that 
            // match the regular expression.
            //

            var matches = File
                        .ReadLines(file)
                        .Zip(Enumerable.Range(1, int.MaxValue),
                             (line, index) => new Match
                             {
                                 File = file,
                                 Line = index,
                                 Text = line,
                             })
                        .Where(match => regex.IsMatch(match.Text));

            //
            // Check if cancellation is requested.
            //

            if (token.IsCancellationRequested)
            {
                return null;
            }

            //
            // Evaluate the query.
            //

            return matches.ToArray();
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}
