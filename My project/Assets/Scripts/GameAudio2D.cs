using UnityEngine;

/// <summary>
/// Sistema de áudio 100% procedural (Fase 5 — Polimento e "Juice").
/// Sintetiza em memória efeitos sonoros suaves (pulo, pouso, laser, pisão, moeda, interruptor, mola, dano, vitória)
/// e uma trilha ambiente melódica em loop, com volume ajustável persistido em PlayerPrefs.
/// </summary>
public class GameAudio2D : MonoBehaviour
{
    private static GameAudio2D instance;

    [Header("Volume Geral (0 a 1)")]
    [Range(0f, 1f)]
    public float masterVolume = 0.65f;
    [Range(0f, 1f)]
    public float musicVolume = 0.28f;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private int currentMusicTheme = -1;

    private AudioClip jumpClip;
    private AudioClip landClip;
    private AudioClip laserClip;
    private AudioClip stompClip;
    private AudioClip coinClip;
    private AudioClip switchClip;
    private AudioClip springClip;
    private AudioClip breakClip;
    private AudioClip hurtClip;
    private AudioClip winClip;
    private AudioClip nightMusicClip;
    private AudioClip dayMusicClip;

    private const string VolumePrefKey = "GameAudio2D_MasterVolume";

    public static GameAudio2D Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameAudio2D>();
                if (instance == null && Application.isPlaying)
                {
                    GameObject obj = new GameObject("GameAudio2D");
                    instance = obj.AddComponent<GameAudio2D>();
                }
            }
            return instance;
        }
    }

    public static float MasterVolume
    {
        get => Instance != null ? Instance.masterVolume : 0.65f;
        set
        {
            if (Instance != null)
            {
                Instance.SetMasterVolume(value);
            }
        }
    }

    public static void AdjustVolume(float delta)
    {
        MasterVolume = Mathf.Clamp01(MasterVolume + delta);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (PlayerPrefs.HasKey(VolumePrefKey))
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, 0.65f));
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        BuildAllClips();
        UpdateSourceVolumes();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePrefKey, masterVolume);
        UpdateSourceVolumes();
    }

    private void UpdateSourceVolumes()
    {
        if (sfxSource != null) sfxSource.volume = masterVolume;
        if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
    }

    public static void EnsureMusicTheme(int stageNumber)
    {
        if (!Application.isPlaying) return;
        GameAudio2D audio = Instance;
        if (audio == null || audio.musicSource == null) return;

        int desiredTheme = (stageNumber == 2) ? 2 : 1;
        if (audio.currentMusicTheme != desiredTheme || !audio.musicSource.isPlaying)
        {
            audio.currentMusicTheme = desiredTheme;
            audio.musicSource.clip = (desiredTheme == 2) ? audio.dayMusicClip : audio.nightMusicClip;
            audio.UpdateSourceVolumes();
            audio.musicSource.Play();
        }
    }

    public static void PlayJump() => PlayOneShot(Instance?.jumpClip, 0.55f);
    public static void PlayLand() => PlayOneShot(Instance?.landClip, 0.35f);
    public static void PlayLaser() => PlayOneShot(Instance?.laserClip, 0.50f);
    public static void PlayStomp() => PlayOneShot(Instance?.stompClip, 0.68f);
    public static void PlayCoin() => PlayOneShot(Instance?.coinClip, 0.62f);
    public static void PlaySwitch() => PlayOneShot(Instance?.switchClip, 0.65f);
    public static void PlaySpring() => PlayOneShot(Instance?.springClip, 0.65f);
    public static void PlayBreak() => PlayOneShot(Instance?.breakClip, 0.65f);
    public static void PlayHurt() => PlayOneShot(Instance?.hurtClip, 0.75f);
    public static void PlayWin() => PlayOneShot(Instance?.winClip, 0.75f);

    private static void PlayOneShot(AudioClip clip, float scale)
    {
        if (!Application.isPlaying || clip == null) return;
        GameAudio2D audio = Instance;
        if (audio != null && audio.sfxSource != null && audio.masterVolume > 0.001f)
        {
            audio.sfxSource.PlayOneShot(clip, scale * audio.masterVolume);
        }
    }

    private void BuildAllClips()
    {
        jumpClip = CreateSweepClip("SFX_Jump", 0.14f, 240f, 540f, isTriangle: true);
        landClip = CreateSweepClip("SFX_Land", 0.07f, 140f, 75f, isTriangle: true);
        laserClip = CreateSweepClip("SFX_Laser", 0.12f, 880f, 260f, isTriangle: false);
        stompClip = CreateSweepClip("SFX_Stomp", 0.16f, 180f, 420f, isTriangle: true);
        coinClip = CreateTwoNoteClip("SFX_Coin", 0.18f, 587.33f, 880.0f);
        switchClip = CreateTwoNoteClip("SFX_Switch", 0.22f, 440.0f, 659.25f);
        springClip = CreateSweepClip("SFX_Spring", 0.22f, 210f, 760f, isTriangle: true);
        breakClip = CreateNoiseBurstClip("SFX_Break", 0.16f, 220f);
        hurtClip = CreateSweepClip("SFX_Hurt", 0.24f, 310f, 110f, isTriangle: false);
        winClip = CreateArpeggioClip("SFX_Win", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.11f);

        nightMusicClip = CreateAmbientLoopClip(
            "BGM_NightRuins",
            new[] { 220.0f, 261.63f, 329.63f, 392.0f, 329.63f, 293.66f, 246.94f, 196.0f },
            noteDuration: 0.45f
        );
        dayMusicClip = CreateAmbientLoopClip(
            "BGM_DayMeadow",
            new[] { 261.63f, 329.63f, 392.0f, 523.25f, 440.0f, 392.0f, 329.63f, 293.66f },
            noteDuration: 0.38f
        );
    }

    private static AudioClip CreateSweepClip(string name, float duration, float startFreq, float endFreq, bool isTriangle)
    {
        const int sampleRate = 22050;
        int count = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float env = Mathf.Sin(t * Mathf.PI);
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += freq / sampleRate;
            float frac = phase - Mathf.Floor(phase);
            float wave = isTriangle
                ? (Mathf.Abs(frac * 4f - 2f) - 1f)
                : Mathf.Sin(frac * Mathf.PI * 2f) * 0.7f + (frac < 0.5f ? 0.3f : -0.3f);
            data[i] = wave * env * 0.45f;
        }

        AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateTwoNoteClip(string name, float duration, float freqA, float freqB)
    {
        const int sampleRate = 22050;
        int count = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float freq = (t < 0.42f) ? freqA : freqB;
            float env = 1f - t * 0.85f;
            phase += freq / sampleRate;
            data[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * 0.45f;
        }

        AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateNoiseBurstClip(string name, float duration, float baseTone)
    {
        const int sampleRate = 22050;
        int count = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
        float[] data = new float[count];
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float env = (1f - t) * (1f - t);
            phase += (baseTone * (1f - t * 0.4f)) / sampleRate;
            float noise = PixelArtBuilder.Hash01(i, 77, 199) * 2f - 1f;
            float tone = Mathf.Sin(phase * Mathf.PI * 2f);
            data[i] = (noise * 0.55f + tone * 0.45f) * env * 0.45f;
        }

        AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateArpeggioClip(string name, float[] notes, float stepDuration)
    {
        const int sampleRate = 22050;
        int stepSamples = Mathf.Max(64, Mathf.RoundToInt(stepDuration * sampleRate));
        int totalSamples = stepSamples * notes.Length;
        float[] data = new float[totalSamples];
        float phase = 0f;

        for (int i = 0; i < totalSamples; i++)
        {
            int noteIdx = Mathf.Min(notes.Length - 1, i / stepSamples);
            float localT = (i % stepSamples) / (float)stepSamples;
            float env = 1f - localT * 0.65f;
            phase += notes[noteIdx] / sampleRate;
            data[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * 0.42f;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateAmbientLoopClip(string name, float[] notes, float noteDuration)
    {
        const int sampleRate = 22050;
        int stepSamples = Mathf.Max(256, Mathf.RoundToInt(noteDuration * sampleRate));
        int totalSamples = stepSamples * notes.Length;
        float[] data = new float[totalSamples];
        float phase = 0f;
        float subPhase = 0f;

        for (int i = 0; i < totalSamples; i++)
        {
            int noteIdx = (i / stepSamples) % notes.Length;
            float localT = (i % stepSamples) / (float)stepSamples;
            float env = Mathf.Sin(localT * Mathf.PI);
            float freq = notes[noteIdx];

            phase += freq / sampleRate;
            subPhase += (freq * 0.5f) / sampleRate;

            float lead = Mathf.Sin(phase * Mathf.PI * 2f) * 0.22f;
            float bass = Mathf.Sin(subPhase * Mathf.PI * 2f) * 0.16f;
            data[i] = (lead * env) + (bass * 0.75f);
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
