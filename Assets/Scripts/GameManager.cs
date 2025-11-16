using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Transform> wayPoints; // 웨이 포인트 리스트
    public GameObject allEnemy; // 모든 적들을 담을 부모 오브젝트
    public CPType _CPType;
    public int[] CPTypeLevel = { 1, 1, 1, 1, 1, 1 };
    // 단방향: GameManager는 이벤트(감지 신호)만 발행하고 구독자들은 스스로 상태를 확인합니다.
    public event Action OnCPTypeUpgraded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
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
        CPTypeLevel[type]++;
        int newLevel = CPTypeLevel[type];
        // 이벤트로 감지 신호만 통지 (구독자가 스스로 GameManager에서 값 조회)
        OnCPTypeUpgraded?.Invoke();
    }
}
