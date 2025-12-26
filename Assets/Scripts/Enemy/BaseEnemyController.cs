using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public abstract class BaseEnemyController : MonoBehaviour
{
    [HideInInspector]
    public CharacterState characterState;
    public StateMachine StateMachine;
    public Transform Target;
    
    [Header("基础属性")]
    public float height = 0.1f;
    // 最大可跨越的高度差（例如台阶/斜坡判断）
    public float _maxStepHeight = 0.5f;
    // 视野范围
    public float SightRange = 10f;
    // 攻击范围
    public float AttackRange = 2.0f;
    // 攻击持续时间
    public float AttackDuation = 1.5f;
    public float Movespeed = 3f;

    [Header("组件引用")]
    public Terrain terrain;
    public NavMeshAsset nav;
    public Animator _animator;


    [Header("巡逻设置 - 可选")]
    public Transform[] PatrolPoints; // 固定点巡逻时使用，随机巡逻可不设置

    // 状态实例 - 子类可以根据需要初始化不同的状态
    // 使用IState接口，支持PatrolState（固定点）、RandomPatrolState（随机）等
    public IState patrolState; // 可选：不巡逻的敌人可不初始化
    public IState attackState; // 使用接口类型，允许不同攻击状态

    // 寻路相关
    public NePathFinder finder;
    protected Coroutine _pathJob;
    protected int _pathRequestId; // 每次请求递增，防止旧回调渲染

    protected virtual void Awake()
    {
        Target = GameObject.FindGameObjectWithTag("Player")?.transform;
        characterState = this.GetComponent<CharacterState>();
        StateMachine = new StateMachine();
        _animator = this.GetComponent<Animator>();
        
        // 状态初始化通常在子类Awake中进行，因为可能依赖特定参数
    }

    protected virtual void Start()
    {

        if (nav != null && terrain != null)
        {
            finder = new NePathFinder(nav, terrain, _maxStepHeight, 4f);
        }
    }

    protected virtual void Update()
    {
        if (_animator == null) return;
        StateMachine.Update();
    }

    public int RequestPathAsync(Vector3 start, Vector3 end, Action<int, List<Vector3>> onDone)
    {
        _pathRequestId++;
        int requestId = _pathRequestId;

        // 取消上一个
        if (_pathJob != null)
        {
            StopCoroutine(_pathJob);
            _pathJob = null;
        }

        if (finder != null)
        {
            _pathJob = StartCoroutine(
                finder.FindPathAsy(
                    start,
                    end,
                    (path) => onDone.Invoke(requestId, path)
                )
            );
        }

        return requestId;
    }

    /// <summary>
    /// 取消当前寻路请求
    /// </summary>
    public void CancelPathRequest(int requestId)
    {
        // 只取消如果是当前的请求。如果是更早的，忽略
        if (requestId != _pathRequestId) return;

        _pathRequestId++; // 增加版本号自动失效回调（即使有回调回来也会被忽略）
        if (_pathJob != null)
        {
            StopCoroutine(_pathJob);
            _pathJob = null;
        }
    }
}

