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
