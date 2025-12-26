using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

/// <summary>
/// 不需要巡逻的敌人控制器示例（如炮台）
/// 不设置patrolState，直接进入攻击状态或待机状态
/// </summary>
public class EnemyControllerTurret : BaseEnemyController
{
    private IState idleState; // 待机状态
    
    [Header("特效")]
    public VisualEffect AttackVFX; // 炮台可能用不同的特效
    
    protected override void Awake()
    {
        base.Awake();
        
        // 不初始化patrolState，因为炮台不需要移动
        // patrolState = null; (默认就是null)
        
        idleState = new TurretIdleState(this, _animator);
        attackState = new RollingState(this, AttackVFX); // 传入VFX，或者实现其他攻击状态
    }
    
    protected override void Start()
    {
        base.Start();
        
        if (AttackVFX != null) AttackVFX.Stop(); // 初始时停止VFX
        
        // 从待机状态开始
        if (StateMachine != null && idleState != null)
        {
            StateMachine.ChangeState(idleState);
        }
    }
}

/// <summary>
/// 炮台待机状态：原地旋转搜索目标
/// </summary>
public class TurretIdleState : IState
{
    private BaseEnemyController _controller;
    private Animator _animator;
    private float _rotationSpeed = 30f; // 每秒旋转角度
    private static readonly int _idleHash = Animator.StringToHash("anim_Idle_Loop_S");
    
    public TurretIdleState(BaseEnemyController controller, Animator animator)
    {
        _controller = controller;
        _animator = animator;
    }
    
    public void OnEnter()
    {
        _animator?.CrossFade(_idleHash, 0.1f);
    }
    
    public void Tick()
    {
        // 原地旋转
        _controller.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        
        // 检测玩家
        if (_controller.Target != null)
        {
            float distance = Vector3.Distance(
                _controller.transform.position, 
                _controller.Target.position
            );
            
            if (distance < _controller.SightRange && _controller.attackState != null)
            {
                _controller.StateMachine.ChangeState(_controller.attackState);
            }
        }
    }
    
    public void OnExit()
    {
        // 清理
    }
}

