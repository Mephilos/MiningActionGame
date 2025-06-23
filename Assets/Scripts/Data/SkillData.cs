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
    
    public abstract void ExecuteEffect(Vector3 position);
}