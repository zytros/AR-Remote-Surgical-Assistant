using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Handler : MonoBehaviour
{

    public RawImage mainVideoStream;               
    public RawImage smallVideoStream;  
    public Button pauseButton;
    public Button backButton;

    private WebCamTexture webCamTexture;
    private Texture2D pausedFrameTexture;
    private List<Texture2D> backTextures = new List<Texture2D>();
    private bool isPaused = false;

    public Color drawColor = Color.red;
    public int brushSize = 1;
    public float maxDistance = 1f;

    private Vector2? lastMousePos = null;
    private Color[] colorBuffer;

    // Start is called before the first frame update
    void Start()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            WebCamDevice device = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(device.name);

            mainVideoStream.texture = webCamTexture;
            smallVideoStream.texture = webCamTexture;

            webCamTexture.Play();

            pauseButton.onClick.AddListener(TogglePause);
            backButton.onClick.AddListener(GoBack);
        }
        else
        {
            Debug.LogWarning("No webcam detected.");
        }
    }
    
    void GoBack()
    {
        if (backTextures.Count == 0)
        {
            return;
        }
        int idx = backTextures.Count - 1;
        Texture2D backTexture = backTextures[idx];
        backTextures.RemoveAt(idx);
        pausedFrameTexture.SetPixels(backTexture.GetPixels());
        pausedFrameTexture.Apply();
        colorBuffer = pausedFrameTexture.GetPixels();
    }

    void TogglePause()
    {
        if (!isPaused)
        {
            // Capture the current frame from the live feed as a paused frame
            pausedFrameTexture = new Texture2D(webCamTexture.width, webCamTexture.height);
            pausedFrameTexture.SetPixels(webCamTexture.GetPixels());
            pausedFrameTexture.Apply();

            // Display the paused frame on the paused frame RawImage
            mainVideoStream.texture = pausedFrameTexture;
            smallVideoStream.texture = webCamTexture;

            // Hide the main RawImage (live feed) and show paused frame with smaller live stream
            colorBuffer = pausedFrameTexture.GetPixels();
        }
        else
        {
            // Switch back to the live feed on the main RawImage
            mainVideoStream.texture = webCamTexture;
            smallVideoStream.texture = pausedFrameTexture;
        }

        // Toggle the paused state
        isPaused = !isPaused;
    }

    void OnDestroy()
    {
        // Release webcam when the script is destroyed
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }
        else if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
        if (isPaused)
        {
            if (Input.GetMouseButton(0) && RectTransformUtility.RectangleContainsScreenPoint(mainVideoStream.rectTransform, Input.mousePosition, Camera.main))
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mainVideoStream.rectTransform, Input.mousePosition, Camera.main, out localPoint);

                Rect rect = mainVideoStream.rectTransform.rect;
                float normalizedX = (localPoint.x - rect.x) / rect.width;
                float normalizedY = (localPoint.y - rect.y) / rect.height;
                int x = Mathf.Clamp((int)(normalizedX * pausedFrameTexture.width), 0, pausedFrameTexture.width - 1);
                int y = Mathf.Clamp((int)(normalizedY * pausedFrameTexture.height), 0, pausedFrameTexture.height - 1);

                Vector2 currentMousePos = new Vector2(x, y);


                if (lastMousePos.HasValue)
                {
                    // Interpolate between last and current positions to fill in gaps
                    DrawLine(lastMousePos.Value, currentMousePos);
                }
                else
                {
                    if (backTextures.Count > 5)
                    {
                        backTextures.RemoveAt(0);
                    }
                    Texture2D old_Texture = new Texture2D(webCamTexture.width, webCamTexture.height);
                    old_Texture.SetPixels(pausedFrameTexture.GetPixels());
                    Debug.Log("Adding image to backImages");
                    backTextures.Add(old_Texture);
                    DrawOnTexture(x, y);
                }

                // Update the last position
                lastMousePos = currentMousePos;
                pausedFrameTexture.SetPixels(colorBuffer);
                pausedFrameTexture.Apply();
            }
            else
            {
                lastMousePos = null;
            }
        }
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;

        // Place points along the line based on maxDistance
        for (float i = 0; i < distance; i += maxDistance)
        {
            Vector2 point = start + direction * i;
            DrawOnTexture((int)point.x, (int)point.y);
        }
        // Ensure the endpoint is drawn
        DrawOnTexture((int)end.x, (int)end.y);

        // Apply buffered changes to the texture all at once
        pausedFrameTexture.SetPixels(colorBuffer);
        pausedFrameTexture.Apply();
    }

    private void DrawOnTexture(int x, int y)
    {
        // Set pixels within the brush size at the given (x, y) position in the buffer
        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int px = Mathf.Clamp(x + i, 0, pausedFrameTexture.width - 1);
                int py = Mathf.Clamp(y + j, 0, pausedFrameTexture.height - 1);

                int bufferIndex = px + py * pausedFrameTexture.width;
                colorBuffer[bufferIndex] = drawColor;
            }
        }
    }
}