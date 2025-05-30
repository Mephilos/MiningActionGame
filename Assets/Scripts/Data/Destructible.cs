using UnityEngine;
using System.Collections.Generic;

public class Destructible : MonoBehaviour
{
    private DestructibleObjectData _data;
    private float _currentHealth;
    private bool _isDead = false;

    public void Initialize(DestructibleObjectData data)
    {
        _data = data;
        if (_data != null)
        {
            _currentHealth = _data.health;
            gameObject.name = $"{_data.objectName}_{transform.position.GetHashCode()}"; // 오브젝트 이름 설정
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] DestructibleObjectData가 null입니다! 기본값으로 초기화합니다.");
            _currentHealth = 10f; // 임시 기본 체력
        }
    }

    public void TakeDamage(float amount, PlayerData attackerData = null) // 공격자 정보 추가 (선택사항)
    {
        if (_isDead || _data == null || _currentHealth <= 0) return;

        _currentHealth -= amount;
        // Debug.Log($"[{gameObject.name}] {_data.objectName}이(가) {amount} 데미지를 받음. 현재 체력: {_currentHealth}");

        // TODO: 피격 이펙트, 사운드 등

        if (_currentHealth <= 0)
        {
            Die(attackerData);
        }
    }

    void Die(PlayerData attackerData = null)
    {
        if (_isDead) return;
        _isDead = true;

        // Debug.Log($"[{gameObject.name}] {_data.objectName} 파괴됨!");

        // 파괴 이펙트 및 사운드 재생
        if (_data.destructionEffectPrefab != null)
        {
            Instantiate(_data.destructionEffectPrefab, transform.position, transform.rotation);
        }
        if (_data.destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(_data.destructionSound, transform.position, 1.0f); // 볼륨 인자 추가
        }

        // 루트 테이블 기반 아이템 드랍
        if (_data.lootTable != null && _data.lootTable.Length > 0)
        {
            foreach (LootDropData dropInfo in _data.lootTable)
            {
                if (dropInfo.itemData == null || dropInfo.itemData.itemPickupPrefab == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] LootTable의 ItemData 또는 itemPickupPrefab이 null입니다.");
                    continue;
                }

                if (Random.value < dropInfo.dropChance) // dropChance (0.0 ~ 1.0)
                {
                    int amountToDrop = Random.Range(dropInfo.minAmount, dropInfo.maxAmount + 1);
                    for (int i = 0; i < amountToDrop; i++)
                    {
                        Vector3 dropPositionOffset = Random.insideUnitSphere * 0.5f; // 드랍 위치 약간 분산
                        dropPositionOffset.y = Mathf.Abs(dropPositionOffset.y * 0.5f) + 0.2f; // 살짝 위로
                        
                        GameObject pickupInstance = Instantiate(dropInfo.itemData.itemPickupPrefab, transform.position + dropPositionOffset, Quaternion.identity);
                        Collectible collectibleScript = pickupInstance.GetComponent<Collectible>();
                        if (collectibleScript != null)
                        {
                            collectibleScript.itemData = dropInfo.itemData; // 드랍된 아이템에 ItemData 설정
                        }
                        // Debug.Log($"[{_data.objectName}] 드랍: {dropInfo.itemData.itemName} (수량: 1)");
                    }
                }
            }
        }
        // 오브젝트 실제 파괴
        Destroy(gameObject);
    }
}