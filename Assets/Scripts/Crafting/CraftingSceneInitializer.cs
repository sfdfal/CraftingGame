using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace CraftingGame.Crafting
{
    /// <summary>
    /// Automatically prepares the scene for the crafting UI after each load.
    /// </summary>
    public static class CraftingSceneInitializer
    {
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(320, 180);
        private const int PixelsPerUnit = 16;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("CraftingSceneInitializer: Kein Main Camera Objekt gefunden.");
                return;
            }

            ConfigureCamera(camera);
            EnsureEventSystem();
            EnsureCanvas();
        }

        private static void ConfigureCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = ReferenceResolution.y * 0.5f / PixelsPerUnit;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 20, 30, 255);

            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();
            }

            pixelPerfect.assetsPPU = PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceResolution.x;
            pixelPerfect.refResolutionY = ReferenceResolution.y;
            pixelPerfect.gridSnapping = true;
            pixelPerfect.upscaleRT = false;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        private static void EnsureCanvas()
        {
            if (Object.FindObjectOfType<CraftingUIManager>() != null)
            {
                return;
            }

            var canvasGo = new GameObject("CraftingCanvas");
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                canvasGo.layer = uiLayer;
            }
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = PixelsPerUnit;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<CraftingUIManager>();
        }
    }
}
