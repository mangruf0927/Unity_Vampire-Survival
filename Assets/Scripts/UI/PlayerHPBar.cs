
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBar : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 worldOffset = new(0f, -1f, 0f);
    [SerializeField] private Vector2 screenOffset = new(0f, -14f);

    private RectTransform barRect;
    private RectTransform parentRect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (!ValidateReferences()) return;

        InitializeUI();
    }

    private void OnEnable()
    {
        if (playerHealth == null) return;

        playerHealth.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.RemoveObserver(this);
    }

    private void LateUpdate()
    {
        if (playerHealth == null || gameCamera == null || parentRect == null) return;

        UpdateBarPosition();
    }

    private bool ValidateReferences()
    {
        if (playerHealth == null || gameCamera == null || slider == null)
        {
            Debug.LogWarning("PlayerHPBar의 Player Health, Camera, Slider를 지정해 주세요.", this);
            enabled = false;
            return false;
        }

        barRect = slider.transform as RectTransform;
        parentRect = barRect.parent as RectTransform;
        canvas = slider.GetComponentInParent<Canvas>();

        if (parentRect != null && canvas != null) return true;

        Debug.LogWarning("PlayerHPBar의 Canvas 및 RectTransform 구성을 확인해 주세요.", this);
        enabled = false;
        return false;
    }

    private void InitializeUI()
    {
        canvasGroup = slider.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = slider.gameObject.AddComponent<CanvasGroup>();
    }

    private void UpdateBarPosition()
    {
        Vector3 screenPosition = gameCamera.WorldToScreenPoint(playerHealth.transform.position + worldOffset);

        bool visible = screenPosition.z > 0f;
        canvasGroup.alpha = visible ? 1f : 0f;

        if (!visible) return;

        Camera uiCamera = GetUICamera();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCamera, out Vector2 localPosition))
        {
            barRect.anchoredPosition = localPosition + screenOffset;
        }
    }

    private Camera GetUICamera()
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera != null ? canvas.worldCamera : gameCamera;
    }

    public void Notify()
    {
        if (playerHealth == null || slider == null) return;

        float ratio = playerHealth.MaxHealth > 0f ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth : 0f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
    }
}
