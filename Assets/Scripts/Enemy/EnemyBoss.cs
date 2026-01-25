using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBoss : BaseEnemyController
{
     [Header("射击设置")]
    public GameObject bulletPrefab;
    [Tooltip("旧：单枪口（不填 bulletSpawnPoints 时会回退用它）")]
    public Transform bulletSpawnPoint;
    [Tooltip("新：多枪口/多枪管（按数组顺序轮流开火）")]
    public Transform[] bulletSpawnPoints;
    public List<AOEData> AOE;
    public float bulletSpeed = 15f;
    public float bulletLifeTime = 3f;
    public float bulletDamage = 10f;
    public float bulletInterval = 0.5f;
    [Tooltip("勾选后：射击时机由动画事件 AE_Shoot / AE_ShootMuzzle 控制；不勾选则按 bulletInterval 自动射击")]
    public bool useAnimationEventTiming = true;
    public BossstartState bossstartState;
    public BossAttackConfig bossAttackConfig;

    public event Action OnBossStart;
    protected override void Awake()
    {
        base.Awake();
        patrolState = new PatrolState(this, _animator, Movespeed);
        ChaseState = new ChaseState(this, _animator, Movespeed,"walk","Idle",true,0.5f);
        bossstartState = new BossstartState(this, _animator);
        attackState = new BossAttackState(this, _animator, bossAttackConfig);
    }
    protected override void Start()
    {
        base.Start();
        StateMachine.ChangeState(bossstartState);

    }
    protected override void Die()
    {
        base.Die();
        _animator.CrossFade("death",0.1f);
    }
    // 当前活跃的 AOE（按索引管理多个）
    private Dictionary<int, AOEAgent> _activeAOEs = new Dictionary<int, AOEAgent>();
    
    public override void DealAOE(string EventName, int AOEIndex)
    {
        switch(EventName)
        {
            case "AOEStart":
                // 创建AOE
                CreateAOE(AOEIndex);
                break;
                
            case "AOEEnd":
                // 激活后延迟销毁
                if (_activeAOEs.TryGetValue(AOEIndex, out var aoe) && aoe != null)
                {
                    aoe.Activate();
                    StartCoroutine(DelayedAOEEnd(aoe, 30));
                    _activeAOEs.Remove(AOEIndex);
                }
                else
                {
                    Debug.LogWarning($"[EnemyBoss] AOEEnd: AOE[{AOEIndex}] 不存在或已销毁");
                }
                break;
        }
    }
    
    private void CreateAOE(int AOEIndex)
    {
        // 检查AOE列表有效性
        if (AOE == null || AOE.Count == 0)
        {
            Debug.LogError("[EnemyBoss] AOE列表为空！");
            return;
        }
        
        if (AOEIndex < 0 || AOEIndex >= AOE.Count)
        {
            Debug.LogError($"[EnemyBoss] AOEIndex {AOEIndex} 超出范围，AOE列表长度: {AOE.Count}");
            return;
        }
        
        var aoeData = AOE[AOEIndex];
        
        if (aoeData.AOEPrefab == null)
        {
            Debug.LogError($"[EnemyBoss] AOE[{AOEIndex}].AOEPrefab 为空！");
            return;
        }
        
        if (aoeData.AoeCenter == null)
        {
            Debug.LogError($"[EnemyBoss] AOE[{AOEIndex}].AoeCenter 为空！");
            return;
        }
        
        Vector3 aoeCenter = aoeData.AoeCenter.position;
        float damage = AOEIndex == 0 ? 20f : 25f;
        
        Debug.Log($"[EnemyBoss] 创建AOE[{AOEIndex}] 位置: {aoeCenter}, 伤害: {damage}");
        
        // 如果该索引已有AOE，先销毁旧的
        if (_activeAOEs.TryGetValue(AOEIndex, out var oldAoe) && oldAoe != null)
        {
            Debug.LogWarning($"[EnemyBoss] AOE[{AOEIndex}] 已存在，先销毁旧的");
            oldAoe.End();
            _activeAOEs.Remove(AOEIndex);
        }
        
        var aoeObj = Instantiate(aoeData.AOEPrefab, aoeCenter, Quaternion.identity);
        var newAoe = aoeObj.GetComponent<AOEAgent>();
        newAoe.OnHit += OnAOEHit;
        if (newAoe == null)
        {
            Debug.LogError($"[EnemyBoss] AOE预制体上没有 AOEAgent 组件！");
            return;
        }
        
        newAoe.damage = damage;
        
        // 存入字典
        _activeAOEs[AOEIndex] = newAoe;
        // 检查Collider
        if (newAoe.damageCollider == null)
        {
            Debug.LogWarning($"[EnemyBoss] AOEAgent 没有设置 damageCollider！");
        }
        
        Debug.Log($"[EnemyBoss] AOE[{AOEIndex}]创建成功: {aoeObj.name}");
    }
    public void OnAOEHit(IDamageable damageable)
    {
        damageable.TakeDamage(new AttackData(transform, 25, false, true));
    }
    private IEnumerator DelayedAOEEnd(AOEAgent aoe, int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null; // 等待一帧
        }
        aoe?.End();
    }
    
    protected override void Update()
    {
        if(StateMachine.CurrentState == ChaseState&&OnBossStart != null)
        {
            StartBoss();
            OnBossStart = null;
        }
        base.Update();
    }
    public void StartBoss()
    {
      OnBossStart?.Invoke();
    }
}
