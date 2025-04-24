using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Core.DataStructure
{
    public class BufferedCollection<T,T3,T2> where T : ICollection<T2>,new() where T3 : ICollection<T2>,new()
    {
        private T[] items;
        private T3 pusher = new T3();
        private T3 pusherSwap = new T3();
        public T[] current => items;
        public T3 buffer => pusher;
        public int ItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var count = pusher.Count + pusherSwap.Count;
                foreach (var collection in items)
                {
                    count += collection.Count;
                }
                return count;
            }
        }

        public BufferedCollection(int count)
        {
            items = new T[count];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new T();
            }
        }
        // Method to get an iterator for all elements in both collections
        public IEnumerator<T2> All()
        {
            foreach (var item in items)
            {
                foreach(var subitem in item)
                {
                    yield return subitem;
                }
            }
            foreach (var item in pusher)
            {
                yield return item;
            }
        }

        public void SwapBuffer()
        {
            var temp = pusher;
            pusher = pusherSwap;
            pusherSwap = temp;
        }


        public void Clear()
        {
            buffer.Clear();
            foreach (var collection in items)
            {
                collection.Clear();
            } 
        }

    }
}