using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CPState", menuName = "Scriptable/ChessPiece/CPState", order = int.MaxValue)]
public class CPState : ScriptableObject
{
    public int Damage;
    public float AttackRange;
    public float AttackDelay;
    public GameObject AttackPrefab;
    public bool IsTargetAttack;
}
