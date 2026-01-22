using UnityEngine;

public interface IPool
{
    public Component Get();
    public void Release(Component obj);

    public void ReleaseAll();

    public int TotalCount { get;}
    public int ActiveCount { get; }
    public int InactiveCount { get; }
}