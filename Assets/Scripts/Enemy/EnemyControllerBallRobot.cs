using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class EnemyControllerBallRobot : BaseEnemyController
{
    [Header("特效")]
    public VisualEffect RollingVFX;
    
    // 状态切换滞后量，防止边缘震荡
    private float _attackHysteresis = 1.5f;
    
    // 跟踪当前是否在攻击状态
    private bool _isInAttackState = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 传入 autoSwitchToAttack = false，由控制器统一管理状态切换
        patrolState = new PatrolState(this, _animator, Movespeed, autoSwitchToAttack: false);
        attackState = new RollingState(this, RollingVFX); // 传入VFX
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (RollingVFX != null) RollingVFX.Stop(); // 初始时停止VFX
        StateMachine.ChangeState(patrolState);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 距离判断：是否切换攻击/巡逻状态
        CheckAttackRange();
    }
    
    /// <summary>
    /// 检查是否需要切换攻击/巡逻状态
    /// </summary>
    private void CheckAttackRange()
    {
        if (Target == null || StateMachine == null) return;
        
        float distance = Vector3.Distance(transform.position, Target.position);
        
        // 当前是巡逻状态，检查是否进入攻击范围
        if (!_isInAttackState && attackState != null)
        {
            if (distance < SightRange - _attackHysteresis)
            {
                _isInAttackState = true;
                StateMachine.ChangeState(attackState);
            }
        }
        // 当前是攻击状态，检查是否超出攻击范围
        else if (_isInAttackState && patrolState != null)
        {
            if (distance > SightRange + _attackHysteresis)
            {
                _isInAttackState = false;
                StateMachine.ChangeState(patrolState);
            }
        }
    }
}
