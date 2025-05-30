using UnityEngine;

[System.Serializable]
public class LootDropData
{
    public ItemData itemData; // 드랍될 아이템의 ScriptableObject (ResourceItemData, HealthItemData 등)
    [Range(0f, 1f)] public float dropChance = 0.5f; // 이 아이템 그룹이 드랍될 확률
    public int minAmount = 1; // 최소 드랍 개수
    public int maxAmount = 1; // 최대 드랍 개수
}