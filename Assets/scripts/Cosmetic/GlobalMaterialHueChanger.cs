using UnityEngine;
using UnityEngine.UI;

public class MaterialHueChanger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The material to change (will be instanced automatically)")]
    public Material targetMaterial;

    [Tooltip("The slider UI element")]
    public Slider hueSlider;

    [Header("Settings")]
    [Tooltip("The renderer that uses this material (optional - for single objects)")]
    public Renderer targetRenderer;

    private Material instancedMaterial;

    // ✅ SAVE THE CHOSEN HUE VALUE
    private float currentHue = 0f;

    void Start()
    {
        // Create an instance of the material so we don't affect all objects using it
        if (targetRenderer != null)
        {
            instancedMaterial = targetRenderer.material; // Automatically creates instance
        }
        else if (targetMaterial != null)
        {
            instancedMaterial = new Material(targetMaterial);
        }

        // Set material to pure red at start
        if (instancedMaterial != null)
        {
            instancedMaterial.color = Color.red; // Pure red = (1, 0, 0, 1)
            Debug.Log("Material set to pure red at start");
        }

        // Setup slider
        if (hueSlider != null)
        {
            hueSlider.minValue = 0f;
            hueSlider.maxValue = 1f;
            hueSlider.value = 0f; // 0 = red in HSV (hue starts at 0)

            // Add listener to detect slider changes
            hueSlider.onValueChanged.AddListener(OnHueChanged);
        }
        else
        {
            Debug.LogError("Hue Slider not assigned!");
        }
    }

    public void OnHueChanged(float hueValue)
    {
        // ✅ SAVE THE VALUE
        currentHue = hueValue;

        if (instancedMaterial != null)
        {
            // Get current color
            Color currentColor = instancedMaterial.color;

            // Convert to HSV
            Color.RGBToHSV(currentColor, out float h, out float s, out float v);

            // Change the hue
            h = hueValue;

            // Convert back to RGB
            Color newColor = Color.HSVToRGB(h, s, v);
            newColor.a = currentColor.a; // Preserve alpha

            // Apply new color
            instancedMaterial.color = newColor;

            Debug.Log($"Hue changed to: {hueValue} (Color: {newColor})");
        }

        // ✅ SAVE TO PLAYER PREFS SO IT PERSISTS ACROSS SCENES
        PlayerPrefs.SetFloat("PlayerHue", currentHue);
        PlayerPrefs.Save();
        Debug.Log($"✅ Saved player hue: {currentHue}");
    }

    // Optional: Call this to change hue from code
    public void SetHue(float hue)
    {
        if (hueSlider != null)
        {
            hueSlider.value = Mathf.Clamp01(hue);
        }
    }

    // ✅ PUBLIC METHOD TO GET CURRENT HUE
    public float GetCurrentHue()
    {
        return currentHue;
    }
}