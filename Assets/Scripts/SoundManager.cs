using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;

    private bool isFading = false;
    private float fadeTargetVolume = 1f;
    private float fadeSpeed = 1f;

    // AudioSource 볼륨은 1.0 고정 (AudioMixer에서 볼륨 조절)
    private const float SOURCE_VOLUME = 1f;

    private const string SFX_KEY = "SFXVolume";
    private const string BGM_KEY = "BGMVolume";
    private const string MASTER_KEY = "MasterVolume";
    private const string VOICE_KEY = "VoiceVolume";
    private const float DEFAULT_VOLUME = 0.75f;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // voiceSource에 Voice 믹서 그룹 할당
        if (voiceSource != null && voiceMixerGroup != null)
        {
            voiceSource.outputAudioMixerGroup = voiceMixerGroup;
        }

        // 게임 시작 시 저장된 볼륨 설정 로드
        LoadVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[SoundManager] AudioMixer is not assigned!");
            return;
        }

        float sfxVol = PlayerPrefs.GetFloat(SFX_KEY, DEFAULT_VOLUME);
        float bgmVol = PlayerPrefs.GetFloat(BGM_KEY, DEFAULT_VOLUME);
        float masterVol = PlayerPrefs.GetFloat(MASTER_KEY, DEFAULT_VOLUME);
        float voiceVol = PlayerPrefs.GetFloat(VOICE_KEY, DEFAULT_VOLUME);

        // 0~1 범위를 dB로 변환하여 AudioMixer에 적용
        audioMixer.SetFloat(AudioMixerParams.Sfx, LinearToDecibel(sfxVol));
        audioMixer.SetFloat(AudioMixerParams.Bgm, LinearToDecibel(bgmVol));
        audioMixer.SetFloat(AudioMixerParams.Master, LinearToDecibel(masterVol));
        audioMixer.SetFloat(AudioMixerParams.Voice, LinearToDecibel(voiceVol));

        Debug.Log($"[SoundManager] Volume settings loaded - SFX: {sfxVol}, BGM: {bgmVol}, Master: {masterVol}, Voice: {voiceVol}");
    }

    /// <summary>
    /// 0~1 선형 볼륨을 데시벨로 변환 (-80dB ~ 0dB)
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    private void Update()
    {
        Fade();
    }

    private void Fade()
    {
        if (!isFading)
        {
            return;
        }

        bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, fadeTargetVolume, fadeSpeed * Time.deltaTime);

        if (Mathf.Approximately(bgmSource.volume, fadeTargetVolume))
        {
            isFading = false;

            if (fadeTargetVolume == 0f)
            {
                bgmSource.Stop();
            }
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = SOURCE_VOLUME;
        bgmSource.Play();
    }
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlayBGMWithFadeIn(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;

        bgmSource.volume = 0f;
        bgmSource.Play();

        fadeTargetVolume = SOURCE_VOLUME;
        fadeSpeed = 1f / fadeTime;
        isFading = true;
    }

    public void StopBGMwithFadeOut(float fadeTime = 1f)
    {
        fadeTargetVolume = 0f;
        fadeSpeed = 1f / fadeTime;
        isFading = true;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, SOURCE_VOLUME);
    }

    /// <summary>
    /// 볼륨 배율을 지정하여 효과음 재생
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volumeScale">볼륨 배율 (1.0 = 기본, 1.5 = 150%)</param>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, SOURCE_VOLUME * volumeScale);
    }

    /// <summary>
    /// 보이스 재생 (이전 보이스 중단 후 새 보이스 재생)
    /// </summary>
    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = SOURCE_VOLUME;
        voiceSource.Play();
    }

    /// <summary>
    /// 볼륨 배율을 지정하여 보이스 재생
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volumeScale">볼륨 배율 (1.0 = 기본, 2.0 = 200%)</param>
    public void PlayVoice(AudioClip clip, float volumeScale)
    {
        if (clip == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.PlayOneShot(clip, SOURCE_VOLUME * volumeScale);
    }

    /// <summary>
    /// 현재 재생 중인 보이스 정지
    /// </summary>
    public void StopVoice()
    {
        voiceSource.Stop();
    }
}
