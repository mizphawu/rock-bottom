using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuManager : MonoBehaviour
{
    [Header("Sliders & Toggle")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;
    public GameObject toggleOffVisual;

    [Header("Live Badge Texts (TextMeshPro)")]
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    public TMP_Text sensitivityValueText;

    [Header("Audio Sources (Optional)")]
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;

    private void Start()
    {
        LoadSavedSettings();
    }

    public void LoadSavedSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.85f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.90f);
        float sens = PlayerPrefs.GetFloat("MouseSensitivity", 1.4f);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (musicVolumeSlider != null) musicVolumeSlider.value = music;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;
        if (sensitivitySlider != null) sensitivitySlider.value = sens;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
        SetMouseSensitivity(sens);
        SetFullscreen(isFullscreen);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (masterValueText != null)
            masterValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetMusicVolume(float value)
    {
        if (musicAudioSource != null) musicAudioSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetSFXVolume(float value)
    {
        if (sfxAudioSource != null) sfxAudioSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("0.0") + "x";
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        if (toggleOffVisual != null)
            toggleOffVisual.SetActive(!isFullscreen);
    }

    public void ResetDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 0.85f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.75f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.90f;
        if (sensitivitySlider != null) sensitivitySlider.value = 1.4f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
    }

    public void ApplyChanges()
    {
        PlayerPrefs.Save();
    }
}