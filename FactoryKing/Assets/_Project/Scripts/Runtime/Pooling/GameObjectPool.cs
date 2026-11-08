using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryKing.Runtime.Pooling
{
    /// <summary>Minimal allocation-free pool of components (crates on belts, effects...).</summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Stack<T> _free = new Stack<T>();
        private readonly Func<T> _create;
        private readonly Transform _storage;

        public ComponentPool(Func<T> create, Transform storage)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _storage = storage;
        }

        public int CountFree => _free.Count;
        public int CountCreated { get; private set; }

        public T Get(Transform parent)
        {
            T item;
            if (_free.Count > 0) item = _free.Pop();
            else { item = _create(); CountCreated++; }
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(_storage, false);
            _free.Push(item);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = _create();
                CountCreated++;
                Release(item);
            }
        }
    }
}
