using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

/// <summary>
/// 使用随机巡逻的敌人控制器示例
/// 不需要设置PatrolPoints，会在初始位置周围随机移动
/// </summary>
public class EnemyControllerRandomPatrol : BaseEnemyController
{
    [Header("随机巡逻设置")]
    public float patrolRadius = 15f; // 巡逻半径
    
    [Header("特效")]
    public VisualEffect RollingVFX;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 使用随机巡逻状态，不需要PatrolPoints
        patrolState = new RandomPatrolState(this, _animator, patrolRadius, Movespeed);
        attackState = new RollingState(this, RollingVFX); // 传入VFX
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (RollingVFX != null) RollingVFX.Stop(); // 初始时停止VFX
        
        if (StateMachine != null && patrolState != null)
        {
            StateMachine.ChangeState(patrolState);
        }
    }
}

