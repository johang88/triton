using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common.Collections
{
    public class TrackingCollection<T> : IList<T>
    {
        private List<T> _items = new List<T>();

        public event Action<T> OnAdd;
        public event Action<T> OnRemove;

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _items[index];
            set
            {
                var old = _items[index];
                _items[index] = value;

                OnRemove?.Invoke(old);
                OnAdd?.Invoke(value);
            }
        }

        public void Add(T item)
        {
            _items.Add(item);
            OnAdd?.Invoke(item);
        }

        public void Clear()
        {
            foreach (var item in _items)
            {
                OnRemove?.Invoke(item);
            }
            _items.Clear();
        }

        public bool Contains(T item)
            => _items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => throw new NotImplementedException();

        public IEnumerator<T> GetEnumerator()
             => _items.GetEnumerator();

        public bool Remove(T item)
        {
            if (_items.Remove(item))
            {
                OnRemove?.Invoke(item);
                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
             => _items.GetEnumerator();

        public int IndexOf(T item)
            => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            OnAdd?.Invoke(item);
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);
            OnRemove?.Invoke(item);
        }
    }
}
