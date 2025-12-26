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
    
    protected override void Awake()
    {
        base.Awake();
        
        patrolState = new PatrolState(this, _animator, Movespeed);
        attackState = new RollingState(this, RollingVFX); // 传入VFX
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (RollingVFX != null) RollingVFX.Stop(); // 初始时停止VFX
        StateMachine.ChangeState(patrolState);
    }
    
    // Update 由基类处理
}
