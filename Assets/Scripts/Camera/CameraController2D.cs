using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // NEW input system

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 12f;      // WASD/arrow keys
    public float dragSpeed = 1.0f;    // Middle mouse drag

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minSize = 3f;
    public float maxSize = 30f;

    [Header("Clamp to Tilemap (optional)")]
    public Tilemap clampToTilemap;
    public float clampPadding = 1f;

    Camera cam;
    bool dragging;
    Vector3 dragOriginWorld;
    bool shuttingDown;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.orthographic = true;
    }

    void OnDisable()
    {
        // During scene unload, components may be destroyed mid-frame
        dragging = false;
        shuttingDown = true;
    }

    void OnApplicationQuit()
    {
        shuttingDown = true;
    }

    void Update()
    {
        if (shuttingDown || cam == null) return;

        HandlePan();
        HandleDrag();
        HandleZoom();
        ClampInsideTilemap();
    }

    void HandlePan()
    {
        if (cam == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);

        if (x != 0f || y != 0f)
            cam.transform.position += new Vector3(x, y, 0f) * panSpeed * Time.deltaTime;
    }

    void HandleDrag()
    {
        if (cam == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.middleButton.wasPressedThisFrame)
        {
            dragging = true;
            dragOriginWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        }
        if (mouse.middleButton.wasReleasedThisFrame)
            dragging = false;

        if (dragging)
        {
            var current = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var delta = dragOriginWorld - current;
            cam.transform.position += delta * dragSpeed;
        }
    }

    void HandleZoom()
    {
        if (cam == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scrollY = mouse.scroll.ReadValue().y; // + op / - ned
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scrollY * (zoomSpeed * 0.1f),
                minSize, maxSize
            );
        }
    }

    void ClampInsideTilemap()
    {
        if (cam == null) return;
        if (!clampToTilemap) return;

        var bounds = clampToTilemap.localBounds;
        var worldMin = clampToTilemap.transform.TransformPoint(bounds.min);
        var worldMax = clampToTilemap.transform.TransformPoint(bounds.max);

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        worldMin += new Vector3(-clampPadding, -clampPadding);
        worldMax += new Vector3(clampPadding, clampPadding);

        float minX = Mathf.Min(worldMin.x + halfW, worldMax.x - halfW);
        float maxX = Mathf.Max(worldMin.x + halfW, worldMax.x - halfW);
        float minY = Mathf.Min(worldMin.y + halfH, worldMax.y - halfH);
        float maxY = Mathf.Max(worldMin.y + halfH, worldMax.y - halfH);

        var p = cam.transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        cam.transform.position = p;
    }
}
