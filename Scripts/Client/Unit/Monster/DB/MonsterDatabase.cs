using System.Collections.Generic;
using UnityEngine;

public class MonsterDatabase : ScriptableObject
{
    public List<MonsterData> Monsters = new();

    public void Register(MonsterData _data)
    {
        if (!Monsters.Contains(_data))
            Monsters.Add(_data);
    }

    public void Unregister(MonsterData _data)
    {
        Monsters.Remove(_data);
    }

    public MonsterData FindById(string _id)
    {
        return Monsters.Find(m => m != null && m.m_ID == _id);
    }
}
