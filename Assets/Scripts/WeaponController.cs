using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("통상 무기 셋팅")]
    public WeaponData currentWeaponData;
    public Transform firePoint; // 발사 위치
    public float bulletSpeed = 30f; // 발사체 속도
    public float maxFireDistance = 100f; //최대 사거리

    private PlayerData _playerData; // 플레이어 스탯 참조
    private Transform _ownerTransform; // 사용자 본체
    private float _nextFireTime;

    public LayerMask aimLayerMask;
    
    private PlayerController _playerController;

    void Awake()
    {
        _ownerTransform = transform;
        _playerData = _ownerTransform.GetComponent<PlayerData>();
        _playerController = _ownerTransform.GetComponent<PlayerController>(); // PlayerController 참조

        if (currentWeaponData == null)
        {
            Debug.LogError($"{gameObject.name} WeaponData가 할당 되지 않음.");
        }
        if (_playerData == null)
        {
            Debug.LogError($"{gameObject.name} PlayerData가 할당 되지 않음.");
            enabled = false; 
            return;
        }
        if (_playerController == null) 
        {
            Debug.LogError($"{gameObject.name}의 WeaponController: PlayerController를 찾을 수 없습니다.");

        }
        if (firePoint == null)
        {
            Debug.LogError($"{gameObject.name}의 WeaponController: FirePoint가 할당되지 않았습니다. 발사 위치를 지정해주세요.");
            enabled = false;
            return;
        }
        if (aimLayerMask == 0)
        {
            aimLayerMask = LayerMask.GetMask("Default");
            Debug.LogWarning("WeaponController: aimLayerMask가 설정되지 않아 Default 레이어로 초기화합니다.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 클릭 시 발사
        {
            TryFireProjectile();
        }
    }

    void TryFireProjectile()
    {
        if (_playerData == null || currentWeaponData == null) return; 

        if (Time.time >= _nextFireTime)
        {
            FireProjectile();
            // 공격 속도는 PlayerData에서 가져옴
            _nextFireTime = Time.time + _playerData.currentAttackSpeed;
            Debug.Log($"공격 실행. 다음 공격 가능 시간: {_nextFireTime}, 현재 공격 속도(쿨다운): {_playerData.currentAttackSpeed}");
        }
    }
    public void FireProjectile()
    {
        if (currentWeaponData.projectilePrefab == null)
        {
            Debug.LogError("현재 무기 데이터 또는 발사체 프리팹이 설정되지 않았습니다");
            return;
        }

        // 발사 방향과 회전값을 항상 캐릭터의 정면으로 고정
        Vector3 fireDirection = _ownerTransform.forward;
        Quaternion bulletRotation = _ownerTransform.rotation; // 캐릭터의 현재 회전값을 그대로 사용

        // firePoint 위치에서 bulletRotation으로 발사체 생성
        GameObject bullet = Instantiate(currentWeaponData.projectilePrefab, firePoint.position, bulletRotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // fireDirection (캐릭터 정면 방향)으로 발사체에 속도 부여
            rb.linearVelocity = fireDirection * bulletSpeed;
        }

        Projectile projectileScript = bullet.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            float totalDamage = _playerData.currentAttackDamage + currentWeaponData.baseDamage;
            projectileScript.SetDamage(totalDamage);
        }
        // 캐릭터가 바라보는 방향(월드 좌표) 로그 추가
        Debug.Log($"발사체 데미지: {(_playerData.currentAttackDamage + currentWeaponData.baseDamage)}");
    }
}