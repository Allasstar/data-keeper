using System;
using System.Collections.Generic;

namespace DataKeeper.PoolSystem
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onRelease;

        public ObjectPool(Func<T> createFunc, Action<T> onRelease = null)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onRelease = onRelease;
        }

        public T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            return _createFunc();
        }

        public void Release(T item)
        {
            if (item == null) return;
            
            _onRelease?.Invoke(item);
            _pool.Push(item);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public int PoolCount => _pool.Count;
    }
}