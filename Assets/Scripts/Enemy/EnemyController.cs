using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{

    public StateMachine StateMachine;
    Transform Target;
    public float height = 0.1f;
    // 最大可跨越的单步高度差（用于台阶/斜坡判断）
    public float _maxStepHeight = 0.5f;
    //视野范围
    public float SightRange = 10f;
    //攻击距离
    public float AttackRange = 2.0f;
    //攻击间隔
    public float AttackDuation = 1.5f;
    //巡逻点
    public Terrain terrain;
    public Transform[] PatrolPoints;
    public PatrolState PatrolState;
    public NePathFinder finder;
    public NavMeshAsset nav;
    private Coroutine _pathJob;
    private int _pathRequestId; // 单调递增，防止旧回调污染
    Animator _animator;
    public float Movespeed = 3f;
    // Start is called before the first frame update
    private void Awake()
    {
        Target = GameObject.FindGameObjectWithTag("Player").transform;
        StateMachine = new StateMachine();
        _animator = this.GetComponent<Animator>();
        PatrolState = new PatrolState(this,_animator,Movespeed);


    }
    void Start()
    {
        finder = new NePathFinder(nav, terrain, _maxStepHeight, 4f);
        StateMachine.ChangeState(PatrolState);
    }

    // Update is called once per frame
    void Update()
    {
        if (_animator == null)
        {
            return;
        }
        StateMachine.Update();
    }
    public int RequestPathAsync(
      Vector3 start,
      Vector3 end,
      Action<int, List<Vector3>> onDone
     )
    {
        _pathRequestId++;
        int requestId = _pathRequestId;

        // 取消上一条
        if (_pathJob != null)
        {
            StopCoroutine(_pathJob);
            _pathJob = null;
        }

        _pathJob = StartCoroutine(
            finder.FindPathAsy(
                start,
                end,
               (path)=>onDone.Invoke(requestId,path)
            )
        );

        return requestId;
    }

    /// <summary>
    /// 取消当前寻路（如果有）
    /// </summary>
    public void CancelPathRequest(int requestId)
    {
        // 只取消“当前最新”的那个请求
        if (requestId != _pathRequestId) return;

        _pathRequestId++; // 让已有回调自动失效（即使它侥幸被调用）
        if (_pathJob != null)
        {
            StopCoroutine(_pathJob);
            _pathJob = null;
        }
    }
}
