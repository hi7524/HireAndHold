using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    private bool isFading = false;
    private float fadeTargetVolume = 1f;
    private float fadeSpeed = 1f;

    [SerializeField] float bgmVolume = 1f;
    [SerializeField] float sfxVolume = 1f;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(Instance);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  
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
