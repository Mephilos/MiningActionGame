using UnityEngine;

[CreateAssetMenu(fileName = "ExplosiveGrenadeSkill", menuName = "Player Character/ExplosiveGrenadeSkillData")] 
public class ExplosiveGrenadeSkillData : SkillData
{
    [Header("일반 수류탄 설정")] 
    public float damage;
    public float explosionRadius;
    public int terrainDestructionRadius;
    public GameObject impactEffectPrefab;
    public override void ExecuteEffect(Vector3 position)
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, position, Quaternion.identity);
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.DestroyBlocksInArea(position, terrainDestructionRadius, 3);
        }
        
        Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider collider in hitColliders)
        {
            if (collider.TryGetComponent<EnemyBase>(out EnemyBase enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
