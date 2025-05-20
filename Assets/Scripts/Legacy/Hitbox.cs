using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public PlayerData playerData;

    void Start()
    {
        if (playerData == null)
        {
            playerData = GetComponentInParent<PlayerData>();
        }
        if (playerData== null)
        {
            Debug.LogError("Hitbox가 PlayerStats를 찾을 수 없습니다!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (playerData == null) return;

        if (other.CompareTag("EnemyProjectile"))
        {
            Projectile enemyProjectile = other.GetComponent<Projectile>();
            if (enemyProjectile != null)
            {

            }
        }
        else if (other.CompareTag("Enemy"))
        {
            BasicEnemy enemy = other.GetComponent<BasicEnemy>();
            if (enemy != null && enemy.enemyBaseData != null)
            {
                
            }
        }
    }
}
