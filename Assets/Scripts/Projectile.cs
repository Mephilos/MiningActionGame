using UnityEngine;

public class Projectile : MonoBehaviour
{
    //TODO: 충돌시 파괴 효과 추가 요망
    // 수명 설정용
    public float lifeTime = 5f;
    private float damageAmount;

    void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }
    public void SetDamage(float damage)
    {
        damageAmount = damage;
    }
    // 물리 충돌이 발생했을 때 호출되는 함수
    void OnTriggerEnter(Collider other)
    {
        BasicEnemy enemy = other.gameObject.GetComponent<BasicEnemy>();
        Debug.Log("발사체 충돌 확인");
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
        }
        //TODO:다른 오브젝트 판정 추가 가능
        // 발사체 자신은 충돌 후 파괴
        Destroy(gameObject);
    }
}
