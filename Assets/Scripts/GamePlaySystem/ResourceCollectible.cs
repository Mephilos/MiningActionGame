using UnityEngine;

public class ResourceCollectible : MonoBehaviour
{
    [Header("자원 설정")]
    [Tooltip("획득할 자원의 양")]
    public int resourceAmount = 100;

    [Header("수집 효과음")]
    public AudioClip collectSound; // 수집 시 재생할 소리
    private AudioSource _audioSource;

    private bool _isCollected = false; // 중복 수집 방지

    void Awake()
    {
        // 효과음 재생을 위한 AudioSource 준비
        if (collectSound != null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.clip = collectSound;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isCollected) return; // 이미 수집되었다면 중복 실행 방지

        if (other.CompareTag("Player"))
        {
            PlayerData playerData = other.GetComponent<PlayerData>();
            if (playerData != null)
            {
                _isCollected = true; // 수집됨으로 표시
                playerData.GainResources(resourceAmount);
                Debug.Log($"플레이어가 자원 아이템 획득: {resourceAmount}");

                if (_audioSource != null && collectSound != null)
                {
                    _audioSource.Play();
                    
                    Destroy(gameObject, _audioSource.clip.length);
                    
                    MeshRenderer renderer = GetComponent<MeshRenderer>();
                    if (renderer)
                    {
                        renderer.enabled = false;
                    }
                    Collider col = GetComponent<Collider>();
                    if (col)
                    {
                        col.enabled = false;
                    }
                }
                else
                {
                    Destroy(gameObject); // 사운드가 없으면 즉시 파괴
                }
            }
        }
    }
}