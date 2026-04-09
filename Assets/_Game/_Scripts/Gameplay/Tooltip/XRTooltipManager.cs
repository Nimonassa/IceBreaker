using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRTooltipManager : MonoBehaviour
{
    public static XRTooltipManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Dynamic Spline Settings")]
    [SerializeField] private Material splineMaterial;
    [SerializeField] private float splineWidth = 0.002f;
    [SerializeField] private int splineSegments = 20;
    [SerializeField] private float startTension = 0.08f;
    [SerializeField] private float endTension = 0.05f;

    [Header("Connection Sphere")]
    [SerializeField] private XRTooltipAnchor spherePrefab;
    [SerializeField] private float sphereRadius = 0.005f;

    [Header("Layout & Movement")]
    [SerializeField] private float verticalOffset = 0.05f;
    [SerializeField] private float horizontalOffset = 0.08f;
    [SerializeField] private float speed = 15f;
    [SerializeField] private float fadeDuration = 0.25f;

    private XRControllerButtonMap leftMap, rightMap;
    private Camera cam;
    private Transform activeController, activeTarget;
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;

    private XRTooltipAnchor sphereInstance;
    private LineRenderer splineRenderer;
    private Coroutine fadeCoroutine;

    private Material instancedSplineMat, instancedSphereMat;
    private Color baseSplineColor, baseSplineEmit;
    private Color baseSphereColor, baseSphereEmit;

    // Registry for handling multiple interactors per hand
    private IXRInteractor leftActiveInteractor;
    private IXRInteractor rightActiveInteractor;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        panelRect = panel.GetComponent<RectTransform>();

        if (!panel.TryGetComponent(out canvasGroup)) 
            canvasGroup = panel.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        panel.SetActive(false);

        // --- Spline Setup ---
        GameObject lineObj = new GameObject("TooltipSpline");
        lineObj.transform.SetParent(transform);
        splineRenderer = lineObj.AddComponent<LineRenderer>();
        splineRenderer.startWidth = splineRenderer.endWidth = splineWidth;
        splineRenderer.useWorldSpace = true;
        splineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        splineRenderer.receiveShadows = false;

        instancedSplineMat = new Material(splineMaterial ? splineMaterial : new Material(Shader.Find("Standard")));
        splineRenderer.material = instancedSplineMat;

        baseSplineColor = instancedSplineMat.HasProperty("_Color") ? instancedSplineMat.color : Color.white;
        baseSplineEmit = instancedSplineMat.HasProperty("_EmissionColor") ? instancedSplineMat.GetColor("_EmissionColor") : Color.black;

        splineRenderer.enabled = false;

        // --- Sphere Setup ---
        if (spherePrefab != null)
        {
            sphereInstance = Instantiate(spherePrefab, transform);
            sphereInstance.SetRadius(sphereRadius);
            instancedSphereMat = new Material(sphereInstance.Material);
            sphereInstance.GetComponent<MeshRenderer>().material = instancedSphereMat;

            baseSphereColor = instancedSphereMat.HasProperty("_Color") ? instancedSphereMat.color : Color.white;
            baseSphereEmit = instancedSphereMat.HasProperty("_EmissionColor") ? instancedSphereMat.GetColor("_EmissionColor") : Color.black;

            sphereInstance.gameObject.SetActive(false);
        }
    }

    // --- INTERACTOR REGISTRY ---
    public void SetActiveInteractor(ControllerSide side, IXRInteractor interactor)
    {
        if (side == ControllerSide.Left) leftActiveInteractor = interactor;
        else rightActiveInteractor = interactor;
    }

    public void RegisterControllerMap(XRControllerButtonMap map, ControllerSide side)
    {
        if (side == ControllerSide.Left) leftMap = map;
        else rightMap = map;
    }

    // --- PUBLIC API ---
    public void Show(string text, ControllerSide side, XRButtonType targetButton, Transform target)
    {
        promptText.text = text;
        activeTarget = target;

        // Retrieve the interactor that actually triggered the hover
        IXRInteractor interactor = (side == ControllerSide.Left) ? leftActiveInteractor : rightActiveInteractor;
        if (interactor == null) return;

        XRControllerButtonMap activeMap = (side == ControllerSide.Left) ? leftMap : rightMap;
        activeController = (activeMap != null) ? activeMap.GetButtonTransform(targetButton) : interactor.transform;

        // Instant teleport to position before showing
        GetTargetPose(out Vector3 startPos, out Quaternion startRot);
        panel.transform.SetPositionAndRotation(startPos, startRot);

        panel.SetActive(true);
        splineRenderer.enabled = true;
        if (sphereInstance) sphereInstance.gameObject.SetActive(true);

        FadeTo(canvasGroup.alpha, 1f);
    }

    public void Hide() => FadeTo(canvasGroup.alpha, 0f, () =>
    {
        panel.SetActive(false);
        splineRenderer.enabled = false;
        if (sphereInstance) sphereInstance.gameObject.SetActive(false);
    });

    // --- ANIMATION & FADING ---
    private void FadeTo(float from, float to, Action callback = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(from, to, callback));
    }

    private IEnumerator FadeRoutine(float from, float to, Action callback)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);

            canvasGroup.alpha = alpha;
            SetMaterialAlpha(instancedSplineMat, baseSplineColor, baseSplineEmit, alpha);
            
            if (instancedSphereMat)
                SetMaterialAlpha(instancedSphereMat, baseSphereColor, baseSphereEmit, alpha);

            yield return null;
        }
        canvasGroup.alpha = to;
        callback?.Invoke();
    }

    private void SetMaterialAlpha(Material mat, Color baseCol, Color baseEmit, float alpha)
    {
        Color c = baseCol;
        c.a *= alpha;
        mat.color = c;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", baseEmit * alpha);
        }
    }

    // --- TRANSFORM CALCULATIONS ---
    private void GetTargetPose(out Vector3 targetPos, out Quaternion targetRot)
    {
        if (activeController == null) { targetPos = Vector3.zero; targetRot = Quaternion.identity; return; }

        Vector3 controllerVP = cam.WorldToViewportPoint(activeController.position);
        float sideMultiplier = (controllerVP.z > 0 && controllerVP.x > 0.5f) ? -1f : 1f;
        Vector3 dynamicOffset = (cam.transform.up * verticalOffset) + (cam.transform.right * horizontalOffset * sideMultiplier);
        
        targetPos = activeController.position + dynamicOffset;
        targetRot = Quaternion.LookRotation(targetPos - cam.transform.position);
    }

    void LateUpdate()
    {
        if (!panel.activeSelf || !activeController) return;

        GetTargetPose(out Vector3 targetPos, out Quaternion targetRot);
        
        panel.transform.SetPositionAndRotation(
            Vector3.Lerp(panel.transform.position, targetPos, Time.deltaTime * speed),
            Quaternion.Lerp(panel.transform.rotation, targetRot, Time.deltaTime * speed)
        );

        Vector3 bottomEdge = panelRect.TransformPoint(new Vector3(panelRect.rect.center.x, panelRect.rect.yMin, 0));
        if (sphereInstance) sphereInstance.transform.position = bottomEdge;

        DrawCubicBezierCurve(activeController.position, bottomEdge);
    }

    private void DrawCubicBezierCurve(Vector3 start, Vector3 end)
    {
        splineRenderer.positionCount = splineSegments;
        float dist = Vector3.Distance(start, end);
        float curStartTension = Mathf.Min(startTension, dist * 0.5f);
        float curEndTension = Mathf.Min(endTension, dist * 0.5f);
        
        Vector3 directionToUI = (end - start).normalized;
        Vector3 smartForward = Vector3.Lerp(activeController.forward, directionToUI, 0.5f).normalized;

        for (int i = 0; i < splineSegments; i++)
        {
            float t = i / (float)(splineSegments - 1);
            float u = 1 - t;
            Vector3 pos = (u * u * u * start) + 
                          (3 * u * u * t * (start + smartForward * curStartTension)) +
                          (3 * u * t * t * (end + Vector3.down * curEndTension)) + 
                          (t * t * t * end);
            splineRenderer.SetPosition(i, pos);
        }
    }
}