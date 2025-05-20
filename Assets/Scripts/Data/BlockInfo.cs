using UnityEngine;

/// <summary>
/// 개별 블록이 자신의 타입을 기억하게 하기 위한 컴포넌트
/// 오브젝트 풀링에서 반환 시 타입 구분용으로 사용
/// </summary>
public class BlockInfo : MonoBehaviour
{
    [Header("이 블록의 타입")]
    public BlockType type;
}