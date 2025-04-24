using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Core.DataStructure
{
    
    public class MaxHeap<T> where T : IComparable<T>
    {
        public List<T> container = new List<T>();
        int cnt = 0;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return cnt; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool cmp(T a, T b)
        {
            return a.CompareTo(b) > 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void swap(int a, int b)
        {
            T tmp = container[a]; container[a] = container[b]; container[b] = tmp;
        }
        void up()
        {
            int pos = cnt - 1;
            while (pos > 0)
            {
                int father = (pos - 1) >> 1;
                //pos>father
                if (cmp(container[pos], container[father]))
                {
                    swap(pos, father);
                    pos = father;
                }
                else
                {
                    break;
                }
            }
        }
        void down()
        {
            int pos = 0, leftChild;
            while ((leftChild = (pos << 1) + 1) < cnt)
            {
                int swapper = leftChild;
                if (leftChild + 1 < cnt && cmp(container[leftChild + 1], container[leftChild]))
                {
                    //right>left
                    swapper++;
                }
                //swapper > pos
                if (cmp(container[swapper], container[pos]))
                {
                    swap(pos, swapper);
                    pos = swapper;
                }
                else
                {
                    break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            container.Add(item);
            cnt++;
            up();
        }
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return cnt == 0; }
        }
        public T Pop()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("堆为空");
            }
            T res = container[0];
            --cnt;
            container[0] = container[cnt];
            container.RemoveAt(cnt);
            down();
            return res;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException("堆为空");
            }
            return container[0];
        }
    }

    public sealed class MinHeap<T> : MaxHeap<T> where T: IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected sealed override bool cmp(T a, T b)
        {
            return !base.cmp(a, b);
        }
    }
}