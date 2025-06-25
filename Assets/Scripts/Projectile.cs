using UnityEngine;
using System.Collections;
public class Projectile : MonoBehaviour
{
    public GameObject impactEffectPrefab;
    public float lifeTime = 5f;
    private float _damageAmount;
    public bool isEnemyProjectile = false;

    private float _explosionRadius;
    private GameObject _explosionEffectPrefab;
    private LayerMask _explosionDamageLayerMask;
    private Coroutine _lifeTimeCoroutine;
    void OnEnable()
    {
        _lifeTimeCoroutine = StartCoroutine(ReturnToObjPoolDelay(lifeTime));
    }

    void OnDisable()
    {
        if (_lifeTimeCoroutine != null)
        {
            StopCoroutine(_lifeTimeCoroutine);
            _lifeTimeCoroutine = null;
        }
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator ReturnToObjPoolDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
    public void SetDamage(float damage)
    {
        _damageAmount = damage;
    }
    //WeaponController 에서 호출 폭발 관련 데이터 설정 메서드
    public void InitializeExplosion(float radius, GameObject effectPrefab, LayerMask damageLayerMask)
    {
        _explosionRadius = radius;
        _explosionEffectPrefab = effectPrefab;
        _explosionDamageLayerMask = damageLayerMask;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!isEnemyProjectile && other.CompareTag("Player"))
        {
            return;
        }

        bool directTargetHit = false; 

        if (!isEnemyProjectile) //플레이어의 발사체인 경우
        {
            EnemyBase enemyHit = other.gameObject.GetComponent<EnemyBase>();
            Destructible destructibleObjectHit = other.gameObject.GetComponent<Destructible>();

            if (enemyHit != null || destructibleObjectHit != null)
            {
                if (_explosionRadius <= 0f) //단일 타겟 공격
                {
                    if (enemyHit != null) enemyHit.TakeDamage(_damageAmount); 
                    if (destructibleObjectHit != null) destructibleObjectHit.TakeDamage(_damageAmount);
                    
                    ShowImpactEffect(); 
                    ObjectPoolManager.Instance.ReturnToPool(gameObject);
                    return;
                }
                directTargetHit = true; //범위 공격 시 지정된 타겟에 직접 맞음
            }
        }
        else if (isEnemyProjectile)
        {
            if (other.CompareTag("Player")) 
            {
                PlayerData playerData = other.gameObject.GetComponent<PlayerData>();
                if (playerData != null)
                {
                    playerData.TakeDamage(_damageAmount);
                    Debug.Log($"플레이어가 적 투사체에 맞음! 데미지: {_damageAmount}");
                }
                ShowImpactEffect();
                ObjectPoolManager.Instance.ReturnToPool(gameObject);
                return;
            }
        }

        // 지면 또는 기타 환경 레이어와 충돌 확인
        bool hitEnvironment = (other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                               other.gameObject.layer == LayerMask.NameToLayer("Default") ||
                               other.gameObject.layer == LayerMask.NameToLayer("Obstacles"));
        
        if (_explosionRadius > 0f && (hitEnvironment || directTargetHit))
        {
            HandleExplosion(transform.position);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        //폭발형이 아니지만 환경에 부딪혔을 때
        else if (!directTargetHit && hitEnvironment) 
        {
            ShowImpactEffect();
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        
    }

    void HandleExplosion(Vector3 explosionCenter)
    {
        if (_explosionEffectPrefab != null)
        {
            ObjectPoolManager.Instance.GetFromPool(_explosionEffectPrefab.name, explosionCenter, Quaternion.identity);
        }

        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, _explosionRadius, _explosionDamageLayerMask);

        foreach (Collider hitCollider in hitColliders)
        {
            if (!isEnemyProjectile) //플레이어의 발사체인 경우
            {
                if (hitCollider.CompareTag("Player")) continue;

                EnemyBase enemy = hitCollider.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damageAmount);
                }

                Destructible destructible = hitCollider.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.TakeDamage(_damageAmount);
                }
            }
        }
    }
    void ShowImpactEffect()
    {
        if (impactEffectPrefab != null)
        {
            ObjectPoolManager.Instance.GetFromPool(impactEffectPrefab.name, transform.position, Quaternion.identity);
        }
    }
}
