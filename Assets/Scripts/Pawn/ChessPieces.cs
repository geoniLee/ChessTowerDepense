using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPieces : MonoBehaviour
{
    public CPState pawnState;
    public float attackRange;
    private int enemyLayerMask;
    private float attackDelay = 100;
    private int type;
    private int typeLevel;
    private EnemyNav target;
    Collider2D enemy = null;

    // 합성용 등급 (0: 기본, 1: 1단계, 2: 2단계, ...)
    public int grade = 0;

    private Coroutine windCoroutine;
    
    // 적에게 공격당한 후 공격 불가 시간
    private float lastHitByEnemyTime = -10f; // 마지막으로 적에게 공격당한 시간
    private const float attackDisableDuration = 1f; // 공격 불가 지속 시간 (1초)
    
    // 킹 버프 시스템은 GameManager에서 전역 관리됨

    void Awake()
    {
        pawnState = Resources.Load<CPState>("State/Player/ChessPiece/" + gameObject.name.Replace("(Clone)", ""));
    }
    void Start()
    {
        type = Random.Range(0, 6);
        // 타입에 따라 색상 적용
        ApplyTypeColor();
        enemyLayerMask = LayerMask.GetMask("Enemy");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCPTypeUpgraded += OnUpgradeNotified;
            if (GameManager.Instance.CPTypeLevel != null && type >= 0 && type < GameManager.Instance.CPTypeLevel.Length)
                typeLevel = GameManager.Instance.CPTypeLevel[type];
            else
                typeLevel = 0;
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 null입니다. OnCPTypeUpgraded 구독을 건너뜁니다.");
            typeLevel = 0;
        }

        if (pawnState != null)
        {
            attackRange = pawnState.AttackRange;
            attackDelay = pawnState.AttackDelay;
        }
        else
        {
            Debug.LogWarning($"pawnState 리소스가 없습니다: {gameObject.name}");
            attackRange = 1f;
            attackDelay = 1f;
        }
    }

    // 타입에 따라 오브젝트의 색상을 설정합니다.
    private void ApplyTypeColor()
    {
        Color col = Color.white;
        switch (type)
        {
            case 0: col = new Color(1, 1, 179f/255f); break; // 전기 - 노랑
            case 1: col = new Color(198f/255f, 179f/255f, 1); break; // 독 - 연두
            case 2: col = new Color(1, 179f/255f, 179f/255f); break; // 불 - 빨강
            case 3: col = new Color(179f/255f, 1, 1); break; // 바람 - 하늘색
            case 4: col = new Color(90f/255f, 90f/255f, 100f/255f); break; // 암흑 - 보라
            case 5: col = new Color(105f/255f, 213f/255f, 1); break; // 얼음 - 파랑
            default: col = Color.white; break;
        }

        // SpriteRenderer 우선 적용 (2D)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = col;
            return;
        }
    }

    void OnDestroy()
    {
        GameManager.Instance.OnCPTypeUpgraded -= OnUpgradeNotified;
    }

    // 속성 레벨을 갱신
    public void OnUpgradeNotified()
    {
        typeLevel = GameManager.Instance.CPTypeLevel[type];
    }

    // Update is called once per frame
    void Update()
    {
        // 적에게 공격당한 후 공격 불가 시간이면 공격하지 않음
        if (!CanAttack()) return;

        // 공격 딜레이 감소
        attackDelay -= Time.deltaTime;
        if (attackDelay <= 0)
        {
            // 공격 시점에 골 지점에 가장 가까운 타겟 탐색
            target = FindClosestEnemyToGoal();
            if (target == null)
            {
                attackDelay = 0.1f; // 타겟 없으면 짧은 딜레이 후 재탐색
                return;
            }

            // 타입별 공격 효과 적용
            ApplyAttackEffect(target);

            // 킹 버프 적용: 아군 킹 개수에 따라 공격속도 배율
            int kingCount = GameManager.Instance != null ? GameManager.Instance.GetAllyKingCount() : 0;
            float attackSpeedMultiplier = 1.0f + Mathf.Min(kingCount, 3) * 1.0f;
            
            // 기본 공격 지연 초기화 (바람 타입 등은 내부에서 조정 가능)
            attackDelay = pawnState.AttackDelay / attackSpeedMultiplier;

            // 타격 후 초기화 (다음 공격 시 새 타겟 탐색)
            enemy = null;
            target = null;
        }
    }

    // 골까지 경로상 가장 가까운 적 탐색 (진행도 기준)
    private EnemyNav FindClosestEnemyToGoal()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
        if (enemies.Length == 0) return null;

        EnemyNav closestEnemy = null;
        float shortestRemainingPath = float.MaxValue;

        foreach (Collider2D enemyCollider in enemies)
        {
            EnemyNav enemyNav = enemyCollider.GetComponent<EnemyNav>();
            if (enemyNav == null) continue;

            float remainingPath = enemyNav.GetRemainingPathDistance();
            if (remainingPath < shortestRemainingPath)
            {
                shortestRemainingPath = remainingPath;
                closestEnemy = enemyNav;
                enemy = enemyCollider;
            }
        }

        return closestEnemy;
    }

    // 타입별 공격 효과 진입점
    private void ApplyAttackEffect(EnemyNav enemyNav)
    {
        if (enemyNav == null) return;

        CPType cp = GameManager.Instance != null ? GameManager.Instance._CPType : null;
        int baseDamage = pawnState != null ? pawnState.Damage : 1;
        
        // CPType의 damageRatio 배율 적용 (레벨당 +0.5)
        float ratio = 1.0f; // 기본 배율
        if (cp != null && cp.damageRatio != null && type >= 0 && type < cp.damageRatio.Length)
        {
            // 기본 배율 + (레벨 - 1) * 0.5
            ratio = cp.damageRatio[type] + (typeLevel - 1) * 0.5f;
        }
        
        // 킹 버프 적용: 아군 킹 개수에 따라 데미지 배율 (1개: 2배, 2개: 3배, 3개 이상: 4배)
        int kingCount = GameManager.Instance != null ? GameManager.Instance.GetAllyKingCount() : 0;
        float kingBuffMultiplier = 1.0f + Mathf.Min(kingCount, 3) * 1.0f;
        
        float damageWithLevel = baseDamage * ratio * kingBuffMultiplier;
        
        // 5레벨당 특성 강화 계산
        int levelBonus = (typeLevel - 1) / 5; // 1~4렉: 0, 5~9렉: 1, 10~14렉: 2...

        // 공격 타입 로그 출력
        string[] typeNames = new string[] { "Electric", "Poison", "Explosion", "Wind", "Death", "Ice" };
        string tname = (type >= 0 && type < typeNames.Length) ? typeNames[type] : "Unknown";
        
        switch (type)
        {
            case 0: // 전기: 체인 전이 (5렉당 타겟 +1)
                int electricTargets = cp.eletricTargetCount + levelBonus;
                StartCoroutine(ElectricChain(enemyNav, electricTargets, damageWithLevel));
                break;
            case 1: // 독: 주기적 데미지 (5렉당 틱 +1)
                StartCoroutine(DoPoison(enemyNav, cp.poisonDamageRatio, cp.poisonDamageSpeed, damageWithLevel, 5 + levelBonus));
                break;
            case 2: // 폭발: 프리팹 생성 (5렉당 범위 +0.5, 배율 +0.05)
                float explosionRange = cp.explosionRange + (levelBonus * 0.5f);
                float explosionRatio = cp.explosionDamageRatio + (levelBonus * 0.05f);
                DoExplosionPrefab(enemyNav, explosionRange, explosionRatio, damageWithLevel);
                break;
            case 3: // 바람: 공격 속도 (5렉당 +0.2)
                float windSpeed = cp.windAttackSpeed + (levelBonus * 0.2f);
                ApplyWind(windSpeed, enemyNav);
                enemyNav.Damaged(Mathf.RoundToInt(damageWithLevel));
                break;
            case 4: // 즉사 확률 (5렉당 +0.005)
                float deathProb = cp.deathProbability + (levelBonus * 0.005f);
                TryInstantKill(enemyNav, deathProb, damageWithLevel);
                break;
            case 5: // 얼음: 슬로우 및 멈춤 확률 (5렉당 정지확률 +0.01)
                float iceStopProb = cp.iceStopProbability + (levelBonus * 0.01f);
                ApplyIceEffect(enemyNav, cp.iceSlowRate, cp.iceSlowTime, iceStopProb, damageWithLevel);
                break;
            default:
                enemyNav.Damaged(Mathf.RoundToInt(damageWithLevel));
                break;
        }
    }

    // 이펙트 생성 헬퍼 메서드 (allEffects 하위에 생성)
    private void SpawnEffect(int typeIndex, Transform target)
    {
        if (target == null) return;
        if (GameManager.Instance == null || GameManager.Instance.typeEffectPrefabs == null)
            return;
            
        if (typeIndex < 0 || typeIndex >= GameManager.Instance.typeEffectPrefabs.Length)
            return;
            
        GameObject effectPrefab = GameManager.Instance.typeEffectPrefabs[typeIndex];
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, target.position, Quaternion.identity);
            if (GameManager.Instance.allEffects != null)
                effect.transform.SetParent(GameManager.Instance.allEffects.transform);
        }
    }

    // 0: 전기 체인
    private IEnumerator ElectricChain(EnemyNav start, int maxTargets, float damage)
    {
        if (start == null) yield break;
        HashSet<GameObject> hit = new HashSet<GameObject>();
        Queue<EnemyNav> q = new Queue<EnemyNav>();
        q.Enqueue(start);
        int count = 0;
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        while (q.Count > 0 && count < maxTargets)
        {
            var cur = q.Dequeue();
            if (cur == null) continue;
            var go = cur.gameObject;
            if (hit.Contains(go)) continue;
            
            // 데미지 적용
            cur.Damaged(Mathf.RoundToInt(damage));
            hit.Add(go);
            count++;

            // 이펙트 생성 (타입 0: Electric)
            SpawnEffect(0, go.transform);

            // 주변 적 추가
            var cols = Physics2D.OverlapCircleAll(go.transform.position, attackRange, 1 << enemyLayer);
            foreach (var c in cols)
            {
                var en = c.GetComponent<EnemyNav>();
                if (en != null && !hit.Contains(en.gameObject)) q.Enqueue(en);
            }

            yield return new WaitForSeconds(0.08f);
        }
    }

    // 1: 독 데미지 — 즉시 데미지 후 주기적 독 데미지 (틱마다 이펙트 생성)
    private IEnumerator DoPoison(EnemyNav enemyNav, float ratio, float period, float baseDamage, int ticks = 5)
    {
        if (enemyNav == null) yield break;

        // 즉시 기본 데미지 적용 + 이펙트
        SpawnEffect(1, enemyNav.transform);
        int immediateDmg = Mathf.RoundToInt(baseDamage);
        enemyNav.Damaged(immediateDmg);

        // 짧은 지연 후 독(주기) 데미지 적용
        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < ticks; i++)
        {
            Debug.Log($"[Poison] Tick {i + 1}/{ticks} 적용: {Mathf.Max(1, Mathf.RoundToInt(baseDamage * ratio))} 데미지");
            if (enemyNav == null) yield break; // 적이 사망했으면 중단
            
            // 틱마다 독 이펙트 생성
            SpawnEffect(1, enemyNav.transform);
            
            int tickDmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * ratio));
            enemyNav.Damaged(tickDmg);
            yield return new WaitForSeconds(Mathf.Max(0.01f, period));
        }
    }

    // 2: 폭발 이펙트 생성 (이펙트 프리팹이 Explosion 컴포넌트로 공격 처리)
    private void DoExplosionPrefab(EnemyNav enemyNav, float range, float damageRatio, float baseDamage)
    {
        if (enemyNav == null) return;
        
        // GameManager에서 폭발 이펙트 프리팹 가져오기
        if (GameManager.Instance == null || GameManager.Instance.typeEffectPrefabs == null)
        {
            Debug.LogWarning("[ChessPieces] GameManager 또는 typeEffectPrefabs가 없습니다.");
            return;
        }
        
        GameObject explosionEffect = GameManager.Instance.typeEffectPrefabs[2];
        if (explosionEffect == null)
        {
            Debug.LogWarning("[ChessPieces] 폭발 이펙트 프리팹(typeEffectPrefabs[2])이 할당되지 않았습니다.");
            return;
        }
        
        var pos = enemyNav.transform.position;
        var inst = Instantiate(explosionEffect, pos, Quaternion.identity);
        if (GameManager.Instance.allEffects != null)
        {
            inst.transform.SetParent(GameManager.Instance.allEffects.transform);
        }
        
        // Explosion 컴포넌트에 범위와 데미지 전달
        inst.SendMessage("InitExplosion", new Vector2(range, baseDamage * damageRatio), SendMessageOptions.DontRequireReceiver);
    }

    // 3: 바람(공격 속도 적용)
    private void ApplyWind(float multiplier, EnemyNav enemyNav)
    {
        if (multiplier <= 0) return;
        // 공격 딜레이를 감소시켜 빠르게 만듦
        attackDelay = Mathf.Max(0.01f, pawnState != null ? pawnState.AttackDelay / multiplier : 0.1f);
        
        // 바람 이펙트 생성 (타입 3: Wind)
        if (enemyNav != null)
            SpawnEffect(3, enemyNav.transform);
    }

    // 4: 즉사 확률
    private void TryInstantKill(EnemyNav enemyNav, float prob, float baseDamage)
    {
        if (enemyNav == null) return;
        
        if (Random.value < prob)
        {
            // 즉사 성공 시 이펙트 생성 (타입 4: Death)
            SpawnEffect(4, enemyNav.transform);
            Destroy(enemyNav.gameObject);
        }
        else
        {
            // 즉사 실패 시 바람 이펙트 생성 (타입 3: Wind)
            SpawnEffect(3, enemyNav.transform);
            enemyNav.Damaged(Mathf.RoundToInt(baseDamage * 0.5f));
        }
    }

    // 5: 얼음
    private void ApplyIceEffect(EnemyNav enemyNav, float slowRate, float slowTime, float stopProb, float baseDamage)
    {
        if (enemyNav == null) return;
        
        // 얼음 이펙트 생성 (타입 5: Ice)
        SpawnEffect(5, enemyNav.transform);
        
        // EnemyNav 내부에서 슬로우/정지 처리를 하도록 요청
        enemyNav.ApplySlow(new Vector2(slowRate, slowTime));

        if (Random.value < stopProb)
        {
            enemyNav.StopForSeconds(1f);
        }

        // 기본 데미지도 적용
        enemyNav.Damaged(Mathf.RoundToInt(baseDamage));
    }

    // 공격 가능 여부 확인 (적에게 공격당한 후 1초간 공격 불가)
    public bool CanAttack()
    {
        return (Time.time - lastHitByEnemyTime) >= attackDisableDuration;
    }

    // 적에게 공격당했을 때 호출
    public void OnHitByEnemy()
    {
        lastHitByEnemyTime = Time.time;
    }
}
