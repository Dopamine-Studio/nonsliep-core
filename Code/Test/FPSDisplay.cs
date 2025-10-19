using UnityEngine;

namespace Nonsliep.Test
{
    /// <summary>
    /// Displays the current Frames Per Second (FPS) on the screen.
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        private float deltaTime = 0.0f;

        void Update()
        {
            // Calculate delta time for FPS calculation
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            // Set up GUI style and size
            int width = Screen.width, height = Screen.height;
            GUIStyle style = new GUIStyle();

            // Set font size to half of the original size
            style.fontSize = height / 50;
            style.alignment = TextAnchor.LowerLeft; // Align text to the bottom-left
            style.normal.textColor = Color.black; // Set text color to black

            // Calculate FPS
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

            // Add some padding on the left and bottom
            float padding = 10.0f; // Padding in pixels
            Rect rect = new Rect(padding, height - height / 50 - padding, width, height / 100);

            // Draw FPS text
            GUI.Label(rect, text, style);
        }
    }
}
