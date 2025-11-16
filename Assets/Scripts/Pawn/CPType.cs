using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CPType", menuName = "Scriptable/ChessPiece/Type", order = int.MaxValue)]
public class CPType : ScriptableObject
{
    public float[] damageRatio = new float[6]; // 데미지 배율
    [Header("전기")]
    public int eletricTargetCount; // 번개 타겟 수
    [Header("독")]
    public float poisonDamageRatio; // 독 데미지 배율
    public float poisonDamageSpeed; // 독 데미지 속도
    [Header("화염")]
    public float explosionDamageRatio; // 폭발 데미지 배율
    public float explosionRange; // 폭발 범위
    [Header("바람")]
    public float windAttackSpeed; // 바람 공격 속도
    [Header("암흑")]
    public float deathProbability; // 즉사 확률
    [Header("얼음")]
    public float iceSlowRate; // 얼음 느려지는 비율
    public float iceSlowTime; // 얼음 느려지는 시간
    public float iceStopProbability; // 얼음 멈추는 확률
}
