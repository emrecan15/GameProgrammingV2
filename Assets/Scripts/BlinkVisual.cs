using UnityEngine;

public class BlinkVisual : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private bool isBlinking = false;
    private float blinkTimer = 0f;
    private float blinkRate = 0.15f; // Yanıp sönme hızı (düşük = daha hızlı)

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void StartBlinking()
    {
        isBlinking = true;
        blinkTimer = 0f;
    }

    public void StopBlinking()
    {
        isBlinking = false;
        if (meshRenderer != null) meshRenderer.enabled = true; // Kapanırken açık kalmasını garantile
    }

    void Update()
    {
        if (!isBlinking || meshRenderer == null) return;

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkRate)
        {
            blinkTimer = 0f;
            meshRenderer.enabled = !meshRenderer.enabled; // Görünürlüğü tersine çevir
        }
    }
}