using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 시스템 관리
/// 웨이브마다 WaveCost에 따라 몬스터 등급 비율을 결정하고 스폰
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("웨이브 설정")]
    public int currentWave = 1;
    public float waveCostMultiplier = 3f; // 웨이브당 기본 비용 (웨이브 1 = 5, 웨이브 2 = 10...)
    public float increasePerWave = 1f; // 웨이브당 비용 증가량
    public float spawnInterval = 0.5f; // 몬스터 스폰 간격
    public float waveDelay = 10f; // 일반 웨이브 사이 대기 시간
    public float bossWaveDelay = 30f; // 보스 웨이브 대기 시간 (20라운드 단위)

    [Header("몬스터 프리팹 (등급순)")]
    public GameObject[] enemyPrefabs; // [0]:Pawn, [1]:Knight, [2]:Bishop, [3]:Rook, [4]:Queen, [5]:King
    
    [Header("스폰 포인트")]
    public List<Transform> spawnPoints;

    private bool isWaveActive = false;
    private int totalEnemiesInWave = 0;
    private int spawnedEnemiesCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 초기 웨이브 UI 업데이트
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateWaveUI(currentWave);
        }
        
        StartCoroutine(WaveRoutine());
    }

    // 웨이브 루틴 - 웨이브 시작 → 스폰 → 모든 적 처치 또는 대기 시간 경과 → 다음 웨이브
    private IEnumerator WaveRoutine()
    {
        while (true)
        {
            Debug.Log($"[WaveManager] ===== 웨이브 {currentWave} 시작 =====");
            
            // 웨이브 UI 업데이트
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateWaveUI(currentWave);
            }
            
            yield return StartCoroutine(SpawnWave());
            
            // 보스 웨이브 여부 확인 (20라운드 단위)
            bool isBossWave = (currentWave % 20 == 0);
            
            // 모든 적이 처치되거나 대기 시간이 지나면 다음 웨이브 시작
            yield return StartCoroutine(WaitForWaveEndCondition(isBossWave));
            
            Debug.Log($"[WaveManager] 웨이브 {currentWave} 완료!");
            GameManager.Instance.Gold += currentWave * 20; // 웨이브 완료 보상
            GameManager.Instance.UpdateGoldUI();
            currentWave++;
        }
    }

    // 현재 웨이브의 몬스터를 스폰
    private IEnumerator SpawnWave()
    {
        isWaveActive = true;
        spawnedEnemiesCount = 0;

        // 웨이브 비용 계산
        float waveCost = waveCostMultiplier + currentWave * increasePerWave;
        
        // 몬스터 구성 계산
        Dictionary<int, int> enemyComposition = CalculateEnemyComposition(currentWave, waveCost);
        
        // 총 스폰할 몬스터 수 계산
        totalEnemiesInWave = 0;
        foreach (var count in enemyComposition.Values)
        {
            totalEnemiesInWave += count;
        }

        Debug.Log($"[WaveManager] 웨이브 {currentWave} - 비용: {waveCost}, 총 몬스터: {totalEnemiesInWave}");

        // 몬스터 스폰 리스트 생성
        List<int> spawnList = new List<int>();
        foreach (var pair in enemyComposition)
        {
            int grade = pair.Key;
            int count = pair.Value;
            for (int i = 0; i < count; i++)
            {
                spawnList.Add(grade);
            }
        }

        // 리스트 섞기 (랜덤 스폰 순서)
        ShuffleList(spawnList);

        // 몬스터 스폰
        foreach (int grade in spawnList)
        {
            SpawnEnemy(grade);
            spawnedEnemiesCount++;
            yield return new WaitForSeconds(spawnInterval);
        }

        isWaveActive = false;
    }

    // 웨이브 종료 조건 대기: 모든 적 처치 또는 대기 시간 경과 중 먼저 만족되는 조건
    private IEnumerator WaitForWaveEndCondition(bool isBossWave)
    {
        float maxDelay = isBossWave ? bossWaveDelay : waveDelay;
        float elapsedTime = 0f;
        
        if (isBossWave)
        {
            Debug.Log($"[WaveManager] 보스 웨이브 대기 중... (30초 타이머 시작)");
            // 보스 타이머 UI 활성화
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetBossTimerActive(true);
            }
        }
        else
        {
            Debug.Log($"[WaveManager] 웨이브 종료 대기 중... (최대 {waveDelay}초 또는 모든 적 처치)");
        }
        
        while (elapsedTime < maxDelay)
        {
            // 모든 적이 처치되었는지 확인 (보스/일반 웨이브 모두 조기 종료 가능)
            if (GameManager.Instance != null && GameManager.Instance.allEnemy != null)
            {
                int aliveEnemyCount = GameManager.Instance.allEnemy.transform.childCount;
                
                if (aliveEnemyCount == 0)
                {
                    // 보스 웨이브였다면 타이머 UI 비활성화
                    if (isBossWave && GameManager.Instance != null)
                    {
                        GameManager.Instance.SetBossTimerActive(false);
                    }
                    
                    Debug.Log($"[WaveManager] 모든 적 처치 완료! ({elapsedTime:F1}초 경과)");
                    yield break; // 즉시 다음 웨이브로
                }
            }
            
            // 보스 웨이브: 남은 시간 UI 업데이트
            if (isBossWave)
            {
                float remainingTime = maxDelay - elapsedTime;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdateBossTimer(remainingTime);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }
        
        // 보스 웨이브 종료 시 타이머 UI 비활성화
        if (isBossWave && GameManager.Instance != null)
        {
            GameManager.Instance.SetBossTimerActive(false);
            Debug.Log($"[WaveManager] 보스 웨이브 타임아웃! (30초 경과)");
        }
        else
        {
            Debug.Log($"[WaveManager] 대기 시간 {waveDelay}초 경과");
        }
    }

    // 모든 적이 처치될 때까지 대기
    private IEnumerator WaitForAllEnemiesDefeated()
    {
        Debug.Log("[WaveManager] 모든 적 처치 대기 중...");
        
        // allEnemy 오브젝트의 자식(적) 수를 확인
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.allEnemy != null)
            {
                int aliveEnemyCount = GameManager.Instance.allEnemy.transform.childCount;
                
                if (aliveEnemyCount == 0)
                {
                    Debug.Log("[WaveManager] 모든 적 처치 완료!");
                    break;
                }
                
                // 0.5초마다 체크
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogWarning("[WaveManager] GameManager 또는 allEnemy가 null입니다.");
                break;
            }
        }
    }

    // 웨이브에 따른 몬스터 구성 계산
    private Dictionary<int, int> CalculateEnemyComposition(int wave, float waveCost)
    {
        Dictionary<int, int> composition = new Dictionary<int, int>();

        // 보스 웨이브 (20, 40, 60...)
        if (wave % 20 == 0)
        {
            // Queen 또는 King 중 하나 스폰
            int bossGrade = (wave / 20) % 2 == 1 ? 4 : 5; // 홀수번째 보스 웨이브는 Queen(4), 짝수는 King(5)
            composition[bossGrade] = 1;
            Debug.Log($"[WaveManager] 보스 웨이브! 등급 {bossGrade} 스폰");
            return composition;
        }

        // 일반 웨이브 - 비율 계산
        Dictionary<int, float> ratios = GetWaveRatios(wave);
        
        // 각 몬스터 등급의 비용 (등급이 높을수록 비용 높음)
        float[] enemyCosts = { 1f, 2f, 4f, 4f, 10f, 15f }; // Pawn, Knight, Bishop, Rook, Queen, King

        // 비율에 따라 각 등급별 스폰 수 계산
        foreach (var pair in ratios)
        {
            int grade = pair.Key;
            float ratio = pair.Value;
            
            // Queen(4)과 King(5)은 일반 웨이브에서 스폰 안 됨 (보스 웨이브만)
            if (grade >= 4)
            {
                continue;
            }
            
            if (ratio > 0 && grade < enemyCosts.Length && grade < enemyPrefabs.Length)
            {
                // 해당 등급에 할당된 비용
                float allocatedCost = waveCost * ratio;
                
                // 비용으로 스폰 가능한 몬스터 수
                int count = Mathf.Max(1, Mathf.FloorToInt(allocatedCost / enemyCosts[grade]));
                
                composition[grade] = count;
                Debug.Log($"[WaveManager] 등급 {grade} - 비율: {ratio:P0}, 비용: {allocatedCost}, 수량: {count}");
            }
        }

        return composition;
    }

    // 웨이브에 따른 몬스터 등급 비율 반환
    private Dictionary<int, float> GetWaveRatios(int wave)
    {
        Dictionary<int, float> ratios = new Dictionary<int, float>();

        // 웨이브를 20으로 나눈 사이클 내 위치 계산
        int cycleWave = ((wave - 1) % 20) + 1;

        if (cycleWave >= 1 && cycleWave <= 5)
        {
            // W1~5: 폰 100%
            ratios[0] = 1.0f; // Pawn
        }
        else if (cycleWave >= 6 && cycleWave <= 10)
        {
            // W6~10: 폰 80% / 나이트 20%
            ratios[0] = 0.8f; // Pawn
            ratios[1] = 0.2f; // Knight
        }
        else if (cycleWave >= 11 && cycleWave <= 15)
        {
            // W11~15: 폰 70% / 나이트 15% / 비숍·룩 15%
            ratios[0] = 0.7f; // Pawn
            ratios[1] = 0.15f; // Knight
            ratios[2] = 0.075f; // Bishop
            ratios[3] = 0.075f; // Rook
        }
        else if (cycleWave >= 16 && cycleWave <= 19)
        {
            // W16~19 (그리고 21웨이브 이상): 폰 60% / 나이트 20% / 비숍·룩 20%
            ratios[0] = 0.6f; // Pawn
            ratios[1] = 0.2f; // Knight
            ratios[2] = 0.1f; // Bishop
            ratios[3] = 0.1f; // Rook
        }

        return ratios;
    }

    // 특정 등급의 몬스터 스폰
    private void SpawnEnemy(int grade)
    {
        if (grade < 0 || grade >= enemyPrefabs.Length || enemyPrefabs[grade] == null)
        {
            Debug.LogWarning($"[WaveManager] 등급 {grade} 프리팹이 없습니다!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[WaveManager] 스폰 포인트가 없습니다!");
            return;
        }

        // 랜덤 스폰 포인트 선택
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        GameObject enemy = Instantiate(enemyPrefabs[grade], spawnPoints[spawnIndex].position, Quaternion.identity);
        
        if (GameManager.Instance != null && GameManager.Instance.allEnemy != null)
        {
            enemy.transform.SetParent(GameManager.Instance.allEnemy.transform);
        }

        // 웨이브별 체력 적용 (EnemyState의 Health × 웨이브)
        EnemyNav enemyNav = enemy.GetComponent<EnemyNav>();
        if (enemyNav != null)
        {
            enemyNav.SetWaveMultiplier(currentWave);
            Debug.Log($"[WaveManager] 등급 {grade} 몬스터 스폰 - 웨이브 배율: x{currentWave} ({spawnedEnemiesCount + 1}/{totalEnemiesInWave})");
        }
        else
        {
            Debug.Log($"[WaveManager] 등급 {grade} 몬스터 스폰 ({spawnedEnemiesCount + 1}/{totalEnemiesInWave})");
        }
    }

    // 리스트 섞기 (Fisher-Yates 알고리즘)
    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // 현재 웨이브 정보 반환
    public int GetCurrentWave() => currentWave;

    // 웨이브 활성 상태 반환
    public bool IsWaveActive() => isWaveActive;
}
