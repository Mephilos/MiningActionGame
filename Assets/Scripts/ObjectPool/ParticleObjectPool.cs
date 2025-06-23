using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleObjectPool : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        StartCoroutine(ReturnAfterPlay());
    }

    private IEnumerator ReturnAfterPlay()
    {
        yield return new WaitForSeconds(_particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax);

        if (gameObject.activeInHierarchy)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}
