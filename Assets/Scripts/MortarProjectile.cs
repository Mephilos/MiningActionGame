using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    public float damage = 10f;
    public float explosionRadius = 1.5f;
    public int terrainDestructionDepth = 1;
    public GameObject impactEffectPrefab;
    public float projectileColliderRadius = 0.5f;
    
    private Vector3 _targetPosition;
    private float _flightDuration = 2.0f;
    private float _elapsedTime;
    private Vector3 _startPosition;
    private float _maxHeightOffset = 5;

    private bool _targetReached = false;

    public void Initialize(Vector3 targetPosition, float flightDuration)
    {
        _targetPosition = targetPosition;
        _flightDuration = flightDuration > 0 ? flightDuration : 2.0f;
        _startPosition = transform.position;
        _elapsedTime = 0;

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (GetComponent<SphereCollider>() == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = projectileColliderRadius;
        }
    }

    void Update()
    {
        if (_targetReached) return;
        _elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / _flightDuration);
        
        // 포물선 궤도 계산
        Vector3 currentPosition = Vector3.Lerp(_startPosition, _targetPosition, progress);
        float arc = Mathf.Sin(progress * Mathf.PI) * _maxHeightOffset;
        currentPosition.y += arc;
        transform.position = currentPosition;

        // 투사체 회전 계산
        if (_elapsedTime > 0.01f)
        {
            Vector3 nextPosition = Vector3.Lerp(_startPosition, _targetPosition,
                Mathf.Clamp01((_elapsedTime + Time.deltaTime) / _flightDuration));
            nextPosition.y += Mathf.Sin(Mathf.Clamp01((_elapsedTime + Time.deltaTime) / _flightDuration) * Mathf.PI) * _maxHeightOffset;
            if ((nextPosition - currentPosition).sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(nextPosition - currentPosition);
            }
        }

        if (progress >= 1.0f)
        {
            OnTargetReached();
        }
    }
    
    void OnTargetReached()
    {
        _targetReached = true;
        
        // 폭발 이펙트
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, _targetPosition, Quaternion.identity);
        }

        // 지형 파괴
        if (StageManager.Instance != null)
        {
            Debug.Log($"[MortarProjectile] 지형 파괴 요청: {_targetPosition}");
            StageManager.Instance.DestroyBlocksInArea(_targetPosition, 1, terrainDestructionDepth);
        }
        else
        {
            Debug.LogError("[MortarProjectile] StageManager 인스턴스를 찾을 수 없습니다.");
        }

        // 폭발 범위 내 플레이어 데미지
        Collider[] hitColliders = Physics.OverlapSphere(_targetPosition, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerData playerData = hitCollider.GetComponent<PlayerData>();
                if (playerData != null)
                {
                    playerData.TakeDamage(damage);
                }
            }
        }
        Destroy(gameObject);
    }
}