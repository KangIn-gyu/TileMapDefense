using UnityEngine;

public enum AttackDirection
{
    UP, 
    DOWN, 
    LEFT, 
    RIGHT
}

public interface IUnit
{
    public void Attack();
    public void TakeDamage(float _damage);
}
