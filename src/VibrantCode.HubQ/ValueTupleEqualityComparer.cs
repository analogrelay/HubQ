using System;
using System.Collections.Generic;

namespace HubSync
{
    internal static class ValueTupleEqualityComparer
    {
        public static ValueTupleEqualityComparer<T1, T2> Create<T1, T2>(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2)
        {
            return new ValueTupleEqualityComparer<T1, T2>(comparer1, comparer2);
        }
    }

    internal class ValueTupleEqualityComparer<T1, T2> : IEqualityComparer<ValueTuple<T1, T2>>
    {
        private readonly IEqualityComparer<T1> _comparer1;
        private readonly IEqualityComparer<T2> _comparer2;

        public ValueTupleEqualityComparer(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2)
        {
            _comparer1 = comparer1;
            _comparer2 = comparer2;
        }

        public bool Equals((T1, T2) x, (T1, T2) y)
        {
            return
                _comparer1.Equals(x.Item1, y.Item1) &&
                _comparer2.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode((T1, T2) t)
        {
            var hashCode = -504981047;
            hashCode = hashCode * -1521134295 + (t.Item1 == null ? 0 : _comparer1.GetHashCode(t.Item1));
            hashCode = hashCode * -1521134295 + (t.Item2 == null ? 0 : _comparer2.GetHashCode(t.Item2));
            return hashCode;
        }
    }
}
