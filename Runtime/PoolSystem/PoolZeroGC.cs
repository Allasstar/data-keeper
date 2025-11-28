using System;

namespace DataKeeper.PoolSystem
{
    public sealed class PoolZeroGC<T>
    {
        private readonly T[] _items;
        private readonly bool[] _activeFlags;
        private readonly int _capacity;

        private int _inactiveCount;
        
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public int ActiveCount => _capacity - _inactiveCount;
        public int InactiveCount => _inactiveCount;
        public int Capacity => _capacity;

        public PoolZeroGC(
            int capacity,
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be > 0");

            _capacity = capacity;
            _items = new T[capacity];
            _activeFlags = new bool[capacity];
            _inactiveCount = capacity;

            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;

            for (int i = 0; i < capacity; i++)
            {
                _items[i] = _createFunc();
                _activeFlags[i] = false;
            }
        }

        public T Get()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (!_activeFlags[i])
                {
                    _activeFlags[i] = true;
                    _inactiveCount--;

                    T item = _items[i];
                    _onGet?.Invoke(item);
                    return item;
                }
            }

            return default;
        }

        public void Release(T item)
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (_activeFlags[i] && ReferenceEquals(_items[i], item))
                {
                    _activeFlags[i] = false;
                    _inactiveCount++;

                    _onRelease?.Invoke(item);
                    return;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _capacity; i++)
                _activeFlags[i] = false;

            _inactiveCount = _capacity;
        }
    }
}