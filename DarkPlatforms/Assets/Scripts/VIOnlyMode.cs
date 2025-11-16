using UnityEngine;
using UnityEngine.Rendering;   // ← Needed for Volume

public class VIOnlyMode : MonoBehaviour
{
    [Header("Visual Impairment / Audio-Only Mode")]
    [SerializeField] private bool hideAllVisuals = true;
    [SerializeField] private bool hideUI = true;
    [SerializeField] private bool disablePostProcessing = true;
    [SerializeField] private bool disableShadows = true;

    private static bool hasRun = false;

    private void Awake()
    {
        if (hasRun) return;
        hasRun = true;

        // Allow command-line activation
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-viMode" || args[i] == "-audioOnly" || args[i] == "--vi-only")
            {
                hideAllVisuals = true;
                Debug.Log("[VIOnlyMode] Activated via command line");
                break;
            }
        }

        if (hideAllVisuals)
            ApplyVIMode();
    }

    private void ApplyVIMode()
    {
        Debug.Log("<color=orange>[VIOnlyMode] ACTIVATED – All visuals disabled</color>");

        // 1. Black out camera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            mainCam.cullingMask = 0; // Render nothing
        }

        // 2. Disable all renderers
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r != null) r.enabled = false;

        // 3. Disable UI canvases
        if (hideUI)
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c != null) c.enabled = false;
        }

        // 4. Stop particle systems
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer) renderer.enabled = false;
            }
        }

        // 5. Disable post-processing Volume
        if (disablePostProcessing)
        {
            var volumeComponent = Object.FindFirstObjectByType<Volume>();   // ← Fixed line
            if (volumeComponent != null)
                volumeComponent.enabled = false;
        }

        // 6. Disable shadows
        if (disableShadows)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null) l.shadows = LightShadows.None;
        }
    }

    // Runtime toggle from other scripts
    public static void EnableVIMode()
    {
        var instance = Object.FindFirstObjectByType<VIOnlyMode>();
        if (instance != null)
            instance.ApplyVIMode();
    }
}