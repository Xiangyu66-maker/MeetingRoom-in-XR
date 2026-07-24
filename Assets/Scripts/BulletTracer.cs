using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    [Header("Tracer Settings")]
    [SerializeField] private float travelTime = 0.06f;
    [SerializeField] private float visibleTime = 0.08f;
    [SerializeField] private float fadeTime = 0.08f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
    }

    public void Play(Vector3 startPosition, Vector3 endPosition)
    {
        StopAllCoroutines();
        StartCoroutine(
            TracerRoutine(startPosition, endPosition)
        );
    }

    private IEnumerator TracerRoutine(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        lineRenderer.enabled = true;

        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, startPosition);

        float elapsedTime = 0f;

        // 弹道从枪口快速飞到目标点
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / travelTime
            );

            Vector3 currentPosition = Vector3.Lerp(
                startPosition,
                endPosition,
                progress
            );

            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, currentPosition);

            yield return null;
        }

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);

        yield return new WaitForSeconds(visibleTime);

        elapsedTime = 0f;

        // 弹道逐渐透明
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeTime
            );

            float alpha = 1f - progress;

            Color fadedStartColor = startColor;
            Color fadedEndColor = endColor;

            fadedStartColor.a = startColor.a * alpha;
            fadedEndColor.a = endColor.a * alpha;

            lineRenderer.startColor = fadedStartColor;
            lineRenderer.endColor = fadedEndColor;

            yield return null;
        }

        lineRenderer.enabled = false;

        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;

        Destroy(gameObject);
    }
}