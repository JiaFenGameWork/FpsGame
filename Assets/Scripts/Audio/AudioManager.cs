using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音效管理器 - 支持音效淡入淡出、混合、3D空间音效
/// 单例模式，全局访问
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试在场景中查找
                _instance = FindObjectOfType<AudioManager>();
                
                // 如果没有，自动创建
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("通道池设置")]
    [SerializeField] private int initialChannelCount = 16;    // 初始通道数量
    [SerializeField] private int maxChannelCount = 32;        // 最大通道数量

    [Header("全局音量设置")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;         // 主音量
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;            // 音效音量
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;          // 音乐音量

    // 通道池
    private List<AudioChannel> _channelPool = new List<AudioChannel>();
    private Transform _channelContainer;
    
    // 音乐通道（专用于背景音乐）
    private AudioChannel _musicChannel;
    
    // 音效缓存（可选，用于预加载）
    private Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    /// <summary>
    /// 主音量
    /// </summary>
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateAllVolumes();
        }
    }

    /// <summary>
    /// 音效音量
    /// </summary>
    public float SFXVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// 音乐音量
    /// </summary>
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (_musicChannel != null && _musicChannel.IsPlaying)
            {
                _musicChannel.SetVolume(musicVolume * masterVolume, 0.1f);
            }
        }
    }

    private void Awake()
    {
        // 单例检查
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeChannelPool();
    }

    private void InitializeChannelPool()
    {
        // 创建通道容器
        _channelContainer = new GameObject("AudioChannels").transform;
        _channelContainer.SetParent(transform);
        
        // 预创建通道
        for (int i = 0; i < initialChannelCount; i++)
        {
            CreateNewChannel();
        }
        
        // 创建音乐专用通道
        GameObject musicGO = new GameObject("MusicChannel");
        musicGO.transform.SetParent(transform);
        _musicChannel = musicGO.AddComponent<AudioChannel>();
    }

    private AudioChannel CreateNewChannel()
    {
        GameObject channelGO = new GameObject($"Channel_{_channelPool.Count}");
        channelGO.transform.SetParent(_channelContainer);
        AudioChannel channel = channelGO.AddComponent<AudioChannel>();
        _channelPool.Add(channel);
        return channel;
    }

    private AudioChannel GetAvailableChannel()
    {
        // 查找空闲通道
        foreach (var channel in _channelPool)
        {
            if (channel.IsIdle)
                return channel;
        }
        
        // 如果没有空闲通道，尝试创建新通道
        if (_channelPool.Count < maxChannelCount)
        {
            return CreateNewChannel();
        }
        
        // 如果达到上限，复用最早的通道（强制停止）
        AudioChannel oldestChannel = _channelPool[0];
        oldestChannel.Stop();
        return oldestChannel;
    }

    #region 音效播放 API

    /// <summary>
    /// 播放2D音效（UI、全局音效）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">音量（0-1）</param>
    /// <param name="pitchVariation">随机音调变化</param>
    /// <returns>音频通道（可用于后续控制）</returns>
    public AudioChannel PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null) return null;
        
        AudioChannel channel = GetAvailableChannel();
        float finalVolume = volume * sfxVolume * masterVolume;
        channel.PlayOneShot(clip, finalVolume, pitchVariation);
        return channel;
    }

    /// <summary>
    /// 在指定位置播放3D音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="position">世界坐标位置</param>
    /// <param name="volume">音量</param>
    /// <param name="spatialBlend">空间混合（0=2D, 1=完全3D）</param>
    /// <param name="pitchVariation">随机音调变化</param>
    /// <returns>音频通道</returns>
    public AudioChannel PlaySFXAtPosition(AudioClip clip, Vector3 position, 
                                          float volume = 1f, float spatialBlend = 1f,
                                          float pitchVariation = 0f)
    {
        if (clip == null) return null;
        
        AudioChannel channel = GetAvailableChannel();
        channel.SetPosition(position);
        channel.Source.spatialBlend = spatialBlend;
        channel.Source.rolloffMode = AudioRolloffMode.Linear;
        channel.Source.minDistance = 1f;
        channel.Source.maxDistance = 50f;
        
        float finalVolume = volume * sfxVolume * masterVolume;
        channel.PlayOneShot(clip, finalVolume, pitchVariation);
        return channel;
    }

    /// <summary>
    /// 播放带淡入的音效
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">目标音量</param>
    /// <param name="fadeInDuration">淡入时间</param>
    /// <param name="loop">是否循环</param>
    /// <returns>音频通道</returns>
    public AudioChannel PlaySFXWithFadeIn(AudioClip clip, float volume = 1f, 
                                          float fadeInDuration = 0.5f, bool loop = false)
    {
        if (clip == null) return null;
        
        AudioChannel channel = GetAvailableChannel();
        float finalVolume = volume * sfxVolume * masterVolume;
        channel.Play(clip, finalVolume, fadeInDuration, 1f, loop);
        return channel;
    }

    /// <summary>
    /// 播放循环音效（如引擎声、风声）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">音量</param>
    /// <param name="fadeInDuration">淡入时间</param>
    /// <returns>音频通道（保存引用以便后续停止）</returns>
    public AudioChannel PlayLoopingSFX(AudioClip clip, float volume = 1f, float fadeInDuration = 0.3f)
    {
        if (clip == null) return null;
        
        AudioChannel channel = GetAvailableChannel();
        float finalVolume = volume * sfxVolume * masterVolume;
        channel.Play(clip, finalVolume, fadeInDuration, 1f, true);
        return channel;
    }

    /// <summary>
    /// 停止循环音效（带淡出）
    /// </summary>
    /// <param name="channel">要停止的通道</param>
    /// <param name="fadeOutDuration">淡出时间</param>
    public void StopLoopingSFX(AudioChannel channel, float fadeOutDuration = 0.3f)
    {
        if (channel != null)
        {
            channel.Stop(fadeOutDuration);
        }
    }

    #endregion

    #region 背景音乐 API

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="clip">音乐剪辑</param>
    /// <param name="volume">音量</param>
    /// <param name="fadeInDuration">淡入时间</param>
    public void PlayMusic(AudioClip clip, float volume = 1f, float fadeInDuration = 1f)
    {
        if (clip == null) return;
        
        float finalVolume = volume * musicVolume * masterVolume;
        _musicChannel.Play(clip, finalVolume, fadeInDuration, 1f, true);
    }

    /// <summary>
    /// 交叉淡入淡出切换音乐
    /// </summary>
    /// <param name="newClip">新音乐</param>
    /// <param name="volume">音量</param>
    /// <param name="crossfadeDuration">交叉淡入淡出时间</param>
    public void CrossfadeMusic(AudioClip newClip, float volume = 1f, float crossfadeDuration = 1f)
    {
        if (newClip == null) return;
        
        float finalVolume = volume * musicVolume * masterVolume;
        _musicChannel.CrossfadeTo(newClip, finalVolume, crossfadeDuration, true);
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    /// <param name="fadeOutDuration">淡出时间</param>
    public void StopMusic(float fadeOutDuration = 1f)
    {
        _musicChannel.Stop(fadeOutDuration);
    }

    /// <summary>
    /// 暂停/恢复音乐
    /// </summary>
    public void PauseMusic(bool pause)
    {
        if (pause)
            _musicChannel.Source.Pause();
        else
            _musicChannel.Source.UnPause();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 预加载音效到缓存
    /// </summary>
    public void PreloadClip(string key, AudioClip clip)
    {
        if (!_clipCache.ContainsKey(key))
            _clipCache[key] = clip;
    }

    /// <summary>
    /// 从缓存获取音效
    /// </summary>
    public AudioClip GetCachedClip(string key)
    {
        _clipCache.TryGetValue(key, out AudioClip clip);
        return clip;
    }

    /// <summary>
    /// 停止所有音效
    /// </summary>
    public void StopAllSFX(float fadeOutDuration = 0.1f)
    {
        foreach (var channel in _channelPool)
        {
            if (channel.IsPlaying)
                channel.Stop(fadeOutDuration);
        }
    }

    /// <summary>
    /// 设置静音
    /// </summary>
    public void SetMute(bool mute)
    {
        AudioListener.volume = mute ? 0f : 1f;
    }

    private void UpdateAllVolumes()
    {
        // 更新音乐音量
        if (_musicChannel != null && _musicChannel.IsPlaying)
        {
            _musicChannel.SetVolume(musicVolume * masterVolume, 0.1f);
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}

