using System;
using System.Threading;

namespace PointingPoker.Data
{
    public class CountingItem<T>
    {
        static int Counter = 0;

        public CountingItem(T item)
        {
            Item = item;
            Count = Interlocked.Increment(ref Counter);
        }

        public CountingItem(int count, T item)
        {
            Item = item;
            Count = count;
        }

        public T Item { get; }

        public int Count { get; }
    }
}
