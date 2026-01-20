using System;
using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;

public enum PoolKey
{
    Tile,
    OperatorSlot,
}

/// <summary>
/// 순수 보기용 
/// </summary>
#if UNITY_EDITOR
[Serializable]
public struct PoolData
{
    [SerializeField] private int m_TotalCount;
    [SerializeField] private int m_ActiveCount;
    [SerializeField] private int m_InactiveCount;

    public void Count(int _total, int _active, int _inactive)
    {
        m_TotalCount = _total;
        m_ActiveCount = _active;
        m_InactiveCount = _inactive;    
    }
}
#endif
public class ObjectPoolManager : MonoBehaviour
{
    private Dictionary<Enum, IPool> pools = new();

#if UNITY_EDITOR
    [SerializeField] private SerializedDictionary<string, PoolData> m_DebugDictionary = new ();
#endif

    public void Register<T>(Enum _key, T _prefab, Transform _root, int _capacity = 10 ) where T : Component, IPoolable
    {
        if (false == pools.ContainsKey(_key))
        {
            pools[_key] = new Pool<T>(_prefab, _root, _capacity);
#if UNITY_EDITOR
            m_DebugDictionary[_key.ToString()] = new PoolData();
#endif
        }
    }

    public T Spawn<T>(Enum _key) where T : Component
    {
        var obj = pools[_key].Get() as T;
#if UNITY_EDITOR
        DubugDictionaryUpdate(_key);
#endif
        return obj;
    }

    public void Release(Enum _key, Component _obj)
    {
        pools[_key].Release(_obj);
#if UNITY_EDITOR
        DubugDictionaryUpdate(_key);
#endif
    }

    public void ReleaseAll(Enum _key)
    {
        if(true == pools.ContainsKey(_key))
        {
            pools[_key].ReleaseAll();
        }
    }

#if UNITY_EDITOR
    private void DubugDictionaryUpdate(Enum _key)
    {
        if (true == m_DebugDictionary.TryGetValue(_key.ToString(), out var data))
        {
            if(true == pools.TryGetValue(_key, out var pool))
            {
                data.Count(pool.TotalCount, pool.ActiveCount, pool.InactiveCount);
            }
        }
    }
#endif
}
