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

    // 타입별 이펙트용 필드
    public GameObject explosionPrefab; // 타입2: 폭발 프리팹 (Inspector에 할당)
    private Coroutine windCoroutine;

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
            case 0: col = Color.yellow; break; // 노랑
            case 1: col = new Color(0.6f, 0f, 0.6f); break; // 보라(마젠타 계열)
            case 2: col = Color.red; break; // 빨강
            case 3: col = new Color(0.78f, 0.95f, 0.66f); break; // 초록
            case 4: col = Color.gray; break; // 회색
            case 5: col = Color.blue; break; // 파랑
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
        if (enemy == null)
        {
            enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayerMask);
            target = enemy?.GetComponent<EnemyNav>();
        }
        else if (enemy != null && target != null)
        {
            attackDelay -= Time.deltaTime;
            if (attackDelay <= 0)
            {
                // 타입별 공격 효과 적용
                ApplyAttackEffect(target);

                // 타격 후 초기화
                enemy = null;
                target = null;

                // 기본 공격 지연 초기화 (바람 타입 등은 내부에서 조정 가능)
                attackDelay = pawnState.AttackDelay;
            }
        }
    }

    // 타입별 공격 효과 진입점
    private void ApplyAttackEffect(EnemyNav enemyNav)
    {
        if (enemyNav == null) return;

        CPType cp = GameManager.Instance != null ? GameManager.Instance._CPType : null;
    float baseDamage = pawnState != null ? pawnState.Damage : 1f;
    // 타입 레벨에 따라 추가 데미지 적용 (요청: damage += (level - 1))
    int extra = Mathf.Max(0, typeLevel - 1);
    float damageWithLevel = baseDamage + extra;

        // 공격 타입 로그 출력
        string[] typeNames = new string[] { "Electric", "Poison", "Explosion", "Wind", "Death", "Ice" };
        string tname = (type >= 0 && type < typeNames.Length) ? typeNames[type] : "Unknown";
        
        switch (type)
        {
            case 0: // 전기: 체인 전이
                StartCoroutine(ElectricChain(enemyNav, cp.eletricTargetCount, damageWithLevel));
                break;
            case 1: // 독: 주기적 데미지
                StartCoroutine(DoPoison(enemyNav, cp.poisonDamageRatio, cp.poisonDamageSpeed, damageWithLevel));
                break;
            case 2: // 폭발: 프리팹 생성(Trigger 내부에서 처리)
                DoExplosionPrefab(enemyNav, cp.explosionRange, cp.explosionDamageRatio, damageWithLevel);
                break;
            case 3: // 바람: 공격 속도(attackDelay) 적용
                ApplyWind(cp.windAttackSpeed);
                enemyNav.Damaged(Mathf.RoundToInt(damageWithLevel));
                break;
            case 4: // 즉사 확률
                TryInstantKill(enemyNav, cp.deathProbability, damageWithLevel);
                break;
            case 5: // 얼음: 슬로우 및 멈춤 확률
                ApplyIceEffect(enemyNav, cp.iceSlowRate, cp.iceSlowTime, cp.iceStopProbability, damageWithLevel);
                break;
            default:
                enemyNav.Damaged(Mathf.RoundToInt(damageWithLevel));
                break;
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
            cur.Damaged(Mathf.RoundToInt(damage));
            hit.Add(go);
            count++;

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

    // 1: 독 데미지 — 즉시 데미지 후 주기적 독 데미지 (로그 추가)
    private IEnumerator DoPoison(EnemyNav enemyNav, float ratio, float period, float baseDamage)
    {
        if (enemyNav == null) yield break;

        // 즉시 기본 데미지 적용
        int immediateDmg = Mathf.RoundToInt(baseDamage);
        enemyNav.Damaged(immediateDmg);

        // 짧은 지연 후 독(주기) 데미지 적용
        yield return new WaitForSeconds(0.05f);

        int ticks = 5; // 임의 설정: 필요하면 CP 설정으로 노출 가능
        for (int i = 0; i < ticks; i++)
        {
            Debug.Log($"[Poison] Tick {i + 1}/{ticks} 적용: {Mathf.Max(1, Mathf.RoundToInt(baseDamage * ratio))} 데미지");
            if (enemyNav == null) yield break; // 적이 사망했으면 중단
            int tickDmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * ratio));
            enemyNav.Damaged(tickDmg);
            yield return new WaitForSeconds(Mathf.Max(0.01f, period));
        }
    }

    // 2: 폭발 프리팹 생성
    private void DoExplosionPrefab(EnemyNav enemyNav, float range, float damageRatio, float baseDamage)
    {
        if (enemyNav == null) return;
        if (explosionPrefab == null)
        {
            // 프리팹 없으면 직접 데미지
            enemyNav.Damaged(Mathf.RoundToInt(baseDamage * damageRatio));
            return;
        }
        var pos = enemyNav.transform.position;
        var inst = Instantiate(explosionPrefab, pos, Quaternion.identity);
        inst.SendMessage("InitExplosion", new Vector2(range, baseDamage * damageRatio), SendMessageOptions.DontRequireReceiver);
    }

    // 3: 바람(공격 속도 적용)
    private void ApplyWind(float multiplier)
    {
        if (multiplier <= 0) return;
        // 공격 딜레이를 감소시켜 빠르게 만듦
        attackDelay = Mathf.Max(0.01f, pawnState != null ? pawnState.AttackDelay / multiplier : 0.1f);
    }

    // 4: 즉사 확률
    private void TryInstantKill(EnemyNav enemyNav, float prob, float baseDamage)
    {
        if (enemyNav == null) return;
        if (Random.value < prob)
        {
            Destroy(enemyNav.gameObject);
        }
        else
        {
            enemyNav.Damaged(Mathf.RoundToInt(baseDamage * 0.5f));
        }
    }

    // 5: 얼음
    private void ApplyIceEffect(EnemyNav enemyNav, float slowRate, float slowTime, float stopProb, float baseDamage)
    {
        if (enemyNav == null) return;
        // EnemyNav 내부에서 슬로우/정지 처리를 하도록 요청
        enemyNav.ApplySlow(new Vector2(slowRate, slowTime));

        if (Random.value < stopProb)
        {
            enemyNav.StopForSeconds(1f);
        }

        // 기본 데미지도 적용
        enemyNav.Damaged(Mathf.RoundToInt(baseDamage));
    }
}
