using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class EnemyControllerRobot1 : BaseEnemyController
{
    // 巡逻点等基础属性已在 BaseEnemyController 中定义

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
    
    // 状态切换滞后量，防止边缘震荡
    private float _attackHysteresis = 1.5f;
    
    // 跟踪当前是否在攻击状态
    private bool _isInAttackState = false;

    protected override void Awake()
    {
        base.Awake();
        // BaseEnemyController 中的 Awake 已经初始化了:
        // Target, characterState, StateMachine, _animator

        // 只有特定的状态初始化需要在这里做
        patrolState = new RandomPatrolState(this, _animator, 10f, Movespeed,"RM_walk","idle");
        
        // 初始化射击状态
        Transform[] spawns = (bulletSpawnPoints != null && bulletSpawnPoints.Length > 0)
            ? bulletSpawnPoints
            : (bulletSpawnPoint != null ? new[] { bulletSpawnPoint } : null);

        if (bulletPrefab != null && spawns != null && spawns.Length > 0)
        {
            attackState = new ShootingState(
                this,
                _animator,
                bulletPrefab,
                spawns,
                bulletSpeed,
                bulletLifeTime,
                bulletDamage,
                bulletInterval,
                "attack",  // 射击动画名称，根据实际情况修改
                "idle",      // 待机动画名称
                useAnimationEventTiming
            );
        }
        ChaseState = new ChaseState(this,_animator,5,"RM_walk","idle",true,1f);
    }
    
    protected override void Start()
    {
        base.Start(); // 初始化 finder, RollingVFX.Stop
        // 启动初始状态
        if (StateMachine != null && patrolState != null)
        {
            StateMachine.ChangeState(patrolState);
        }
    }
    protected override void Die()
    {
        base.Die();
        _animator.CrossFade("death",0.1f);
    }
    protected override void Update()
    {
        base.Update();
        
        // 距离判断：是否进入攻击状态
        CheckAttackRange();
    }
    
    /// <summary>
    /// 检查是否需要切换攻击/巡逻状态
    /// </summary>
    private void CheckAttackRange()
    {
        if (Target == null || StateMachine == null) return;
        
        float distance = Vector3.Distance(transform.position, Target.position);
          if (distance < SightRange + _attackHysteresis&&StateMachine.CurrentState!=ChaseState)
        {
            if(distance>AttackRange-_attackHysteresis)
            {
            StateMachine.ChangeState(ChaseState);
            }
        }
    }
}
