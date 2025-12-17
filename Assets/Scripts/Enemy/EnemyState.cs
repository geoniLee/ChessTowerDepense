using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Monster", menuName = "Scriptable/Monster", order = int.MaxValue)]
public class EnemyState : ScriptableObject
{
    public int enemyGrade = 0; // 등급 (0:Pawn, 1:Knight, 2:Bishop, 3:Rook, 4:Queen, 5:King)
    public int waveCost = 10; // 이 몬스터의 웨이브 비용
    public int Health;
    public float Speed;
    public int Damage;
    public float StunTime;
}
