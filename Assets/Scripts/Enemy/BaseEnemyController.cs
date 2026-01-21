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
    public IState ChaseState;

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
        if (characterState.CurrentHealth <= 0)
        {
            Die();

        }
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

        // 注意：Unity 的 StartCoroutine 会立即执行协程直到第一次 yield。
        // NePathFinder.FindPathAsy 在“很快成功/很快失败”时可能在同一帧直接 ondone + yield break，
        // 这会导致调用方还没来得及接收并写入 requestId（例如 State 内的 _pathRequestId），回调就已经触发。
        // 为了避免这种竞态，这里包一层协程，先 yield 一帧再开始真正寻路与回调。
        if (finder != null)
        {
            _pathJob = StartCoroutine(PathJobDelayedOneFrame(start, end, requestId, onDone));
        }
        else
        {
            // finder 尚未初始化时，直接回调 null，避免外部一直处于 pending 状态
            onDone?.Invoke(requestId, null);
        }

        return requestId;
    }

    private IEnumerator PathJobDelayedOneFrame(Vector3 start, Vector3 end, int requestId, Action<int, List<Vector3>> onDone)
    {
        // 让调用方先完成本帧的赋值（例如 _pathRequestId = RequestPathAsync(...)）
        yield return null;

        if (finder == null)
        {
            onDone?.Invoke(requestId, null);
            yield break;
        }

        yield return finder.FindPathAsy(start, end, (path) => onDone?.Invoke(requestId, path));
    }
    protected virtual void Die()
    {
        characterState.Die();
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

    #region Animation Events (动作片段事件)
    /// <summary>
    /// 通用动画事件回调：string 参数。
    /// 在 AnimationClip 的 Event 里填函数名 "AE" 并传入字符串 key。
    /// 例如： "HitStart" / "HitEnd" / "IFrameOn" / "IFrameOff" / "Shoot:3" 等。
    /// 会转发给当前 State（如果它实现了 IAnimationEventReceiver）。
    /// </summary>
    public void AE(string key)
    {
        if (StateMachine == null) return;
        var current = StateMachine.CurrentState;
        if (current is IAnimationEventReceiver receiver)
        {
            receiver.OnAnimationEvent(key);
        }
    }

    /// <summary>
    /// 通用动画事件回调：int 参数。
    /// 在 AnimationClip 的 Event 里填函数名 "AE_Int" 并传入 int。
    /// 会转发给当前 State（如果它实现了 IAnimationEventReceiver）。
    /// </summary>
    public void AE_Int(int value)
    {
        if (StateMachine == null) return;
        var current = StateMachine.CurrentState;
        if (current is IAnimationEventReceiver receiver)
        {
            receiver.OnAnimationEventInt(value);
        }
    }

    /// <summary>
    /// AnimationClip 事件回调：射击（不带参数）。
    /// 在动画事件里填函数名 "AE_Shoot" 即可触发当前攻击状态的开火时机。
    /// </summary>
    public void AE_Shoot()
    {
        AE_Shoot(1);
    }

    /// <summary>
    /// AnimationClip 事件回调：射击（可传入连发次数）。
    /// 注意：Unity 的 AnimationEvent 支持传 int 参数。
    /// </summary>
    public void AE_Shoot(int count)
    {
        if (StateMachine == null) return;

        // 只在当前状态为 ShootingState 时响应，避免误触发
        var current = StateMachine.CurrentState;
        if (current is ShootingState shooting)
        {
            shooting.RequestShootFromAnimationEvent(count);
        }
    }

    /// <summary>
    /// AnimationClip 事件回调：指定枪口索引射击（muzzleIndex 对应 bulletSpawnPoints 数组下标）。
    /// 在动画事件里填函数名 "AE_ShootMuzzle"，参数填 0/1/2... 即可指定枪口。
    /// </summary>
    public void AE_ShootMuzzle(int muzzleIndex)
    {
        if (StateMachine == null) return;
        var current = StateMachine.CurrentState;
        if (current is ShootingState shooting)
        {
            shooting.RequestShootFromAnimationEventMuzzle(muzzleIndex);
        }
    }
    #endregion
}

