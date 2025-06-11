using UnityEngine;

public class SkillGrenade : MonoBehaviour
{
    public SkillData sourceSkillData;

    public float lifeTime = 5f;
    public GameObject smokeEffectPrefab;
    public bool _isLanded = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isLanded && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            _isLanded = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            if (smokeEffectPrefab != null)
            {
                Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
            }

            if (sourceSkillData != null)
            {
                sourceSkillData.ExecuteEffect(transform.position);
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();

            if (renderer) renderer.enabled = false;
            Destroy(renderer, 1f);
        }
    }
}
