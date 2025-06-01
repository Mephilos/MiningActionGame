using UnityEngine;

[CreateAssetMenu(fileName = "NewStageTheme", menuName = "Game Data/Stage Theme Data")]
public class StageThemeData : ScriptableObject
{
    [Header("Theme Identification")]
    public string themeName = "Default Theme";

    [Header("Chunk Visuals")]
    public Material chunkMaterial; // 현 테마에서 사용할 청크 아틀라스 머티리얼

    [Header("Terrain Generation Overrides")]
    public bool overrideTerrainParameters = true; // StageManager 기본값 덮어쓰기 여부
    public float minNoiseScale = 0.02f; 
    public float maxNoiseScale = 0.04f; 
    public float minHeightMultiplier = 0f; 
    public float maxHeightMultiplier = 20f; 

    [Header("Gameplay")]
    public float timeToSurvivePerStage = 60f; // 이 테마의 스테이지에서 버텨야 하는 시간
    
    [Header("파괴 오브젝트")]
    public DestructibleObjectData[] destructibleObjects; // 이 테마에서 스폰될 파괴 가능 오브젝트 목록
    [Tooltip("청크 내에서 파괴 가능 오브젝트를 몇 번이나 스폰 시도할지 결정합니다.")]
    public int maxDestructibleSpawnAttempts = 10; // 이전: maxDestructibleObjectsPerChunk
    [Tooltip("각 스폰 시도에서 실제로 오브젝트가 스폰될 확률입니다.")]
    [Range(0f, 1f)] public float destructibleSpawnChancePerAttempt = 0.5f; // 이전: destructibleObjectSpawnChancePerChunkArea

    [Header("장식 오브젝트")]
    public DecorativeObjectData[] decorativeObjects; // 이 테마에서 스폰될 장식용 오브젝트 목록
    public int maxDecorativeSpawnAttempts = 20;
    [Range(0f, 1f)] public float decorativeSpawnChancePerAttempt = 0.7f;
    
    // TODO: 추후 등장 아이템, 적, 배경음악 같은 걸 설계
}