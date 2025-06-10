using UnityEngine;

[CreateAssetMenu(fileName = "New FireSupportSkillData", menuName = "Player Character/FireSupportSkillData")]
public class FireSupportSkillData : SkillData
{
    [Header("포격 스킬 전용 설정")]
    public GameObject grenadePrefab; // 이 스킬이 사용할 수류탄 프리팹
    public float throwForce = 15f;

    [Header("포격 설정")]
    public GameObject fireSupportProjectilePrefab; // 떨어질 포탄 프리팹
    public GameObject targetIndicatorPrefab; // 포탄 낙하 지점 표시 프리팹
    public int waves = 3;
    public int projectilesPerWave = 4;
    public float spawnRadius;
    public float aimDuration;
    public float fallDuration;

    // PlayerController가 이 메서드를 호출
    public override void Activate(PlayerController player)
    {
        if (grenadePrefab == null || player.grenadeThrowPoint == null) return;

        // 마우스 커서 방향으로 수류탄 발사
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;
        // 플레이어 레이어는 충돌에서 제외
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~LayerMask.GetMask("Player")))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(50f);
        }

        Transform throwPoint = player.grenadeThrowPoint;
        Vector3 direction = (targetPoint - throwPoint.position).normalized;

        GameObject grenadeGO = Instantiate(grenadePrefab, throwPoint.position, Quaternion.LookRotation(direction));
        
        // 생성된 수류탄에 이 스킬 데이터(자기 자신)를 전달
        if(grenadeGO.TryGetComponent<SkillGrenade>(out var skillGrenade))
        {
            skillGrenade.sourceSkillData = this;
        }
        
        if (grenadeGO.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(direction * throwForce, ForceMode.Impulse);
        }
    }

    // 수류탄이 땅에 닿으면 이 메서드를 호출
    public void ExecuteStrike(Vector3 position)
    {
        if (StageManager.Instance != null)
        {
            // StageManager에 포격 요청 시, 스킬 데이터를 함께 넘겨줌
            StageManager.Instance.RequestFireSupportStrike(position, this);
        }
    }
}
