using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthItemData", menuName = "Game Data/Items/Health Item")]
public class HealthItemData : ItemData
{
    [Header("체력 회복 정보")]
    public float healAmount = 25f;

    public override void Pickup(PlayerData playerData)
    {
        if (playerData != null)
        {
            playerData.Heal(healAmount);
            Debug.Log($"플레이어 {itemName}으로 체력 {healAmount} 회복");
        }
    }
}