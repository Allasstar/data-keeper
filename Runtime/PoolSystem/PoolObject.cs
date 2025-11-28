using System;
using System.Collections.Generic;

namespace DataKeeper.PoolSystem
{
    public class PoolObject<T>
    {
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>();

        private readonly Func<T> _create;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onGet;

        public int ActiveCount => _active.Count;
        public int InactiveCount => _inactive.Count;

        public IEnumerable<T> ActiveItems => _active;
        public IEnumerable<T> InactiveItems => _inactive;

        public PoolObject(Func<T> create, Action<T> onRelease = null, Action<T> onGet = null)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onRelease = onRelease;
            _onGet = onGet;
        }

        public T Get()
        {
            T item = _inactive.Count > 0
                ? _inactive.Pop()
                : _create();

            _active.Add(item);
            _onGet?.Invoke(item);

            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;

            if (!_active.Contains(item))
                return;

            _active.Remove(item);
            _onRelease?.Invoke(item);
            _inactive.Push(item);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                _inactive.Push(_create());
        }

        public void Clear()
        {
            _active.Clear();
            _inactive.Clear();
        }
    }
}