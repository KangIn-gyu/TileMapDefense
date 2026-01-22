using System.Collections.Generic;
using UnityEngine;

public class OperatorDatabase : ScriptableObject
{
    public List<OperatorData> Operators = new();

    public void Register(OperatorData _data)
    {
        if (!Operators.Contains(_data))
            Operators.Add(_data);
    }

    public void Unregister(OperatorData _data)
    {
        Operators.Remove(_data);
    }

    public OperatorData FindById(string _id)
    {
        return Operators.Find(m => m != null && m.m_ID == _id);
    }
}
