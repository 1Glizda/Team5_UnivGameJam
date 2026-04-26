using UnityEngine;

public enum SoundType
{
    VALVE,
    SMOKE,
    STEPS,
    PLANK
}

public enum MusicType
{
    BACKGROUNDMUSIC
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private AudioClip pausedClip;
    private float pausedTime;

    [Header("---------- Audio Source ----------")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioClip[] soundList;
    [SerializeField] private AudioClip[] musicList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic(MusicType.BACKGROUNDMUSIC);
    }

    public static void PlaySound(SoundType sound, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null) return;

        if (Instance.soundList.Length > (int)sound)
        {
            Instance.sfxSource.pitch = pitch;
            Instance.sfxSource.PlayOneShot(
                Instance.soundList[(int)sound],
                volume
            );
        }
        else
        {
            Debug.LogWarning("Sound clip not found for: " + sound);
        }
    }

    public static void PlayMusic(MusicType music)
    {
        if (Instance == null) return;

        if (Instance.musicList.Length > (int)music)
        {
            Instance.musicSource.clip = Instance.musicList[(int)music];
            Instance.musicSource.volume = 1f;
            Instance.musicSource.loop = true;
            Instance.musicSource.Play();
        }
        else
        {
            Debug.LogWarning("Music clip not found for: " + music);
        }
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            pausedClip = musicSource.clip;
            pausedTime = musicSource.time;
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (pausedClip != null)
        {
            musicSource.clip = pausedClip;
            musicSource.time = pausedTime;
            musicSource.Play();
            pausedClip = null;
        }
    }
}