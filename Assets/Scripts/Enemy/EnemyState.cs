using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Monster", menuName = "Scriptable/Monster", order = int.MaxValue)]
public class EnemyState : ScriptableObject
{
    public int Health;
    public float Speed;
    public int Damage;
    public float StunTime;
}
