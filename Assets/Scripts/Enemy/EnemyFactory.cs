using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    public List<GameObject> enemyPrefabs;
    public List<Transform> spawnPoints;
    private int EnemyIndex = 0;

    private void Start()
    {
        StartCoroutine(SpawnEnemiesCoroutine());
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        while (true)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            var enemy = Instantiate(enemyPrefabs[EnemyIndex], spawnPoints[spawnIndex].position, Quaternion.identity);
            enemy.transform.SetParent(GameManager.Instance.AllEnemy.transform);
            EnemyIndex = (EnemyIndex + 1) % enemyPrefabs.Count;
            yield return new WaitForSeconds(1f);
        }
    }
}
