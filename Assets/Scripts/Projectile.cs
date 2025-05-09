using UnityEngine;

public class Projectile : MonoBehaviour
{
    //TODO: 충돌시 파괴 효과
    // 수명 설정용
    public float lifeTime = 5f;

    void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    // 물리 충돌이 발생했을 때 호출되는 함수
    void OnCollisionEnter(Collision collision)
    {
        // 충돌한 대상이 Chunk인지 확인 (Tag 또는 Layer 사용 권장)
        if (collision.collider.CompareTag("Chunk") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // 충돌 지점 정보 가져오기
            ContactPoint contact = collision.contacts[0]; // 첫 번째 충돌 지점 정보 사용
            Vector3 hitPoint = contact.point;           // 충돌한 월드 좌표
            Vector3 hitNormal = contact.normal;         // 충돌한 표면의 법선 벡터 (바깥쪽 방향)

            // 파괴할 블록의 위치 계산 (충돌 지점에서 법선 방향으로 약간 안쪽으로 이동)
            // 블록 경계에 정확히 맞으면 어떤 블록을 지울지 모호할 수 있음
            // 법선 벡터 반대 방향(안쪽)으로 살짝 이동한 지점을 기준으로 블록 좌표를 계산
            Vector3 blockCenterToCheck = hitPoint - hitNormal * 0.01f;

            // 월드 좌표를 정수형 블록 좌표로 변환
            int blockX = Mathf.FloorToInt(blockCenterToCheck.x);
            int blockY = Mathf.FloorToInt(blockCenterToCheck.y);
            int blockZ = Mathf.FloorToInt(blockCenterToCheck.z);

            Debug.Log($"Collision with Chunk detected at World: {hitPoint}, checking block at: ({blockX}, {blockY}, {blockZ})");

            // ChunkManager를 통해 해당 좌표의 블록 파괴 요청
            // (ChunkManager에 블록 파괴 함수를 추가해야 함)
            bool destroyed = ChunkManager.Instance.DestroyBlockAt(blockX, blockY, blockZ); // Instance 싱글톤 또는 다른 접근 방식 필요

            if (destroyed)
            {
                Debug.Log($"Block destroyed successfully at ({blockX}, {blockY}, {blockZ})");
            }
            else
            {
                Debug.LogWarning($"Failed to destroy block at ({blockX}, {blockY}, {blockZ}). Maybe it's Air or out of bounds?");
            }

            // TODO: 충돌 효과 생성
        }

        // 발사체 자신은 충돌 후 파괴
        Destroy(gameObject);
    }
}
