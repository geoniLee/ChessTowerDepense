using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pawn", menuName = "Scriptable/Pawn", order = int.MaxValue)]
public class PawnState : ScriptableObject
{
    public int Damage;
    public float AttackRange;
    public float AttackDelay;
    public GameObject AttackPrefab;
    public bool IsTargetAttack;
}
