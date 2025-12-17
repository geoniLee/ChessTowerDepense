using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Transform> wayPoints; // 웨이 포인트 리스트
    public GameObject allEnemy; // 모든 적들을 담을 부모 오브젝트
    public CPType _CPType;
    public int[] CPTypeLevel = { 1, 1, 1, 1, 1, 1 };
    
    // 타입별 공격 이펙트 프리팹 (모든 기물이 공유)
    // [0]: Electric, [1]: Poison, [2]: Explosion, [3]: Wind, [4]: Death, [5]: Ice
    public GameObject[] typeEffectPrefabs = new GameObject[6];

    [Header("골드 시스템")]
    public int Gold = 100; // 시작 골드
    public int spawnCost = 10; // 현재 소환 비용 (10부터 시작, 소환마다 10씩 증가)
    
    [Header("체력 시스템")]
    public int Health = 3; // 체력
    public TextMeshProUGUI healthText; // 체력 표시
    
    [Header("웨이브 UI")]
    public TextMeshProUGUI waveText; // 웨이브 표시
    
    [Header("보스 타이머 UI")]
    public TextMeshProUGUI bossTimerText; // 보스 타이머 표시 (빨간색)
    
    private bool isGameOver = false; // 게임 오버 상태
    
    [Header("골드 UI")]
    public TextMeshProUGUI goldText; // 소지 골드 표시
    public TextMeshProUGUI spawnCostText; // 소환 비용 표시
    
    [Header("속성 업그레이드 버튼")]
    public GameObject upgradeButtonsParent; // 6개 업그레이드 버튼의 부모 오브젝트
    private UnityEngine.UI.Button[] upgradeButtons; // 자동 할당될 버튼들
    
    [Header("게임 오버 UI")]
    public GameObject gameOverUI; // 게임 오버 시 표시할 UI
    
    // 킹 버프 시스템
    private int allyKingCount = 0; // 아군 킹 개수
    
    // 단방향: GameManager는 이벤트(감지 신호)만 발행하고 구독자들은 스스로 상태를 확인합니다.
    public event Action OnCPTypeUpgraded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 부모 오브젝트에서 버튼 자동 할당
        if (upgradeButtonsParent != null)
        {
            upgradeButtons = upgradeButtonsParent.GetComponentsInChildren<UnityEngine.UI.Button>();
            if (upgradeButtons.Length != 6)
            {
                Debug.LogWarning($"[GameManager] 업그레이드 버튼이 6개가 아닙니다. 현재: {upgradeButtons.Length}개");
            }
        }
        
        UpdateGoldUI();
        UpdateUpgradeButtonTexts();
        UpdateHealthUI();
    }

    private void Update()
    {
        // 게임 오버 상태에서 터치/클릭 감지
        if (isGameOver)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                RestartGame();
            }
        }
    }

    public void ClearAllEnemies()
    {
        foreach (Transform enemy in allEnemy.transform)
        {
            Destroy(enemy.gameObject);
        }
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

    public void UpgradeCPType(int type)
    {
        if (CPTypeLevel == null || type < 0 || type >= CPTypeLevel.Length) return;
        
        // 업그레이드 비용 계산 (현재 레벨 × 100)
        int upgradeCost = CPTypeLevel[type] * 100;
        
        if (Gold >= upgradeCost)
        {
            Gold -= upgradeCost;
            CPTypeLevel[type]++;
            int newLevel = CPTypeLevel[type];
            
            Debug.Log($"[GameManager] 속성 {type} 업그레이드: Lv.{newLevel}, 비용: {upgradeCost}, 남은 골드: {Gold}");
            
            UpdateGoldUI(); // UI 업데이트
            UpdateUpgradeButtonTexts(); // 버튼 텍스트 업데이트
            
            // 이벤트로 감지 신호만 통지 (구독자가 스스로 GameManager에서 값 조회)
            OnCPTypeUpgraded?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[GameManager] 속성 업그레이드 골드 부족! 필요: {upgradeCost}, 보유: {Gold}");
        }
    }

    // 기물 소환 시 골드 소비 (성공 시 true 반환, 비용 10씩 증가)
    public bool TrySpendGoldForSpawn()
    {
        if (Gold >= spawnCost)
        {
            Gold -= spawnCost;
            Debug.Log($"[GameManager] 골드 소비: {spawnCost}, 남은 골드: {Gold}");
            spawnCost += 10; // 다음 소환 비용 증가
            Debug.Log($"[GameManager] 다음 소환 비용: {spawnCost}");
            UpdateGoldUI(); // UI 업데이트
            return true;
        }
        else
        {
            Debug.LogWarning($"[GameManager] 골드 부족! 필요: {spawnCost}, 보유: {Gold}");
            return false;
        }
    }

    // 골드 UI 업데이트
    public void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {Gold}";
        
        if (spawnCostText != null)
            spawnCostText.text = $"Cost: {spawnCost}";
    }

    // 체력 UI 업데이트
    public void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"Health: {Health}";
    }

    // 웨이브 UI 업데이트
    public void UpdateWaveUI(int wave)
    {
        if (waveText != null)
            waveText.text = $"Wave: {wave}";
    }

    // 보스 타이머 UI 업데이트 (소수점 1자리)
    public void UpdateBossTimer(float remainingTime)
    {
        if (bossTimerText != null)
        {
            bossTimerText.text = $"{remainingTime:F1}";
        }
    }

    // 보스 타이머 UI 활성화/비활성화
    public void SetBossTimerActive(bool active)
    {
        if (bossTimerText != null)
        {
            bossTimerText.gameObject.SetActive(active);
        }
    }

    // 속성 업그레이드 버튼 텍스트 업데이트
    public void UpdateUpgradeButtonTexts()
    {
        if (upgradeButtons == null) return;
        
        for (int i = 0; i < upgradeButtons.Length && i < CPTypeLevel.Length; i++)
        {
            if (upgradeButtons[i] != null)
            {
                // 버튼 하위의 모든 TextMeshProUGUI 컴포넌트 가져오기
                var textComponents = upgradeButtons[i].GetComponentsInChildren<TextMeshProUGUI>();
                
                // 두 번째 TMP가 있으면 비용 업데이트
                if (textComponents.Length >= 2)
                {
                    int cost = CPTypeLevel[i] * 100;
                    textComponents[1].text = $"Cost: {cost}";
                }
            }
        }
    }

    // 적이 Goal에 도착했을 때 호출 - 체력 감소
    public void OnEnemyReachedGoal()
    {
        Health--;
        UpdateHealthUI(); // 체력 UI 업데이트
        Debug.Log($"[GameManager] 적이 Goal 도착! 남은 체력: {Health}");
        
        if (Health <= 0)
        {
            GameOver();
        }
    }

    // 게임 오버 처리 - 게임 멈춤
    private void GameOver()
    {
        Debug.Log("[GameManager] 게임 오버! 화면을 터치하여 재시작하세요.");
        isGameOver = true;
        Time.timeScale = 0f; // 게임 멈춤
        
        // 게임 오버 UI 활성화
        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    // 게임 재시작 - 현재 씬 다시 로드
    private void RestartGame()
    {
        Debug.Log("[GameManager] 게임 재시작");
        Time.timeScale = 1f; // 게임 속도 복원
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 아군 킹 개수 반환
    public int GetAllyKingCount()
    {
        return allyKingCount;
    }

    // 킹 소환 시 호출 (카운트 증가)
    public void OnKingSpawned()
    {
        allyKingCount++;
        Debug.Log($"[GameManager] 킹 소환! 현재 킹 개수: {allyKingCount}");
    }
}
