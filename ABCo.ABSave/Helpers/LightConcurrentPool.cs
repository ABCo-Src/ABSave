namespace ABCo.ABSave.Helpers
{
    internal class LightConcurrentPool<T> where T : class
    {
        int _itemCount = 0;
        readonly T[] _items;

        public LightConcurrentPool(int maxCapacity) => _items = new T[maxCapacity];

        public T? TryRent()
        {
            lock (_items)
            {
                if (_itemCount == 0) return null;
                return _items[_itemCount--];
            }
        }

        public void Release(T item)
        {
            lock (_items)
            {
                int newCount = _itemCount + 1;
                if (newCount == _items.Length) return;

                _items[_itemCount] = item;
                _itemCount = newCount;
            }
        }
    }
}