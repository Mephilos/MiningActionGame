using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity; 

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

   
        float horizontal = Input.GetAxis("Horizontal"); 
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}