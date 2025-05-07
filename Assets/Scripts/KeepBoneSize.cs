using UnityEngine;

public class KeepBoneSize : MonoBehaviour
{
    public Vector3 Size = Vector3.one;

    void LateUpdate()
    {
        transform.localScale = Size;
    }
}
