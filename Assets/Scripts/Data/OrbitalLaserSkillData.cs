using UnityEngine;

[CreateAssetMenu(fileName = "OrbitalLaseerSkill", menuName = "Player Character/OrbitalLaseerSkillData")]
public class OrbitalLaserSkillData : SkillData
{
    public GameObject laserBeamPrefab;
    public float duration;
    public float tickRate;
    public float damagePerTick;
    public float radius;
    public override void ExecuteEffect(Vector3 position)
    {
        if (laserBeamPrefab == null) return;
        GameObject laserGO = Instantiate(laserBeamPrefab, position, Quaternion.identity);

        if (laserGO.TryGetComponent<OrbitalLaserBeam>(out OrbitalLaserBeam beam))
        {
            
        }
    }
}
