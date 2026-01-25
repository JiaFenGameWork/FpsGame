using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// AOE 阶段枚举
/// </summary>
public enum AOEPhase
{
    None,       // 未开始
    Active,     // 激活阶段（造成伤害）
    Ended       // 结束
}

/// <summary>
/// AOE 代理 - 处理伤害事件和生命周期
/// </summary>
public class AOEAgent : MonoBehaviour
{
    [Header("伤害设置")]
    public float damage = 20f;
    public bool canHitMultipleTimes = false;
    public float hitInterval = 0.5f;
    
    [Header("组件")]
    public Collider damageCollider;
    
    // 当前阶段
    public AOEPhase CurrentPhase { get; private set; } = AOEPhase.None;
    
    // 事件
    public event Action OnActivate;
    public event Action<IDamageable> OnHit;
    
    // 内部状态
    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();
    private Dictionary<GameObject, float> _lastHitTime = new Dictionary<GameObject, float>();
    
    void Awake()
    {
        if (damageCollider == null)
            damageCollider = GetComponent<Collider>();
            
        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }
    }
    
    #region 阶段控制（由动画事件调用）
    
    /// <summary>
    /// 激活伤害
    /// </summary>
    public void Activate()
    {
        if (CurrentPhase == AOEPhase.Active || CurrentPhase == AOEPhase.Ended)
            return;
            
        CurrentPhase = AOEPhase.Active;
        _hitTargets.Clear();
        _lastHitTime.Clear();
        
        if (damageCollider != null)
            damageCollider.enabled = true;
            
        OnActivate?.Invoke();
    }
    
    /// <summary>
    /// 结束并销毁
    /// </summary>
    public void End()
    {
        if (CurrentPhase == AOEPhase.Ended) return;
        
        CurrentPhase = AOEPhase.Ended;
        
        if (damageCollider != null)
            damageCollider.enabled = false;
        Destroy(gameObject);
    }
    
    #endregion
    
    #region 伤害检测
    
    void OnTriggerEnter(Collider other)
    {
        TryHitTarget(other.gameObject);
    }
    
    void OnTriggerStay(Collider other)
    {
        if (canHitMultipleTimes)
            TryHitTarget(other.gameObject);
    }
    
    private void TryHitTarget(GameObject target)
    {
        if (CurrentPhase != AOEPhase.Active) return;
        
        // 检查是否有可伤害的组件
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return;
        
        // 单次命中检查
        if (!canHitMultipleTimes && _hitTargets.Contains(target))
            return;
            
        // 多次命中间隔检查
        if (canHitMultipleTimes && _lastHitTime.TryGetValue(target, out float lastTime))
        {
            if (Time.time - lastTime < hitInterval)
                return;
        }
        
        _hitTargets.Add(target);
        _lastHitTime[target] = Time.time;
        OnHit?.Invoke(damageable);
    }
    
    #endregion
    
    #region 动画事件接口
    
    public void AE_AOEActivate() => Activate();
    public void AE_AOEEnd() => End();
    
    #endregion
}
[System.Serializable]
public struct AOEData
{
    public GameObject AOEPrefab;
    public Transform AoeCenter;
}