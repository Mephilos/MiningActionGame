using UnityEngine.AI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float minSpawnDistance = 10f;
    public float maxSpawnDistance = 25f;
    public float spawnInterval = 5f;
    public int maxEnemiesToSpawnAtOnce = 1;
    public int maxTotalEnemies = 10;


    public int spawnPositionAttempts = 10;
    public float navMeshSampleRadius = 2.0f;

    private Transform playerTransform;
    private float timer;
    
    public static List<BasicEnemy> activeEnemies = new List<BasicEnemy>();
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if(playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("플레이어 케릭터를 찾을 수 없습니다");
            enabled = false;
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyPrefab이 할당 되어 있지 않습니다.");
            enabled = false;
            return;
        }
        activeEnemies.Clear();
        timer = spawnInterval;
    }

    void Update()
    {
        if(playerTransform == null || enemyPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            if (CanSpawnMoreEnemies())
            {
                AttemptSpawn();
            }
        }
    }
    /// <summary>
    /// 적을 더 스폰 할수 있는지 체크하는 함수
    /// </summary>
    /// <returns>(bool)스폰된 케릭터 수가 max를 넘어서는지 않서는지</returns>
    bool CanSpawnMoreEnemies()
    {
        if(maxTotalEnemies <= 0) return true;
        
        return activeEnemies.Count < maxTotalEnemies;
    }

    void AttemptSpawn()
    {
        for(int i = 0; i < maxEnemiesToSpawnAtOnce; i++)
        {
            if (!CanSpawnMoreEnemies()) break;
            Vector3 spawnPosition = Vector3.zero;
            bool positionFound = false;

            for(int attempt = 0; attempt < spawnPositionAttempts; attempt++)
            {
                //플레이어 주변 랜덤 방향 백터 생성
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y); // 높이는 NavMesh로 체크

                Vector3 potentialSpawnPoint = playerTransform.position + randomDirection;

                NavMeshHit hit;
                if(NavMesh.SamplePosition(potentialSpawnPoint, out hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPosition = hit.position;
                    positionFound = true;
                    break;
                }
            }
            if(positionFound)
            {   
                //Debug.Log($"스폰 위치: {spawnPosition}");
                GameObject spawnedEnemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                //Debug.LogWarning("유효 스폰위치 못 찾음");
            }
        }
    }

    /// <summary>
    /// 적 스폰위치 디버그
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if(playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
        }
    }
}
