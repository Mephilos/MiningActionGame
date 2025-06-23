using UnityEngine;

public class ReturnToPoolAfterDelay : MonoBehaviour
{
    public float lifeTime = 2f;
    public string poolTag;
    private ParticleSystem _particleSystem;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        float delay = lifeTime;
        if (_particleSystem != null)
        {
            delay = _particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax;
        }
        Invoke(nameof(Return), delay);
    }

    void Return()
    {
        if (gameObject.activeInHierarchy)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}