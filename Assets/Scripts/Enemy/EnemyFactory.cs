using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [구버전 - 사용 안 함]
/// WaveManager로 대체되었습니다.
/// 웨이브 시스템을 사용하려면 WaveManager 컴포넌트를 사용하세요.
/// </summary>
public class EnemyFactory : MonoBehaviour
{
    public List<GameObject> enemyPrefabs;
    public List<Transform> spawnPoints;
    private int EnemyIndex = 0;

    [Header("웨이브 시스템 사용 시 이 컴포넌트를 비활성화하세요")]
    public bool useOldSystem = false;

    private void Start()
    {
        if (useOldSystem)
        {
            StartCoroutine(SpawnEnemiesCoroutine());
        }
        else
        {
            Debug.LogWarning("[EnemyFactory] 구버전 시스템입니다. WaveManager를 사용하세요.");
        }
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        while (true)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            var enemy = Instantiate(enemyPrefabs[EnemyIndex], spawnPoints[spawnIndex].position, Quaternion.identity);
            enemy.transform.SetParent(GameManager.Instance.allEnemy.transform);
            EnemyIndex = (EnemyIndex + 1) % enemyPrefabs.Count;
            yield return new WaitForSeconds(1f);
        }
    }
}
