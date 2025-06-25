using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance { get; private set; }
    
    // 현재 활성화되어 관리 중인 NavMeshSurface
    private NavMeshSurface _currentActiveSurface;
    public bool IsSurfaceBaked { get; private set; } = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 이미 인스턴스가 존재");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지정된 NavMeshSurface를 현재 활성 Surface로 등록하고 즉시 NavMesh를 빌드(또는 리빌드)
    /// </summary>
    /// <param name="surfaceToManage">관리하고 빌드할 NavMeshSurface</param>
    public void RegisterAndBakeSurface(NavMeshSurface surfaceToManage)
    {
        if (surfaceToManage == null)
        {
            IsSurfaceBaked = false;
            return;
        }

        _currentActiveSurface = surfaceToManage;
        BakeCurrentSurfaceInternal();
    }

    /// <summary>
    /// 현재 등록된 활성 NavMeshSurface에 대해 NavMesh를 다시 빌드
    /// Chunk의 지형이 변경되었을 때 호출
    /// </summary>
    public void RebakeCurrentSurface()
    {
        if (_currentActiveSurface == null)
        {
            IsSurfaceBaked = false;
            return;
        }
        BakeCurrentSurfaceInternal();
    }

    private void BakeCurrentSurfaceInternal()
    {
        if (_currentActiveSurface == null)
        {
            IsSurfaceBaked = false;
            return;
        }
        IsSurfaceBaked = false;
        // Debug.Log($"[NavMeshManager] NavMesh 빌드 시작: {_currentActiveSurface.gameObject.name}");
        _currentActiveSurface.BuildNavMesh();
        // Debug.Log($"[NavMeshManager] NavMesh 빌드 완료: {_currentActiveSurface.gameObject.name}");
        IsSurfaceBaked = true;
    }

    /// <summary>
    /// 현재 씬의 모든 NavMesh 데이터를 제거하고, 활성 Surface 참조를 초기화
    /// 주로 스테이지 변경 시 호출됩니다.
    /// </summary>
    public void ClearAllNavMeshData()
    {
        NavMesh.RemoveAllNavMeshData();
        _currentActiveSurface = null; // 활성 Surface 참조도 초기화
        IsSurfaceBaked = false;
    }

    /// <summary>
    /// 지정된 위치 근처에서 NavMesh 위의 유효한 위치를 찾습니다.
    /// </summary>
    /// <param name="center">검색을 시작할 중심 위치</param>
    /// <param name="sampleRadius">중심 위치로부터 검색할 반경</param>
    /// <param name="foundPosition">찾은 유효한 위치 (출력 파라미터)</param>
    /// <returns>유효한 위치를 찾았으면 true, 그렇지 않으면 false를 반환합니다.</returns>
    public bool FindValidPositionOnNavMesh(Vector3 center, float sampleRadius, out Vector3 foundPosition)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(center, out hit, sampleRadius, NavMesh.AllAreas))
        {
            foundPosition = hit.position;
            return true;
        }
        foundPosition = center; // 유효한 위치를 찾지 못한 경우, 입력된 center 위치를 그대로 반환
        return false;
    }

    // 게임 오브젝트 파괴 시 Instance 참조 정리
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}