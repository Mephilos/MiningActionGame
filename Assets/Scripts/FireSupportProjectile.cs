using UnityEngine;

public class FireSupportProjectile : MonoBehaviour
{
    [Header("능력치")]
    public float damage = 50f;
    public float explosionRadius = 2f;
    public int terrainDestructionRadius = 1;

    [Header("효과")]
    public GameObject impactEffectPrefab;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _elapsedTime;

    // StageManager에서 이 함수를 호출
    public void Initialize(Vector3 start, Vector3 target, float duration)
    {
        _startPosition = start;
        _targetPosition = target;
        _flightDuration = duration;
        transform.position = _startPosition;
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / _flightDuration);

        transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);

        if (progress >= 1.0f)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.DestroyBlocksInArea(transform.position, terrainDestructionRadius, 2);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<EnemyBase>(out var enemy))
            {
                enemy.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
