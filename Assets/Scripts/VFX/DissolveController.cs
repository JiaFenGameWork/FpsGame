using UnityEngine;

/// <summary>
/// 溶解效果控制器
/// </summary>
public class DissolveController : MonoBehaviour
{
    [Header("材质设置")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;
    
    [Header("溶解参数")]
    [Range(0f, 1f)]
    [SerializeField] private float dissolveAmount = 0f;
    [SerializeField] private float dissolveDuration = 2f;
    
    [Header("自动播放")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool reverseOnComplete = false;
    
    private Material _material;
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    
    private float _currentDissolve = 0f;
    private float _targetDissolve = 0f;
    private bool _isAnimating = false;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        if (targetRenderer != null)
        {
            // 创建材质实例，避免修改原始材质
            _material = targetRenderer.materials[materialIndex];
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartDissolve();
        }
    }

    private void Update()
    {
        if (_isAnimating)
        {
            // 平滑过渡到目标值
            _currentDissolve = Mathf.MoveTowards(_currentDissolve, _targetDissolve, Time.deltaTime / dissolveDuration);
            SetDissolveAmount(_currentDissolve);
            
            // 检查是否完成
            if (Mathf.Approximately(_currentDissolve, _targetDissolve))
            {
                _isAnimating = false;
                
                if (reverseOnComplete && _targetDissolve >= 1f)
                {
                    // 自动反向
                    Invoke(nameof(StartAppear), 0.5f);
                }
            }
        }
    }

    private void OnValidate()
    {
        // 在编辑器中实时预览
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        if (targetRenderer != null && Application.isPlaying == false)
        {
            var mat = targetRenderer.sharedMaterial;
            if (mat != null && mat.HasProperty(DissolveAmountID))
            {
                mat.SetFloat(DissolveAmountID, dissolveAmount);
            }
        }
    }

    /// <summary>
    /// 设置溶解程度 (0-1)
    /// </summary>
    public void SetDissolveAmount(float amount)
    {
        dissolveAmount = Mathf.Clamp01(amount);
        if (_material != null)
        {
            _material.SetFloat(DissolveAmountID, dissolveAmount);
        }
    }

    /// <summary>
    /// 开始溶解（消失）
    /// </summary>
    public void StartDissolve()
    {
        _currentDissolve = 0f;
        _targetDissolve = 1f;
        _isAnimating = true;
    }

    /// <summary>
    /// 开始出现（反向溶解）
    /// </summary>
    public void StartAppear()
    {
        _currentDissolve = 1f;
        _targetDissolve = 0f;
        _isAnimating = true;
    }

    /// <summary>
    /// 立即设置为完全溶解
    /// </summary>
    public void DissolveImmediate()
    {
        _isAnimating = false;
        _currentDissolve = 1f;
        SetDissolveAmount(1f);
    }

    /// <summary>
    /// 立即设置为完全显示
    /// </summary>
    public void AppearImmediate()
    {
        _isAnimating = false;
        _currentDissolve = 0f;
        SetDissolveAmount(0f);
    }

    /// <summary>
    /// 动画播放到指定值
    /// </summary>
    public void AnimateTo(float targetValue)
    {
        _targetDissolve = Mathf.Clamp01(targetValue);
        _isAnimating = true;
    }
}





















