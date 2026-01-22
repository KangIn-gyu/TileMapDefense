using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Pool<T> : IPool where T : Component, IPoolable
{
    private ObjectPool<T> m_Pool;
    private HashSet<T> m_ActiveObjects = new();
    private readonly List<T> m_ReleaseBuffer = new();

    public int TotalCount => m_Pool.CountAll;
    public int ActiveCount => m_Pool.CountActive;
    public int InactiveCount => m_Pool.CountInactive;

    public Component Get() => m_Pool.Get();
    public void Release(Component obj) => m_Pool.Release((T)obj);
    public Pool(T _prefab, Transform _root, int _capacity)
    {
        m_Pool = new ObjectPool<T>(
            () =>
            {
                return GameObject.Instantiate(_prefab, _root);
            },
            obj =>
            {
                m_ActiveObjects.Add(obj);
                obj.gameObject.SetActive(true);
                obj.OnSpawn();
            },
            obj =>
            {
                obj.OnDespawn();
                obj.gameObject.SetActive(false);
                m_ActiveObjects.Remove(obj);
            },
            obj => GameObject.Destroy(obj.gameObject),
            defaultCapacity: _capacity
        );
    }

    public void ReleaseAll()
    {
        if (0 == m_ActiveObjects.Count)
            return;

        m_ReleaseBuffer.Clear();
        m_ReleaseBuffer.AddRange(m_ActiveObjects); 

        for (int i = 0; i < m_ReleaseBuffer.Count; ++i)
        {
            m_Pool.Release(m_ReleaseBuffer[i]);
        }
    }
}
