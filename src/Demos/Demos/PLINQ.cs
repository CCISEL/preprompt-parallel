using System.Linq;
using System.Collections.Generic;

namespace Demos
{
    public class PLINQ
    {
        [Ignore]
        public void Run()
        {
            Utils.Measure("LINQ", () =>
            {
                int[] arr = Enumerable.Range(10000, 20000).ToArray();
                bool[] results = arr
                                .Select(Utils.IsPrime)
                                .ToArray()   ;
            });

            Utils.Measure("PLINQ", () =>
            {
                IEnumerable<int> arr = Enumerable.Range(10000, 20000);
                bool[] results = arr.AsParallel()
                                 .Select(Utils.IsPrime)
                                 .ToArray();
            });

            Utils.Measure("PLINQ Deferred", () =>
            {
                IEnumerable<int> arr = Enumerable.Range(10000, 20000);
                ParallelQuery<bool> results = arr.AsParallel()
                                                 .Select(Utils.IsPrime);
                
                foreach (bool b in results) 
                { }
            });
        }
    }
}
