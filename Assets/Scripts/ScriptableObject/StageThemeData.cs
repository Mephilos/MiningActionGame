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
    // TODO: 추후 등장 아이템, 적, 배경음악 같은 걸 설계
}