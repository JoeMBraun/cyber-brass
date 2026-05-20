using UnityEngine;

namespace CyberBrass.UI
{
    /// <summary>
    /// Renders a clean, retro-futuristic Amber crosshair in the center of the screen
    /// using OnGUI. This provides a lightweight reticle for player aiming
    /// without requiring a Canvas or UI assets.
    /// </summary>
    public class Crosshair : MonoBehaviour
    {
        [Header("Crosshair Style")]
        [Tooltip("The color of the crosshair lines. Default is Amber CRT Glow (#FFB347).")]
        [SerializeField] private Color crosshairColor = new Color(1.0f, 0.7f, 0.18f, 0.85f);
        
        [Tooltip("Length of the four outer tick lines in pixels.")]
        [SerializeField] private int lineLength = 10;
        
        [Tooltip("Thickness of the crosshair lines in pixels.")]
        [SerializeField] private int thickness = 2;
        
        [Tooltip("Gap size between the center and the start of the outer ticks.")]
        [SerializeField] private int gap = 6;
        
        [Tooltip("Toggle the central aiming dot.")]
        [SerializeField] private bool showDot = true;
        
        [Tooltip("Size of the central aiming dot in pixels.")]
        [SerializeField] private int dotSize = 3;

        private Texture2D _whiteTexture;

        private void Awake()
        {
            // Create a 1x1 white texture programmatically to draw colored crosshair lines
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnGUI()
        {
            if (_whiteTexture == null) return;

            // Calculate center of screen dynamically (works on resize/different aspect ratios)
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            GUI.color = crosshairColor;

            // 1. Draw Center Dot
            if (showDot)
            {
                Rect dotRect = new Rect(
                    center.x - dotSize / 2f,
                    center.y - dotSize / 2f,
                    dotSize,
                    dotSize
                );
                GUI.DrawTexture(dotRect, _whiteTexture);
            }

            // 2. Draw Left Tick
            Rect leftRect = new Rect(
                center.x - gap - lineLength,
                center.y - thickness / 2f,
                lineLength,
                thickness
            );
            GUI.DrawTexture(leftRect, _whiteTexture);

            // 3. Draw Right Tick
            Rect rightRect = new Rect(
                center.x + gap,
                center.y - thickness / 2f,
                lineLength,
                thickness
            );
            GUI.DrawTexture(rightRect, _whiteTexture);

            // 4. Draw Top Tick
            Rect topRect = new Rect(
                center.x - thickness / 2f,
                center.y - gap - lineLength,
                thickness,
                lineLength
            );
            GUI.DrawTexture(topRect, _whiteTexture);

            // 5. Draw Bottom Tick
            Rect bottomRect = new Rect(
                center.x - thickness / 2f,
                center.y + gap,
                thickness,
                lineLength
            );
            GUI.DrawTexture(bottomRect, _whiteTexture);
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
            {
                Destroy(_whiteTexture);
            }
        }
    }
}
