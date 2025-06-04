using UnityEngine;

[CreateAssetMenu(fileName = "NewDestructibleObjectData", menuName = "Game Data/Destructible Object Data")]
public class DestructibleObjectData : ScriptableObject, IWeightedItem
{
    [Header("기본 설정")]
    public string objectName = "파괴 가능한 오브젝트";
    public GameObject prefab; // LOD Group이 적용된 오브젝트 프리팹
    public float health = 100f;

    [Header("배치 설정")]
    public BlockType[] placeableOnBlockTypes = { BlockType.Grass, BlockType.Stone }; // 배치될 수 있는 바닥 블록 타입
    public float yOffset = 0f; // 바닥으로부터의 Y축 높이 오프셋 (오브젝트 피봇에 따라 조절)
    public float collisionRadius = 0.5f; // 겹침 방지 확인을 위한 반지름

    [Header("파괴 시 설정")]
    public LootDropData[] lootTable; // 파괴 시 드랍될 아이템 목록
    public GameObject destructionEffectPrefab; // 파괴 이펙트 프리팹
    public AudioClip destructionSound; // 파괴 효과음

    [Header("스폰 가중치")]
    [SerializeField] float spawnWeight = 1f; // 같은 테마 내 여러 오브젝트 중 선택될 확률 가중치
    public float SpawnWeight => spawnWeight;
}