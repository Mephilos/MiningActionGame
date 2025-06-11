using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceHealthBar : MonoBehaviour
{
    public Image healthBarFill;
    public Transform targetToFollow;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (maxHealth > 0)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    void LateUpdate()
    {
        if (targetToFollow != null && mainCamera != null)
        {
            transform.position = targetToFollow.position + offset;
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }

    public void SetVisibility(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
