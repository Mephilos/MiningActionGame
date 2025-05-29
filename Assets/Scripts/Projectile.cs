using UnityEngine;

public class Projectile : MonoBehaviour
{
    //TODO: 충돌시 파괴 효과 추가 요망
    // 수명 설정용
    public float lifeTime = 5f;
    private float _damageAmount;
    public bool isEnemyProjectile = false;

    void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }
    public void SetDamage(float damage)
    {
        _damageAmount = damage;
    }
    // 물리 충돌이 발생했을 때 호출되는 함수
    void OnTriggerEnter(Collider other)
    {
        if (isEnemyProjectile) // 이 투사체가 적의 것이라면
        {
            if (other.CompareTag("Player")) // 플레이어 태그를 가진 오브젝트와 충돌 시
            {
                PlayerData playerData = other.gameObject.GetComponent<PlayerData>();
                if (playerData != null)
                {
                    playerData.TakeDamage(_damageAmount);
                    Debug.Log($"플레이어가 적 투사체에 맞음! 데미지: {_damageAmount}");
                }
                Destroy(gameObject); // 충돌 후 파괴
            }
        }
        else // 플레이어
        {
            BasicEnemy enemy = other.gameObject.GetComponent<BasicEnemy>(); // 또는 모든 적 유형을 처리할 수 있는 부모 클래스/인터페이스
            RangedEnemy rangedEnemy = other.gameObject.GetComponent<RangedEnemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(_damageAmount);
                Destroy(gameObject);
            }
            else if (rangedEnemy != null)
            {
                rangedEnemy.TakeDamage(_damageAmount);
                Destroy(gameObject);
            }
            // TODO: 다른 오브젝트 판정 추가 가능
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) 
        {
            Destroy(gameObject);
        }
    }
}
