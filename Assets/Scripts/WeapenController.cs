using UnityEngine;

public class WeapenController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 30f;
    public float maxFireDistance = 100f;
    public bool rotateOwnerToAim = true;

    private Transform owner; // 사용자 본체

    [Header("드릴 셋팅")]
    public float drillDamage = 1f;
    public float drillDelay = 0.5f;
    public float drillRange = 3f;
    private float lastDrillTime = 0f;

    public enum WeaponMode
    {
        Projectile,
        Drill
    }

    public WeaponMode currentWeaponMode = WeaponMode.Projectile;
    void Awake()
    {
        owner = transform;
    }

    void Update()
    {
        HandleWeaponSwitchInput();

        if (currentWeaponMode == WeaponMode.Projectile)
        {
            if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭 시 발사
            {
                FireProjectile();
            }
        }
        else if (currentWeaponMode == WeaponMode.Drill)
        {
            if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 누르고 있을 때 드릴 작동 시도
            {
                TryUseDrill();
            }
        }
    }
    /// <summary>
    /// 숫자 키 입력으로 무기 모드를 전환
    /// </summary>
    void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 숫자 1번 키
        {
            currentWeaponMode = WeaponMode.Projectile;
            Debug.Log("무기 변경: 발사체");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // 숫자 2번 키
        {
            currentWeaponMode = WeaponMode.Drill;
            Debug.Log("무기 변경: 드릴");
        }
    }

    public void FireProjectile()
    {
        //카메라 중심에 Ray 생성
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        Debug.DrawRay(ray.origin, ray.direction * maxFireDistance, Color.yellow, 1f);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxFireDistance))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxFireDistance;
        }
        if (rotateOwnerToAim)
        {
            Vector3 lookDir = targetPoint - owner.position;
            lookDir.y = 0;

            if (lookDir.sqrMagnitude > 0.01f)
            {
                owner.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        // 발사체 생성
        Vector3 fireDir = (targetPoint - firePoint.position).normalized;
        //미사일 머리 방향 수정
        Quaternion bulletRotation = Quaternion.LookRotation(fireDir) * Quaternion.Euler(90f, 0f, 0f);
        //미사일 인스턴스 생성
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        bullet.GetComponent<Rigidbody>().linearVelocity = fireDir * bulletSpeed;
        Debug.Log("발사");
    }

    void TryUseDrill()
    {
        if (Time.time >= lastDrillTime + drillDelay) // 공격 딜레이 확인
        {
            UseDrill();
            lastDrillTime = Time.time; // 마지막 공격 시간 갱신
        }
    }
    void UseDrill()
    {
        Ray drillRay = new Ray(firePoint.position, Camera.main.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(drillRay.origin, drillRay.direction * drillRange, Color.blue, 0.5f);

        if (Physics.Raycast(drillRay, out hit, drillRange)) // 수정된 drillRay 사용
        {
            if (hit.collider.CompareTag("Chunk") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Vector3 hitPoint = hit.point;           // 충돌한 월드 좌표
                Vector3 hitNormal = hit.normal;         // 충돌한 표면의 법선 벡터

                // 블록 경계에 정확히 맞으면 어떤 블록을 지울지 모호할 수 있으므로 법선 벡터 반대 방향(안쪽)으로 살짝 이동한 지점을 기준으로 블록 좌표를 계산
                Vector3 blockToDestroyLocation = hitPoint - hitNormal * 0.01f;

                int blockX = Mathf.FloorToInt(blockToDestroyLocation.x);
                int blockY = Mathf.FloorToInt(blockToDestroyLocation.y);
                int blockZ = Mathf.FloorToInt(blockToDestroyLocation.z);

                Debug.Log($"드릴: 블록 감지 ({blockX}, {blockY}, {blockZ}). 파괴 시도");

                if (ChunkManager.Instance != null)
                {
                    // 감지된 단일 블록 파괴
                    bool destroyed = ChunkManager.Instance.DestroyBlockAt(blockX, blockY, blockZ);
                    if (destroyed)
                    {
                        Debug.Log($"드릴: 블록 파괴 성공 ({blockX}, {blockY}, {blockZ})");
                        // TODO: 효과음 파티클 추가
                    }
                    else
                    {
                        Debug.LogWarning($"드릴: 블록 파괴 실패 ({blockX}, {blockY}, {blockZ}) (이미 공기이거나 범위 밖일 수 있습니다)");
                    }
                }
                else
                {
                    Debug.LogError("Chunk 인스턴스를 찾을 수 없습니다");
                }
            }
        }
    }
}
