using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    public ItemData itemData; // 이 픽업 오브젝트가 나타내는 아이템의 데이터
    public float attractionRadius = 3f; // 플레이어가 가까이 오면 끌려가는 반경
    public float attractionSpeed = 5f;  // 끌려가는 속도
    public AudioClip pickupSound; // 획득 시 효과음

    private Transform _playerTransform;
    private bool _isAttracting = false;

    protected virtual void Awake()
    {
        Collider col = GetComponent<Collider>();
    }

    protected virtual void Update()
    {
        if (_isAttracting && _playerTransform != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position + Vector3.up * 0.5f, attractionSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, _playerTransform.position + Vector3.up * 0.5f) < 0.5f)
            {
                PerformPickup();
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemData == null)
            {
                Debug.LogError($"[{gameObject.name}] ItemData 설정 필요");
                Destroy(gameObject);
                return;
            }
            // 끌어당기기 시작
            if (attractionRadius > 0)
            {
                _playerTransform = other.transform;
                _isAttracting = true;
            }
            else
            {
                _playerTransform = other.transform; // PerformPickup에서 사용하기 위함
                PerformPickup();
            }
        }
    }

    protected virtual void PerformPickup()
    {
        if (_playerTransform == null) return; // 안전장치

        PlayerData playerData = _playerTransform.GetComponent<PlayerData>();
        if (playerData != null && itemData != null)
        {
            itemData.Pickup(playerData); // ItemData에 정의된 Pickup 로직 실행

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            Destroy(gameObject);
        }
    }
}