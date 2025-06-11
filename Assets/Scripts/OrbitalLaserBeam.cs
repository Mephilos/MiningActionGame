using System.Collections;
using UnityEngine;

public class OrbitalLaserBeam : MonoBehaviour
{
    private float _duration;
    private float _tickRate;
    private float _damagePerTick;
    private float _radius;

    public void Initialize(float duration, float tickRate, float damagePerTick, float radius)
    {
        Debug.Log($"레이저 빔 초기화! 지속시간: {duration}, 틱당 데미지: {damagePerTick}");
        _duration = duration;
        _tickRate = tickRate;
        _damagePerTick = damagePerTick;
        _radius = radius;
        
        transform.localScale = new Vector3(radius * 2, transform.localScale.y, radius * 2);
        
        Destroy(gameObject, _duration);
        StartCoroutine(DamageOverTime());
    }

    private IEnumerator DamageOverTime()
    {
        WaitForSeconds wait = new WaitForSeconds(_tickRate);

        while (true)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, _radius, LayerMask.GetMask("Enemy"));
            foreach (Collider hit in hitColliders)
            {
                if (hit.TryGetComponent<EnemyBase>(out EnemyBase enemy))
                {
                    enemy.TakeDamage(_damagePerTick);
                    Debug.Log($"{enemy.name}에게 {_damagePerTick} 데미지");
                }
            }
            yield return wait;
        }
    }
}
