// Scripts/Data 폴더에 새로 생성
using UnityEngine;

public abstract class SkillData : ScriptableObject
{
    [Header("공통 스킬 정보")]
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;
    public float cooldown =  30f;

    // 스킬 발동 시 호출될 메서드
    public abstract void Activate(PlayerController player);
}