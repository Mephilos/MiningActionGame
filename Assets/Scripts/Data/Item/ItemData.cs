using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("기본 아이템 정보")]
    public string itemName = "새 아이템";
    public GameObject itemPickupPrefab;

    /// <summary>
    /// 플레이어가 이 아이템을 획득했을 때 호출될 로직
    /// </summary>
    /// <param name="playerData">플레이어의 데이터</param>
    public abstract void Pickup(PlayerData playerData);
}