using UnityEngine;

[CreateAssetMenu(fileName = "New FireSupportSkillData", menuName = "Player Character/FireSupportSkillData")]
public class FireSupportSkillData : SkillData
{
    [Header("포격 설정")]
    public GameObject fireSupportProjectilePrefab; // 떨어질 포탄 프리팹
    public GameObject targetIndicatorPrefab; // 포탄 낙하 지점 표시 프리팹
    public int waves = 3;
    public int projectilesPerWave = 4;
    public float spawnRadius = 5f;
    public float aimDuration = 1.5f;
    public float fallDuration = 0.5f;

    public GameObject smokePrefab;
    // 수류탄이 땅에 닿으면 이 메서드를 호출
    public override void ExecuteEffect(Vector3 position)
    {
        if (StageManager.Instance != null)
        {
            // StageManager에 포격 요청 시, 스킬 데이터를 함께 넘겨줌
            StageManager.Instance.RequestFireSupportStrike(position, this);
        }
    }
}
