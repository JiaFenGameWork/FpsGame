using System.Collections;
using UnityEngine;

/// <summary>
/// 音频通道 - 处理单个音效的播放、淡入淡出、混合
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    private AudioSource _audioSource;
    private Coroutine _fadeCoroutine;
    private Coroutine _lifeCoroutine;
    
    /// <summary>
    /// 当前是否正在播放
    /// </summary>
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
    
    /// <summary>
    /// 当前是否空闲（可复用）
    /// </summary>
    public bool IsIdle => !IsPlaying && _fadeCoroutine == null;
    
    /// <summary>
    /// 音频源组件
    /// </summary>
    public AudioSource Source => _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        
        _audioSource.playOnAwake = false;
    }

    /// <summary>
    /// 播放音效（支持淡入）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">目标音量</param>
    /// <param name="fadeInDuration">淡入时间（0为立即播放）</param>
    /// <param name="pitch">音调</param>
    /// <param name="loop">是否循环</param>
    /// <param name="spatialBlend">3D空间混合（0=2D, 1=3D）</param>
    public void Play(AudioClip clip, float volume = 1f, float fadeInDuration = 0f, 
                     float pitch = 1f, bool loop = false, float spatialBlend = 0f)
    {
        if (clip == null) return;
        
        // 停止之前的淡出协程
        StopAllFades();
        
        _audioSource.clip = clip;
        _audioSource.pitch = pitch;
        _audioSource.loop = loop;
        _audioSource.spatialBlend = spatialBlend;
        
        if (fadeInDuration > 0)
        {
            _audioSource.volume = 0f;
            _audioSource.Play();
            _fadeCoroutine = StartCoroutine(FadeVolume(volume, fadeInDuration));
        }
        else
        {
            _audioSource.volume = volume;
            _audioSource.Play();
        }
    }

    /// <summary>
    /// 播放一次性音效（PlayOneShot，支持多个音效叠加）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="volume">音量</param>
    /// <param name="pitchVariation">随机音调变化范围（增加多样性）</param>
    public void PlayOneShot(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null) return;
        
        // 应用随机音调变化
        if (pitchVariation > 0)
        {
            _audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        }
        else
        {
            _audioSource.pitch = 1f;
        }
        
        _audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// 停止播放（支持淡出）
    /// </summary>
    /// <param name="fadeOutDuration">淡出时间（0为立即停止）</param>
    public void Stop(float fadeOutDuration = 0f)
    {
        StopAllFades();
        
        if (fadeOutDuration > 0 && _audioSource.isPlaying)
        {
            _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOutDuration));
        }
        else
        {
            _audioSource.Stop();
            _audioSource.volume = 0f;
        }
    }

    /// <summary>
    /// 交叉淡入淡出到新音效
    /// </summary>
    /// <param name="newClip">新音频剪辑</param>
    /// <param name="targetVolume">目标音量</param>
    /// <param name="crossfadeDuration">交叉淡入淡出时间</param>
    /// <param name="loop">是否循环</param>
    public void CrossfadeTo(AudioClip newClip, float targetVolume = 1f, 
                            float crossfadeDuration = 0.5f, bool loop = false)
    {
        if (newClip == null) return;
        
        StopAllFades();
        _fadeCoroutine = StartCoroutine(CrossfadeCoroutine(newClip, targetVolume, crossfadeDuration, loop));
    }

    /// <summary>
    /// 设置3D音效位置
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// 设置音量（支持平滑过渡）
    /// </summary>
    public void SetVolume(float volume, float duration = 0f)
    {
        if (duration > 0)
        {
            StopAllFades();
            _fadeCoroutine = StartCoroutine(FadeVolume(volume, duration));
        }
        else
        {
            _audioSource.volume = volume;
        }
    }

    /// <summary>
    /// 在指定时间后自动停止并归还通道
    /// </summary>
    public void AutoStopAfter(float delay, float fadeOutDuration = 0.1f)
    {
        if (_lifeCoroutine != null)
            StopCoroutine(_lifeCoroutine);
        _lifeCoroutine = StartCoroutine(AutoStopCoroutine(delay, fadeOutDuration));
    }

    private IEnumerator AutoStopCoroutine(float delay, float fadeOutDuration)
    {
        yield return new WaitForSeconds(delay);
        Stop(fadeOutDuration);
        _lifeCoroutine = null;
    }

    private IEnumerator FadeVolume(float targetVolume, float duration)
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 使用平滑的缓动曲线
            t = t * t * (3f - 2f * t); // Smoothstep
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smoothstep
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        _audioSource.Stop();
        _audioSource.volume = 0f;
        _fadeCoroutine = null;
    }

    private IEnumerator CrossfadeCoroutine(AudioClip newClip, float targetVolume, 
                                           float duration, bool loop)
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;
        
        // 先淡出当前音效
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            t = t * t * (3f - 2f * t);
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        // 切换音效
        _audioSource.Stop();
        _audioSource.clip = newClip;
        _audioSource.loop = loop;
        _audioSource.volume = 0f;
        _audioSource.Play();
        
        // 淡入新音效
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.5f);
            t = t * t * (3f - 2f * t);
            _audioSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        
        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }

    private void StopAllFades()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    /// <summary>
    /// 重置通道状态
    /// </summary>
    public void Reset()
    {
        StopAllFades();
        if (_lifeCoroutine != null)
        {
            StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = null;
        }
        _audioSource.Stop();
        _audioSource.clip = null;
        _audioSource.volume = 1f;
        _audioSource.pitch = 1f;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }
}

