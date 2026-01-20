using UnityEngine;

public interface IValueUI<T>
{
    public void OnValueChanged(T _value);
}
