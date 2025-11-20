using System.Collections;
using UnityEngine;

/// <summary>
/// 파티클 시스템이나 애니메이션이 끝나면 자동으로 GameObject를 파괴하는 컴포넌트
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [Tooltip("파티클 시스템 재생 시간 후 파괴 (0이면 자동 감지)")]
    public float destroyDelay = 0f;

    [Tooltip("파티클 시스템 자동 감지 활성화")]
    public bool autoDetectParticle = true;

    void Start()
    {
        float delay = destroyDelay;

        // 파티클 시스템 자동 감지
        if (autoDetectParticle && delay <= 0f)
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 파티클의 duration + startLifetime 사용
                delay = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }

        // 기본값 설정 (감지 실패 시)
        if (delay <= 0f)
        {
            delay = 2f; // 기본 2초 후 파괴
        }

        Destroy(gameObject, delay);
    }
}
