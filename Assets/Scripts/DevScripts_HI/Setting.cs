using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Setting : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider voiceSlider;

    private const string SFX_KEY = "SFXVolume";
    private const string BGM_KEY = "BGMVolume";
    private const string MASTER_KEY = "MasterVolume";
    private const string VOICE_KEY = "VoiceVolume";

    private const float DEFAULT_VOLUME = 0.75f; // 기본값: 75%

    private void OnEnable()
    {
        Debug.Log($"[OnEnable] sfxSlider: {(sfxSlider != null ? sfxSlider.name : "null")}");
        Debug.Log($"[OnEnable] bgmSlider: {(bgmSlider != null ? bgmSlider.name : "null")}");
        Debug.Log($"[OnEnable] masterSlider: {(masterSlider != null ? masterSlider.name : "null")}");

        // 슬라이더 이벤트 리스너 등록
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (voiceSlider != null)
            voiceSlider.onValueChanged.AddListener(SetVoiceVolume);

        LoadVolumeSettings();
    }

    private void OnDisable()
    {
        // 슬라이더 이벤트 리스너 제거
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (voiceSlider != null)
            voiceSlider.onValueChanged.RemoveListener(SetVoiceVolume);
    }

    private void LoadVolumeSettings()
    {
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, DEFAULT_VOLUME);
        float bgmVolume = PlayerPrefs.GetFloat(BGM_KEY, DEFAULT_VOLUME);
        float masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, DEFAULT_VOLUME);
        float voiceVolume = PlayerPrefs.GetFloat(VOICE_KEY, DEFAULT_VOLUME);

        Debug.Log($"[LoadVolumeSettings] SFX: {sfxVolume}, BGM: {bgmVolume}, Master: {masterVolume}, Voice: {voiceVolume}");

        // 이벤트 발생 없이 슬라이더 값만 설정
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgmVolume);
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(masterVolume);
        if (voiceSlider != null)
            voiceSlider.SetValueWithoutNotify(voiceVolume);

        // 오디오 믹서에 실제 볼륨 적용
        ApplyVolumeToMixer(AudioMixerParams.Sfx, sfxVolume);
        ApplyVolumeToMixer(AudioMixerParams.Bgm, bgmVolume);
        ApplyVolumeToMixer(AudioMixerParams.Master, masterVolume);
        ApplyVolumeToMixer(AudioMixerParams.Voice, voiceVolume);
    }

    public void SetSFXVolume(float volume)
    {
        Debug.Log($"[SetSFXVolume] Called with volume: {volume}");
        PlayerPrefs.SetFloat(SFX_KEY, volume);
        PlayerPrefs.Save();
        ApplyVolumeToMixer(AudioMixerParams.Sfx, volume);
    }

    public void SetBGMVolume(float volume)
    {
        Debug.Log($"[SetBGMVolume] Called with volume: {volume}");
        PlayerPrefs.SetFloat(BGM_KEY, volume);
        PlayerPrefs.Save();
        ApplyVolumeToMixer(AudioMixerParams.Bgm, volume);
    }

    public void SetMasterVolume(float volume)
    {
        Debug.Log($"[SetMasterVolume] Called with volume: {volume}");
        PlayerPrefs.SetFloat(MASTER_KEY, volume);
        PlayerPrefs.Save();
        ApplyVolumeToMixer(AudioMixerParams.Master, volume);
    }

    public void SetVoiceVolume(float volume)
    {
        Debug.Log($"[SetVoiceVolume] Called with volume: {volume}");
        PlayerPrefs.SetFloat(VOICE_KEY, volume);
        PlayerPrefs.Save();
        ApplyVolumeToMixer(AudioMixerParams.Voice, volume);
    }

    private void ApplyVolumeToMixer(string parameterName, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[ApplyVolumeToMixer] AudioMixer is null!");
            return;
        }

        // 0~1 범위를 dB로 변환 (-80dB ~ 0dB)
        float dB = LinearToDecibel(volume);
        audioMixer.SetFloat(parameterName, dB);

        Debug.Log($"[ApplyVolumeToMixer] {parameterName} = {volume} (linear) -> {dB} dB");
    }

    /// <summary>
    /// 0~1 선형 볼륨을 데시벨로 변환 (-80dB ~ 0dB)
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        // 0일 때 -80dB (거의 무음), 1일 때 0dB (최대)
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
}
