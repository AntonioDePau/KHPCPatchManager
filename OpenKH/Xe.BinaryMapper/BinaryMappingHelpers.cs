using System;
using System.Collections.Generic;
using System.Linq;

namespace Xe.BinaryMapper
{
    public static class BinaryMappingHelpers
    {
        public static int TryGetCount<TItem>(this List<TItem> list) => list?.Count ?? 0;

        public static List<TItem> CreateOrResize<TItem>(this List<TItem> list, int count)
            where TItem : new()
        {
            list = list ?? new List<TItem>();
            var difference = list.Count - count;
            if (difference < 0)
            {
                list.AddRange(Enumerable.Range(0, -difference).Select(x => new TItem()));
            }
            else if (difference > 0)
            {
                list.RemoveRange(count, difference);
            }

            return list;
        }
    }
}
