using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class EnemyControllerRobot1 : BaseEnemyController
{
    // 巡逻点等基础属性已在 BaseEnemyController 中定义

    protected override void Awake()
    {
        base.Awake();
        // BaseEnemyController 中的 Awake 已经初始化了:
        // Target, characterState, StateMachine, _animator

        // 只有特定的状态初始化需要在这里做
        patrolState = new RandomPatrolState(this, _animator, 10f, Movespeed,"RM_walk","idle");
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

    // Update 已经由基类处理 StateMachine.Update()
    // 如果有特殊逻辑可以重写 Update 并调用 base.Update()
}
