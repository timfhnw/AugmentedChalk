using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using static UnityEngine.GraphicsBuffer;

public class CameraImageCapture : MonoBehaviour
{
    [SerializeField] float ocrInterval = 2f;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] Material plotMaterial;
    [SerializeField] Material plotSelectionMaterial;
    [SerializeField] GameObject frame;
    [SerializeField] Texture2D testImage;
    [SerializeField] RawImage arImageFeed;
    [SerializeField] InputActionReference placeAction;
    [SerializeField] InputActionReference pointAction;
    [SerializeField] int plotStart = -10;
    [SerializeField] int plotEnd = 10;
    [SerializeField] Material gaussianBlurMat;
    [SerializeField] int maxFails = 7;
    [SerializeField] UIHandler ui;
    [SerializeField] const float holdThreshold = 0.75f;
    [SerializeField] InputActionReference dragDeltaAction;
    [SerializeField] InputActionReference dragPosAction;
    [SerializeField] InputActionReference rotationAction;
    float touchDuration = 0f;
    bool holding = false;
    Vector2 lastTouchPos;
    GameObject selectedPlot;
    TesseractDriver tesseractDriver;
    bool isTesseractReady = false;
    float timer = 0f;


    Coroutine moveRoutine;

    List<GameObject> plots;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    bool plotReady = false;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        placeAction.action.performed += OnPlacePerformed;
        placeAction.action.Enable();
        dragDeltaAction.action.Enable();
        dragPosAction.action.Enable();
        rotationAction.action.Enable();
    }

    void OnDisable()
    {
        placeAction.action.performed -= OnPlacePerformed;
        rotationAction.action.Disable();
        placeAction.action.Disable();
        dragDeltaAction.action.Disable();
        dragPosAction.action.Disable();
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        EnhancedTouchSupport.Enable();
        tesseractDriver = new TesseractDriver();
        tesseractDriver.Setup(() => isTesseractReady = true);
        plots = new List<GameObject>();
        if (ui.deleteButton != null) ui.deleteButton.onClick.AddListener(OnDeleteButtonPressed);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (isTesseractReady && timer >= ocrInterval)
        {
            timer = 0f;
            CaptureImage();
        }

        if (ui.isDeleteActive() == (selectedPlot == null))
        {
            ui.setDelete(selectedPlot != null);
        }

        if (selectedPlot != null)
        {
            float twist = rotationAction.action.ReadValue<float>();
            if (Mathf.Abs(twist) > 0.01f) // avoid stutter in rotation
            {
                Debug.Log(twist);
                selectedPlot.transform.Rotate(selectedPlot.transform.up, -twist, Space.World); // own axis rotation
            } 
            else if (dragDeltaAction.action.enabled && dragPosAction.action.triggered)
            {
                Vector2 dragPos = dragPosAction.action.ReadValue<Vector2>();
                TryMoveSelectedPlot(dragPos);
            }
        }
    }

    void TryMoveSelectedPlot(Vector2 screenPos)
    {
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 targetPos = hits[0].pose.position;
            if (Vector3.Distance(selectedPlot.transform.position, targetPos) > 0.01f)
            {
                MovePlotToTarget(selectedPlot, targetPos, 0.15f);
            }
        }
    }

    public void OnDeleteButtonPressed()
    {
        if (selectedPlot == null) return;
        plots.Remove(selectedPlot);
        Destroy(selectedPlot);
        selectedPlot = null;
        ui.setDelete(false);
    }


    void MovePlotToTarget(GameObject plot, Vector3 targetPos, float duration)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(SmoothMove(plot, targetPos, duration));
    }

    IEnumerator SmoothMove(GameObject plot, Vector3 targetPos, float duration)
    {
        Vector3 start = plot.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            plot.transform.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        plot.transform.position = targetPos;
    }

    void OnPlacePerformed(InputAction.CallbackContext ctx)
    {
        if ((ui != null && ui.isActive())) return;
        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        if (TrySelectPlot(screenPos)) return;
        if (holding) return;
        TryPlacePlot(screenPos);
    }

    void CaptureImage()
    {
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            Texture2D texture = ConvertCaptureToTexture(image);
            if (arImageFeed != null) arImageFeed.texture = texture;
            if (testImage != null)
            {
                texture = CopyTextureReadable(testImage);
            }
            string recognizedText = tesseractDriver.Recognize(texture);
            Debug.Log(recognizedText);
            string func = ProcessFunction.ExtractFunction(recognizedText);
            Debug.Log(func);
            if (func == null) return;
            var expr = ProcessFunction.ConvertTextToExpression(func);
            Debug.Log(expr.Latexise());

            var plot = ProcessFunction.Plot3D(expr, plotStart, plotEnd, plotMaterial, plotSelectionMaterial, frame);
            var plotRenderer = plot.GetComponent<Renderer>();
            if (plotRenderer != null) plotRenderer.enabled = false;
            plot.SetActive(false);
            plots.Add(plot);
            plotReady = true;
            if (ui != null) ui.ShowOverlay();
        }
    }

    bool TrySelectPlot(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (plots.Contains(hit.collider.gameObject))
            {
                if (selectedPlot != null && selectedPlot == hit.collider.gameObject)
                {
                    selectedPlot.GetComponentInChildren<Renderer>().material = plotMaterial;
                    selectedPlot = null;
                    return true;
                }
                if(selectedPlot != null) selectedPlot.GetComponentInChildren<Renderer>().material = plotMaterial;
                selectedPlot = hit.collider.gameObject;
                selectedPlot.GetComponentInChildren<Renderer>().material = plotSelectionMaterial;
                return true;
            }
            if (selectedPlot != null && hit.collider.gameObject != null)
            {
                selectedPlot.GetComponentInChildren<Renderer>().material = plotMaterial;
                selectedPlot = null;
                return true;
            }
        }
        return false;
    }

    Texture2D Blur(Texture2D t2d)
    {
        return ProcessFunction.ApplyGaussianBlur(t2d, gaussianBlurMat, 2, 5);
    }

    void TryPlacePlot(Vector2 touchPosition)
    {
        if (!plotReady) return;
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            pose.position.y += 0.01f;
            var plot = plots.Last();
            plot.transform.position = pose.position;
            plot.transform.rotation = pose.rotation;
            plot.SetActive(true);
            plot.AddComponent<ARAnchor>();
            var plotRenderer = plot.GetComponent<Renderer>();
            if (plotRenderer != null) plotRenderer.enabled = true;
            plotReady = false;
            if (selectedPlot != null) selectedPlot.GetComponentInChildren<Renderer>().material = plotMaterial;
            selectedPlot = plot;
            selectedPlot.GetComponentInChildren<Renderer>().material = plotSelectionMaterial;
        }
    }


    Texture2D ConvertCaptureToTexture(XRCpuImage image)
    {
        var transform = XRCpuImage.Transformation.MirrorY;
        var rect = new RectInt(0, 0, image.width, image.height);
        var outDims = new Vector2Int(image.width, image.height);
#if UNITY_EDITOR
        transform = XRCpuImage.Transformation.None;
        rect = new RectInt(0, 0, image.width, image.height);
        outDims = new Vector2Int(image.width, image.height);
#endif
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = rect,
            outputDimensions = outDims,
            outputFormat = TextureFormat.RGBA32,
            transformation = transform
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);

        var texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(buffer);
        texture.Apply();

        buffer.Dispose();
        image.Dispose();
#if UNITY_EDITOR
        return texture;
#endif
        return RotateTexture90(texture);
    }

    Texture2D RotateTexture90(Texture2D source)
    {
        int width = source.width;
        int height = source.height;

        Texture2D result = new Texture2D(height, width, source.format, false);
        Color[] srcPixels = source.GetPixels();
        Color[] dstPixels = new Color[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                dstPixels[x * height + (height - y - 1)] = srcPixels[y * width + x];
            }
        }

        result.SetPixels(dstPixels);
        result.Apply();
        return result;
    }

    Texture2D CopyTextureReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    void OnDestroy()
    {
        placeAction.action.performed -= OnPlacePerformed;
    }
}