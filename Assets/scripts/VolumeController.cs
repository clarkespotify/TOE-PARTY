using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeController : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;

    void Start()
    {
        if (volumeSlider != null)
        {
            // Connect the slider to the function in code
            volumeSlider.onValueChanged.AddListener(SetVolume);

            // Load and apply saved volume
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            volumeSlider.value = savedVolume;
        }
    }

    public void SetVolume(float volume)
    {
        if (audioMixer != null)
        {
            volume = Mathf.Clamp(volume, 0.0001f, 1f);
            float dB = Mathf.Log10(volume) * 20;
            audioMixer.SetFloat("MasterVolume", dB);
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }
    }
}