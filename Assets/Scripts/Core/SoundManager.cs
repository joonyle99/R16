using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections.Generic;

public enum BgmType
{
    OutGame = 0,
    InGame = 10,
}

public enum SfxType
{
    AimDrag = 0,
    LockOn = 1,
    Shoot = 2,
    NoCharge = 3,
    Attack = 4,
    Bounce = 5,
    Damaged = 6,
    Energy = 7,
    Jump = 8,
    Rush = 9,
}

[System.Serializable]
public struct BgmEntry
{
    public BgmType type;
    public AudioClip clip;
}

[System.Serializable]
public struct SfxEntry
{
    public SfxType type;
    public AudioClip clip;
}

public class SoundManager : Singleton<SoundManager>, IManager, IGameStateListener<OutGameState>, IGameStateListener<InGameState>
{
    public int Priority => 10;

    [SerializeField] private BgmEntry[] _bgmEntries;
    [SerializeField] private SfxEntry[] _sfxEntries;

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Space]
    
    [SerializeField] private float _bgmFadeDuration = 0.35f;

    private Dictionary<BgmType, BgmEntry> _bgmMap;
    private Dictionary<SfxType, SfxEntry> _sfxMap;

    private float _bgmVolume;
    private Tween _bgmFadeTween;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        _bgmFadeTween?.Kill();
    }

    public void Initialize()
    {
        _bgmMap = new Dictionary<BgmType, BgmEntry>();
        foreach (var entry in _bgmEntries)
            _bgmMap.TryAdd(entry.type, entry);

        _sfxMap = new Dictionary<SfxType, SfxEntry>();
        foreach (var entry in _sfxEntries)
            _sfxMap.TryAdd(entry.type, entry);

        _bgmVolume = _bgmSource.volume;
        
        if (_bgmMap.TryGetValue(BgmType.OutGame, out var outgameEntry) && outgameEntry.clip != null) outgameEntry.clip.LoadAudioData();
        if (_bgmMap.TryGetValue(BgmType.InGame, out var inGameEntry) && inGameEntry.clip != null) inGameEntry.clip.LoadAudioData();
    }

    public void OnStateChanged(OutGameState prevState, OutGameState currState)
    {
        if (currState != OutGameState.None)
        {
            _bgmSource.volume = _bgmVolume;
            if (_bgmMap.TryGetValue(BgmType.OutGame, out var entry) && entry.clip != null) _bgmSource.clip = entry.clip;
            if (!_bgmSource.isPlaying) _bgmSource.Play();
        }
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        if (currState == InGameState.Play)
        {
            _bgmSource.volume = _bgmVolume;
            if (_bgmMap.TryGetValue(BgmType.InGame, out var entry) && entry.clip != null) _bgmSource.clip = entry.clip;
            if (!_bgmSource.isPlaying) _bgmSource.Play();
        }
        else
        {
            _bgmSource.Stop();
        }
    }

    public void PlayBgm(BgmType type, float volume = -1f)
    {
        if (!_bgmMap.TryGetValue(type, out var entry) || entry.clip == null) return;
        if (_bgmSource.clip == entry.clip) return;

        if (volume >= 0f) _bgmVolume = volume;

        FadeBgmTo(0f);
        _bgmFadeTween.OnComplete(() =>
        {
            _bgmSource.clip = entry.clip;
            _bgmSource.Play();
            FadeBgmTo(_bgmVolume);
        });
    }

    public void StopBgm()
    {
        FadeBgmTo(0f);
        _bgmFadeTween.OnComplete(() =>
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        });
    }

    public void PlaySfx(SfxType type, float volume = 0.5f)
    {
        if (_sfxMap.TryGetValue(type, out var entry) && entry.clip != null)
        {
            _sfxSource.PlayOneShot(entry.clip, volume);
        }
    }

    private void FadeBgmTo(float targetVolume)
    {
        _bgmFadeTween?.Kill();
        _bgmFadeTween = _bgmSource.DOFade(targetVolume, _bgmFadeDuration).SetUpdate(true);
    }

    private void SetSfxPaused(bool paused)
    {
        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in sources)
        {
            if (source == _bgmSource) continue;
            if (paused) source.Pause();
            else source.UnPause();
        }
    }
}
