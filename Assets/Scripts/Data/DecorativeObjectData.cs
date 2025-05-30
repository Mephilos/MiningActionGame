using UnityEngine;

[CreateAssetMenu(fileName = "NewDecorativeObjectData", menuName = "Game Data/Decorative Object Data")]
public class DecorativeObjectData : ScriptableObject, IWeightedItem
{
    [Header("기본 설정")]
    public string objectName = "장식 오브젝트";
    public GameObject prefab; // LOD Group이 적용된 오브젝트 프리팹

    [Header("배치 설정")]
    public BlockType[] placeableOnBlockTypes = { BlockType.Grass, BlockType.Stone };
    public float yOffset = 0f;
    public float collisionRadius = 0.5f; // 겹침 방지용

    [Header("스폰 가중치")]
    [SerializeField] private float spawnWeight = 1f; // 인터페이스에서 사용할 실제 가중치 값
    public float SpawnWeight => spawnWeight;
}