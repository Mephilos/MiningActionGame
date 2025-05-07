using UnityEngine;

public class WeapenController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 30f;
    public float maxFireDistance = 100f;
    public bool rotateOwnerToAim = true;

    private Transform owner; // 사용자 본체

    void Awake()
    {
        owner = transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
    }

    public void Fire()
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
}
