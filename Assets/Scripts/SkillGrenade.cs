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
        int otherLayer = collision.gameObject.layer;
        
        if (!_isLanded && otherLayer == LayerMask.NameToLayer("Ground") || otherLayer == LayerMask.NameToLayer("Enemy"))
        {
            _isLanded = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            if (smokeEffectPrefab != null) Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
            
            if (sourceSkillData != null) sourceSkillData.ExecuteEffect(transform.position);
            

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer) renderer.enabled = false;
            Destroy(renderer, 1f);
        }
    }
}
