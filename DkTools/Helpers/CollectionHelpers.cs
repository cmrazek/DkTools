using System.Collections.Generic;
using System.Linq;

namespace DkTools.Helpers
{
    internal static class CollectionHelpers
    {
        public static bool HasSameContent<T>(this IEnumerable<T> left, IEnumerable<T> right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;
            if (left.Count() != right.Count()) return false;

            var lit = left.GetEnumerator();
            var rit = right.GetEnumerator();
            while (true)
            {
                var lend = lit.MoveNext();
                var rend = rit.MoveNext();
                if (lend != rend) return false;
                if (!lend) return true;
                if (!EqualityComparer<T>.Default.Equals(lit.Current, rit.Current)) return false;
            }
        }
    }
}
