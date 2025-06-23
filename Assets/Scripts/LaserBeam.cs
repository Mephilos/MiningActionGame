using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    public float beamDuration = 0.15f; // 레이저가 보이는 시간
    public float fadeOutDelay = 0.05f; // 사라지기 시작 전 딜레이 (beamDuration 내에 포함)
    public float fadeOutDuration = 0.1f; // 사라지는 데 걸리는 시간 (beamDuration 내에 포함)

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            Debug.LogError("LaserBeam 스크립트는 LineRenderer 컴포넌트가 필요합니다.");
            enabled = false;
        }
    }

    public void Show(Vector3 startPoint, Vector3 endPoint)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);
        gameObject.SetActive(true); // 혹시 모르니 활성화
        
        StartCoroutine(FadeOutBeam());
    }

    private IEnumerator FadeOutBeam()
    {
        // 초기 딜레이
        yield return new WaitForSeconds(Mathf.Max(0, beamDuration - fadeOutDelay - fadeOutDuration)); 
        
        // 서서히 사라지는 효과
        if (fadeOutDuration > 0)
        {
            float timer = 0;
            Color originalStartColor = _lineRenderer.startColor;
            Color originalEndColor = _lineRenderer.endColor;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
                _lineRenderer.startColor = new Color(originalStartColor.r, originalStartColor.g, originalStartColor.b, alpha);
                _lineRenderer.endColor = new Color(originalEndColor.r, originalEndColor.g, originalEndColor.b, alpha);
                yield return null;
            }
        }
        Destroy(gameObject); // 최종적으로 오브젝트 파괴
    }
}