using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer audioMixer;

    private bool isFading = false;
    private float fadeTargetVolume = 1f;
    private float fadeSpeed = 1f;

    [SerializeField] float bgmVolume = 1f;
    [SerializeField] float sfxVolume = 1f;

    private const string SFX_KEY = "SFXVolume";
    private const string BGM_KEY = "BGMVolume";
    private const string MASTER_KEY = "MasterVolume";
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

        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, DEFAULT_VOLUME);
        float bgmVolume = PlayerPrefs.GetFloat(BGM_KEY, DEFAULT_VOLUME);
        float masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, DEFAULT_VOLUME);

        audioMixer.SetFloat(AudioMixerParams.Sfx, sfxVolume);
        audioMixer.SetFloat(AudioMixerParams.Bgm, bgmVolume);
        audioMixer.SetFloat(AudioMixerParams.Master, masterVolume);

        Debug.Log($"[SoundManager] Volume settings loaded - SFX: {sfxVolume}, BGM: {bgmVolume}, Master: {masterVolume}");
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
        bgmSource.volume = bgmVolume;
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

        fadeTargetVolume = bgmVolume;    
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

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
