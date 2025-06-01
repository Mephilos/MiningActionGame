using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceItemData", menuName = "Game Data/Items/Resource Item")]
public class ResourceItemData : ItemData
{
    [Header("자원 정보")]
    public int resourceAmount = 10;

    public override void Pickup(PlayerData playerData)
    {
        if (playerData != null)
        {
            playerData.GainResources(resourceAmount);
            Debug.Log($"플레이어가 {itemName} ({resourceAmount}) 획득");
        }
    }
}