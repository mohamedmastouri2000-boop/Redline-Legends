using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedlineLegends.Utilities
{
    /// <summary>
    /// Component pool for VFX/audio/skid marks. Pre-warms on construction so no Instantiate happens
    /// during a race; if the pool runs dry it grows rather than failing.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _free;
        private readonly List<T> _all;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public int CountAll => _all.Count;
        public int CountFree => _free.Count;

        public ObjectPool(T prefab, int prewarm, Transform parent = null, Action<T> onGet = null, Action<T> onRelease = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            _prefab = prefab;
            _parent = parent;
            _onGet = onGet;
            _onRelease = onRelease;
            _free = new Stack<T>(prewarm);
            _all = new List<T>(prewarm);
            for (int i = 0; i < prewarm; i++)
                _free.Push(CreateNew());
        }

        private T CreateNew()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            _all.Add(instance);
            return instance;
        }

        public T Get()
        {
            var instance = _free.Count > 0 ? _free.Pop() : CreateNew();
            instance.gameObject.SetActive(true);
            _onGet?.Invoke(instance);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null) return;
            _onRelease?.Invoke(instance);
            instance.gameObject.SetActive(false);
            if (_parent != null) instance.transform.SetParent(_parent, false);
            _free.Push(instance);
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < _all.Count; i++)
            {
                var instance = _all[i];
                if (instance != null && instance.gameObject.activeSelf)
                    Release(instance);
            }
        }

        public void DestroyAll()
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null) UnityEngine.Object.Destroy(_all[i].gameObject);
            _all.Clear();
            _free.Clear();
        }
    }
}
