using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Transform> WayPoints; // 웨이 포인트 리스트
    public GameObject AllEnemy; // 모든 적들을 담을 부모 오브젝트
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
        foreach (Transform enemy in AllEnemy.transform)
        {
            Destroy(enemy.gameObject);
        }
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
