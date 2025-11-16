using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단한 폭발 이펙트
/// - CircleCollider2D(isTrigger)를 사용하여 주변의 적을 감지하고 데미지를 전달합니다.
/// - InitExplosion(Vector2(range, damage))으로 초기화합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Explosion : MonoBehaviour
{
    private float damage = 1f;
    private float range = 1f;
    private float lifetime = 0.3f;
    private CircleCollider2D cc;

    private void Awake()
    {
        cc = GetComponent<CircleCollider2D>();
        if (cc == null)
            cc = gameObject.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
    }

    // Init with (range, damage)
    private void Init(Vector2 args)
    {
        range = args.x;
        damage = args.y;
        if (cc != null)
        {
            cc.radius = Mathf.Max(0.01f, range);
        }

        // 자동 제거
        StartCoroutine(LifeCoroutine());
    }

    // SendMessage entrypoint used by Spawn code
    private void InitExplosion(Vector2 args)
    {
        Init(args);
    }

    private IEnumerator LifeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;

        // 우선 EnemyNav가 있으면 TakeDamage 호출
        var en = other.GetComponent<EnemyNav>();
        if (en != null)
        {
            en.TakeDamage(Mathf.RoundToInt(damage));
            return;
        }

        // 그 외에는 SendMessage로 Try
        other.gameObject.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
    }
}
