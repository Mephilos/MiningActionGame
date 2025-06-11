// Scripts/Data 폴더에 새로 생성
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("공통 스킬 정보")]
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;
    public float cooldown =  30f;

    [Header("투척 설정")]
    public GameObject grenadePrefab;
    public float throwForce;

    /// <summary>
    /// 스킬 발동 (파생되는 메서드와 공유)
    /// 기본적인 스킬 공통 설정
    /// </summary>
    // public virtual void Activate(PlayerController player)
    // {
    //     if (grenadePrefab == null || player.grenadeThrowPoint == null) return;
    //
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
    //     {
    //         ThrowGrenade(player, hit.point);
    //     }
    //     else
    //     {
    //         ThrowGrenade(player, ray.GetPoint(50f));
    //     }
    // }
    //
    // private void ThrowGrenade(PlayerController player, Vector3 targetPoint)
    // {
    //     Transform throwPoint = player.grenadeThrowPoint;
    //     Vector3 direction = (targetPoint - throwPoint.position).normalized;
    //     GameObject grenadeGO = Instantiate(grenadePrefab, throwPoint.position, Quaternion.LookRotation(direction));
    //
    //     if (grenadeGO.TryGetComponent<SkillGrenade>(out SkillGrenade skillGrenade))
    //     {
    //         skillGrenade.sourceSkillData = this;
    //     }
    //
    //     if (grenadeGO.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
    //     {
    //         rigidbody.AddForce(direction * throwForce, ForceMode.Impulse);
    //     }
    // }
    public abstract void ExecuteEffect(Vector3 position);
}