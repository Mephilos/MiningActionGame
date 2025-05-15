using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class CollisionTest_CubeA : MonoBehaviour
{
    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.useGravity = false; 
        }

        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.isTrigger = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"OnCollisionEnter 충돌 ");
        if (collision.gameObject.CompareTag("PlayerTest")) 
            Debug.LogWarning($"(PlayerTest)와 충돌");        
    }
}

   
