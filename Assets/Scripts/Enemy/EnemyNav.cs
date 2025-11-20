using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EnemyNav : MonoBehaviour
{
    public EnemyState enemyState;
    private List<Vector2> wayPoints = new List<Vector2>();
    private Rigidbody2D rb;
    private int curIndex = 0;
    private Vector2 FirstDirection;
    private float speed => enemyState.Speed;
    private int currentHealth = 1;
    private int maxHealth = 1;
    
    // 이동 상태 관리
    private Vector2 currentDirection; // 현재 이동 방향
    private float speedMultiplier = 1f; // 속도 배율 (슬로우 효과용)
    private bool isStopped = false; // 정지 상태
    private Coroutine currentSlowCoroutine = null; // 현재 실행 중인 슬로우 코루틴
    
    [Header("UI")]
    public Slider healthSlider; // 할당되어 있지 않으면 런타임에 자동 생성

    [Header("능력")]
    private float abilityCooldown = 10f; // 능력 쿨타임
    private float lastAbilityTime = -100f; // 마지막 능력 사용 시간

    void Awake()
    {
        enemyState = Resources.Load<EnemyState>("State/Enemy/" + gameObject.name.Replace("(Clone)", ""));
        if (enemyState != null)
        {
            maxHealth = Mathf.Max(1, enemyState.Health);
            currentHealth = maxHealth;
        }
    }
    void Start()
    {
        for (int i = 0; i < GameManager.Instance.wayPoints.Count; i++)
            wayPoints.Add(GameManager.Instance.wayPoints[i].position);
        rb = GetComponent<Rigidbody2D>();
        if (wayPoints[0].y < transform.position.y)
            FirstDirection = Vector2.down;
        else
            FirstDirection = Vector2.up;
        
        currentDirection = FirstDirection;
        rb.velocity = currentDirection * speed;

        UpdateHealthSlider();

        // 등급별 능력 시작 (등급 2 이상)
        if (enemyState != null && enemyState.enemyGrade >= 2)
        {
            StartCoroutine(AbilityCoroutine());
        }
    }

    void Update()
    {
        // 방향 전환 체크
        if (curIndex < 2 && Vector2.Distance(rb.position, wayPoints[curIndex]) < 0.2f)
        {
            currentDirection = curIndex == 0 ? Vector2.right : FirstDirection * -1;
            curIndex++;
        }
        
        // velocity 업데이트 (정지 상태가 아닐 때만)
        if (!isStopped && rb != null)
        {
            rb.velocity = currentDirection * speed * speedMultiplier;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Goal") && curIndex == 2)
        {
            Debug.Log("도착");
            
            // 체력 감소
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyReachedGoal();
            }
            
            Destroy(gameObject, 0.2f);
        }
    }
    
    public void Damaged(int damage)
    {
        if ((currentHealth - damage) <= 0)
        {
            Debug.Log("적 제거");
            
            // 골드 보상
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Gold += 10;
                GameManager.Instance.UpdateGoldUI(); // UI 업데이트
                Debug.Log($"[EnemyNav] 골드 +10, 현재 골드: {GameManager.Instance.Gold}");
            }
            
            Destroy(gameObject);
        }
        else
        {
            currentHealth -= damage;
            UpdateHealthSlider();
        }
    }

    private void UpdateHealthSlider()
    {
        if (healthSlider == null) return;
        if (maxHealth <= 0) maxHealth = 1;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = Mathf.Clamp01((float)currentHealth / (float)maxHealth);
    }

    // 외부에서 호출하기 위한 보조 메서드
    public void TakeDamage(int damage)
    {
        Damaged(damage);
    }

    // ApplySlow(Vector2(slowRate, slowTime)) 형태로 호출 가능
    public void ApplySlow(Vector2 slowParams)
    {
        float slowRate = slowParams.x;
        float slowTime = slowParams.y;
        
        // 이미 슬로우가 적용 중이면 기존 것을 중단하고 새로운 슬로우 적용
        if (currentSlowCoroutine != null)
        {
            StopCoroutine(currentSlowCoroutine);
        }
        
        currentSlowCoroutine = StartCoroutine(ApplySlowCoroutine(slowRate, slowTime));
    }

    private IEnumerator ApplySlowCoroutine(float slowRate, float slowTime)
    {
        if (rb == null) yield break;
        
        speedMultiplier = 1f - Mathf.Clamp01(slowRate);
        
        yield return new WaitForSeconds(slowTime);
        
        if (rb != null)
        {
            speedMultiplier = 1f; // 원래 속도로 복원
            currentSlowCoroutine = null; // 코루틴 참조 초기화
        }
    }

    // StopForSeconds(float seconds) 호출 가능
    public void StopForSeconds(float seconds)
    {
        StartCoroutine(StopForSecondsCoroutine(seconds));
    }

    private IEnumerator StopForSecondsCoroutine(float seconds)
    {
        if (rb == null) yield break;
        
        isStopped = true;
        rb.velocity = Vector2.zero;
        
        yield return new WaitForSeconds(seconds);
        
        if (rb != null)
        {
            isStopped = false;
            // Update()에서 velocity가 자동으로 복원됨
        }
    }

    // 등급별 능력 코루틴
    private IEnumerator AbilityCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(abilityCooldown);

            if (enemyState == null) yield break;

            // TileSpawner에서 랜덤 기물 선택
            var spawner = FindObjectOfType<TileSpawner>();
            if (spawner == null) continue;

            var allPieces = spawner.GetAllSpawnedPieces();
            if (allPieces.Count == 0) continue;

            // 랜덤 기물 선택
            var randomPiece = allPieces[Random.Range(0, allPieces.Count)];
            if (randomPiece == null) continue;

            // 등급에 따라 패턴 적용
            ApplyAbilityPattern(randomPiece, enemyState.enemyGrade);
        }
    }

    // 등급별 패턴 적용
    private void ApplyAbilityPattern(GameObject centerPiece, int grade)
    {
        var spawner = FindObjectOfType<TileSpawner>();
        if (spawner == null) return;

        Vector3Int centerCell = spawner.WorldToCell(centerPiece.transform.position);
        List<GameObject> targetPieces = new List<GameObject>();

        switch (grade)
        {
            case 2: // 대각선 (X)
                targetPieces.AddRange(GetDiagonalPieces(spawner, centerCell));
                break;
            case 3: // 직선 (+)
                targetPieces.AddRange(GetCrossPieces(spawner, centerCell));
                break;
            case 4: // 대각선 + 직선
                targetPieces.AddRange(GetDiagonalPieces(spawner, centerCell));
                targetPieces.AddRange(GetCrossPieces(spawner, centerCell));
                break;
            case 5: // 모든 기물
                targetPieces.AddRange(spawner.GetAllSpawnedPieces());
                break;
        }

        // 중복 제거 및 투명도 감소 적용
        HashSet<GameObject> uniquePieces = new HashSet<GameObject>(targetPieces);
        foreach (var piece in uniquePieces)
        {
            if (piece != null)
            {
                StartCoroutine(ReduceAlphaTemporarily(piece, 0.5f, 1f));
            }
        }
    }

    // 대각선 (X) 패턴 기물 가져오기
    private List<GameObject> GetDiagonalPieces(TileSpawner spawner, Vector3Int centerCell)
    {
        List<GameObject> pieces = new List<GameObject>();
        Vector3Int[] diagonals = new Vector3Int[]
        {
            new Vector3Int(1, 1, 0),   // 우상
            new Vector3Int(1, -1, 0),  // 우하
            new Vector3Int(-1, 1, 0),  // 좌상
            new Vector3Int(-1, -1, 0)  // 좌하
        };

        foreach (var dir in diagonals)
        {
            for (int i = 1; i < 10; i++) // 최대 10칸까지
            {
                Vector3Int targetCell = centerCell + dir * i;
                var piece = spawner.GetPieceAt(targetCell);
                if (piece != null)
                    pieces.Add(piece);
            }
        }

        return pieces;
    }

    // 직선 (+) 패턴 기물 가져오기
    private List<GameObject> GetCrossPieces(TileSpawner spawner, Vector3Int centerCell)
    {
        List<GameObject> pieces = new List<GameObject>();
        Vector3Int[] crosses = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),   // 우
            new Vector3Int(-1, 0, 0),  // 좌
            new Vector3Int(0, 1, 0),   // 상
            new Vector3Int(0, -1, 0)   // 하
        };

        foreach (var dir in crosses)
        {
            for (int i = 1; i < 10; i++) // 최대 10칸까지
            {
                Vector3Int targetCell = centerCell + dir * i;
                var piece = spawner.GetPieceAt(targetCell);
                if (piece != null)
                    pieces.Add(piece);
            }
        }

        return pieces;
    }

    // 투명도 일시적 감소
    private IEnumerator ReduceAlphaTemporarily(GameObject piece, float targetAlpha, float duration)
    {
        var sr = piece.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        Color originalColor = sr.color;
        Color reducedColor = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
        sr.color = reducedColor;

        yield return new WaitForSeconds(duration);

        if (sr != null)
            sr.color = originalColor;
    }
}
