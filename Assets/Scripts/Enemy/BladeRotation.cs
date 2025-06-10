using UnityEngine;
using UnityEngine.Serialization;

public class BladeRotation : MonoBehaviour
{
    public float rotationSpeed;
    void Update()
    {
        gameObject.transform.Rotate(0, rotationSpeed * 500 * Time.deltaTime, 0);
    }
}
