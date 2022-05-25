using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ambermoon
{
    public class EqualityComparer<T, U> : IEqualityComparer<T>
    {
        readonly Func<T, U> mapper;

        public EqualityComparer(Func<T, U> mapper)
        {
            this.mapper = mapper;
        }

        public bool Equals(T x, T y)
        {
            return mapper(x).Equals(mapper(y));
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return mapper(obj).GetHashCode();
        }
    }
}
