using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceHealthBar : MonoBehaviour
{
    public Image healthBarFill;
    public Image attackGaugeFill;
    public Transform targetToFollow;
    public Vector3 offset = new Vector3(0, 2f, 0);
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        if (attackGaugeFill != null)
        {
            attackGaugeFill.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (targetToFollow != null && _mainCamera != null)
        {
            transform.position = targetToFollow.position + offset;
            transform.LookAt(transform.position + _mainCamera.transform.forward);
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (maxHealth > 0)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    public void UpdateAttackGauge(float fillAmount)
    {
        if (attackGaugeFill != null)
        {
            attackGaugeFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    public void SetAttackGaugeVisibility(bool isVisible)
    {
        if (attackGaugeFill != null && attackGaugeFill.gameObject.activeSelf != isVisible)
        {
            attackGaugeFill.gameObject.SetActive(isVisible);
        }
    }

    public void SetVisibility(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
