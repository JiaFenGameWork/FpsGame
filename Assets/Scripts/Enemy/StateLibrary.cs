using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.GraphicsBuffer;

#region 巡逻
public class PatrolState : IState
{
    private BaseEnemyController enemyController;

    // Ѳ�ߵ�����
    private int currentPatrolIndex = 0;

    // ��ǰ·��
    public List<Vector3> currentPath;
    private int currentPathIndex = 0;

    // �ƶ�����
    public float moveSpeed = 3f;
    private float arrivalThreshold = 0.5f;
    private float patrolPointThreshold = 1f;

    // ״̬
    private bool isMoving = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float waitDuration = 2f;
    static int walkHash = Animator.StringToHash("anim_Walk_Loop");
    static int idleHash = Animator.StringToHash("anim_Idle_Loop_S");
    private bool _isPathPending = false;
    private bool _active = false;
    private int _pathRequestId = 0;
    private Animator _animator;
    
    // 状态切换的滞后量，防止边缘震荡
    private float _attackHysteresis = 1.5f;

    // --- 卡住检测变量 ---
    private Vector3 _lastStuckCheckPos;
    private float _stuckCheckTimer = 0f;
    private const float STUCK_CHECK_INTERVAL = 1.0f; // 每1秒检测一次
    private const float STUCK_MIN_DIST = 0.2f;       // 1秒内移动少于0.2米视为卡住
    // -------------------

    public PatrolState(BaseEnemyController controller, Animator ani,float speed = 3f)
    {
        this.enemyController = controller;
        this.moveSpeed = speed;
        this._animator = ani;
    }

    public void OnEnter()
    {
        if (enemyController.nav == null)
        {
            Debug.LogError("����Ҫ��Baker�決һ��octree��Ȼ������");
            return;
        }
        Debug.Log($"[PatrolState] OnEnter - 当前位置: {enemyController.transform.position}");
        _animator.CrossFade(idleHash, 0.1f);
        _active = true;
        
        // 初始化卡住检测
        _lastStuckCheckPos = enemyController.transform.position;
        _stuckCheckTimer = 0f;
        // Ѱ�������Ѳ�ߵ���Ϊ��ʼĿ��
        currentPatrolIndex = FindNearestPatrolPointIndex();

        // ���㵽��һ��Ѳ�ߵ��·��
        CalculatePathToCurrentPatrolPoint();
    }

    public void OnExit()
    {
        isMoving = false;
        isWaiting = false;
        currentPath = null;
        _active = false;
        if (_isPathPending) enemyController.CancelPathRequest(_pathRequestId);
        _isPathPending = false;
    }

    public void Tick()
    {
        if (enemyController.PatrolPoints == null || enemyController.PatrolPoints.Length == 0)
            return;
        if (_isPathPending)
        {
            // Debug.Log("[PatrolState] Tick - 等待寻路回调...");
            return;
        }
        // �ȴ�״̬
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                isWaiting = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint();
            }
            return;
        }
        float dis = (enemyController.transform.position - enemyController.Target.position).magnitude;
        // 使用比 SightRange 更小的距离进入攻击，防止边缘震荡
        if (dis < enemyController.SightRange - _attackHysteresis)
        {
            enemyController.StateMachine.ChangeState(enemyController.attackState);
        }
        // �ƶ�״̬
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            CheckIfStuck();
            MoveAlongPath();
        }
    }

    private void CheckIfStuck()
    {
        _stuckCheckTimer += Time.deltaTime;
        if (_stuckCheckTimer >= STUCK_CHECK_INTERVAL)
        {
            float dist = Vector3.Distance(enemyController.transform.position, _lastStuckCheckPos);
            if (dist < STUCK_MIN_DIST)
            {
                Debug.LogWarning($"检测到卡住 (移动距离 {dist:F2} < {STUCK_MIN_DIST})，尝试跳到下一个点");
                MoveToNextPatrolPoint();
            }
            _lastStuckCheckPos = enemyController.transform.position;
            _stuckCheckTimer = 0f;
        }
    }

    private void MoveAlongPath()
    {
        if (currentPathIndex >= currentPath.Count)
        {
            OnReachPatrolPoint();
            return;
        }

        Vector3 targetPos = currentPath[currentPathIndex];
        Vector3 currentPos = enemyController.transform.position;

        Vector3 direction = targetPos - currentPos;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;

        if (horizontalDistance < arrivalThreshold)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                OnReachPatrolPoint();
            }
            return;
        }

        // ˮƽ�ƶ�����
        Vector3 moveDir = direction.normalized;
        moveDir.y = 0;
        Vector3 nextPos = currentPos + moveDir * moveSpeed * Time.deltaTime;

        enemyController.transform.position = nextPos;

        // ��ת
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            enemyController.transform.rotation = Quaternion.Slerp(
                enemyController.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }
    private void OnReachPatrolPoint()
    {
        isMoving = false;
        isWaiting = true;
        waitTimer = 0f;
        _animator.CrossFade(idleHash, 0.1f);
    }

    private void MoveToNextPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % enemyController.PatrolPoints.Length;

        CalculatePathToCurrentPatrolPoint();

    }

    private void CalculatePathToCurrentPatrolPoint()
    {
        if(!_active) return;
        if (enemyController.PatrolPoints.Length == 0) return;

        Vector3 start = enemyController.transform.position;
        Vector3 end = enemyController.PatrolPoints[currentPatrolIndex].position;
        if (_isPathPending)
        {
            enemyController.CancelPathRequest(_pathRequestId);
            _isPathPending = false;
        }
        isMoving = false;
        currentPath = null;
        currentPathIndex = 0;

        _isPathPending = true;

        _pathRequestId= enemyController.RequestPathAsync(start, end, callback);
        
    }
    public void callback(int id,List<Vector3> path)
    {
        if(!_active)
        {
            Debug.Log("[PatrolState] callback - 状态不活跃，忽略回调");
            return;
        }
        if (id != _pathRequestId)
        {
            Debug.Log($"[PatrolState] callback - 请求ID不匹配 (收到:{id}, 期望:{_pathRequestId})");
            return;
        }
        _animator.CrossFade(walkHash, 0.1f);
        _isPathPending = false;
        if(path != null && path.Count > 0)
        {
            Debug.Log($"[PatrolState] callback - 成功获取路径，路径点数: {path.Count}");
            currentPath = path;
            currentPathIndex = 0;
            isMoving=true;
            isWaiting = false;
            waitTimer = 0f;
        }
        else
        {
            Debug.LogWarning($"[PatrolState] callback - 寻路失败！当前位置: {enemyController.transform.position}, 目标点: {enemyController.PatrolPoints[currentPatrolIndex].position}");
            // �Ҳ���·��ʱ����΢�ȴ�һ��������һ������ֹ��ѭ������֡��
            isWaiting = true;
            waitTimer = 0.5f;
            currentPatrolIndex = (currentPatrolIndex + 1) % enemyController.PatrolPoints.Length;
        }
    }
    private int FindNearestPatrolPointIndex()
    {
        if (enemyController.PatrolPoints == null || enemyController.PatrolPoints.Length == 0)
            return 0;

        float minDistance = float.MaxValue;
        int nearestIndex = 0;
        Vector3 currentPos = enemyController.transform.position;

        for (int i = 0; i < enemyController.PatrolPoints.Length; i++)
        {
            float dist = Vector3.Distance(currentPos, enemyController.PatrolPoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }

    public void RecalculatePath()
    {
        CalculatePathToCurrentPatrolPoint();
    }

}
#endregion
#region 随机巡逻
public class RandomPatrolState : IState
{
    private BaseEnemyController enemyController;
    private Animator _animator;
    
    // 移动相关
    private Vector3 _randomTarget;
    private List<Vector3> currentPath;
    private int currentPathIndex = 0;
    private float moveSpeed = 3f;
    private float arrivalThreshold = 0.5f;
    
    // 随机范围
    private float _patrolRadius = 10f;
    private Vector3 _patrolCenter;
    
    // 状态
    private bool isMoving = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float waitDuration = 2f;
    public string walkTrigger;
    public string idleTrigger;
    public int walkHash;
    public int idleHash;
    
    private bool _isPathPending = false;
    private bool _active = false;
    private int _pathRequestId = 0;
    
    private float _attackHysteresis = 1.5f;
    
    public RandomPatrolState(BaseEnemyController enemy, Animator ani, float radius = 10f, float speed = 3f, string walkTrigger = "anim_Walk_Loop", string idleTrigger = "anim_Idle_Loop_S")
    {
        enemyController = enemy;
        _animator = ani;
        _patrolRadius = radius;
        moveSpeed = speed;
        this.walkTrigger = walkTrigger;
        this.idleTrigger = idleTrigger;
        this.walkHash = Animator.StringToHash(walkTrigger);
        this.idleHash = Animator.StringToHash(idleTrigger);
    }
    
    public void OnEnter()
    {
        if (enemyController.nav == null)
        {
            Debug.LogError("需要先Baker一个octree，否则不能寻路");
            return;
        }
        
        _animator.CrossFade(idleHash, 0.1f);
        _active = true;
        _patrolCenter = enemyController.transform.position;
        
        // 生成第一个随机目标点
        GenerateRandomTarget();
    }
    
    public void OnExit()
    {
        isMoving = false;
        isWaiting = false;
        currentPath = null;
        _active = false;
        if (_isPathPending) enemyController.CancelPathRequest(_pathRequestId);
        _isPathPending = false;
    }
    
    public void Tick()
    {
        if (_isPathPending) return;
        
        // 等待状态
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                isWaiting = false;
                waitTimer = 0f;
                GenerateRandomTarget(); // 生成新的随机目标
            }
            return;
        }
        
        // 检测玩家
        if (enemyController.Target != null)
        {
            float dis = (enemyController.transform.position - enemyController.Target.position).magnitude;
            if (dis < enemyController.SightRange - _attackHysteresis)
            {
                if (enemyController.attackState != null)
                {
                    enemyController.StateMachine.ChangeState(enemyController.attackState);
                }
            }
        }
        
        // 移动状态
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            MoveAlongPath();
        }
    }
    
    private void GenerateRandomTarget()
    {
        // 在巡逻中心周围随机生成一个点
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _patrolRadius;
        _randomTarget = _patrolCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // 计算路径
        CalculatePathToTarget();
    }
    
    private void CalculatePathToTarget()
    {
        if (!_active) return;
        
        Vector3 start = enemyController.transform.position;
        Vector3 end = _randomTarget;
        
        if (_isPathPending)
        {
            enemyController.CancelPathRequest(_pathRequestId);
            _isPathPending = false;
        }
        
        isMoving = false;
        currentPath = null;
        currentPathIndex = 0;
        _isPathPending = true;
        
        _pathRequestId = enemyController.RequestPathAsync(start, end, PathCallback);
    }
    
    private void PathCallback(int id, List<Vector3> path)
    {
        if (!_active) return;
        if (id != _pathRequestId) return;
        
        _isPathPending = false;
        
        if (path != null && path.Count > 0)
        {
            _animator.CrossFade(walkHash, 0.1f);
            currentPath = path;
            currentPathIndex = 0;
            isMoving = true;
            isWaiting = false;
            waitTimer = 0f;
        }
        else
        {
            // 找不到路径时等待后重新生成
            isWaiting = true;
            waitTimer = 0f;
        }
    }
    
    private void MoveAlongPath()
    {
        if (currentPathIndex >= currentPath.Count)
        {
            OnReachTarget();
            return;
        }
        
        Vector3 targetPos = currentPath[currentPathIndex];
        Vector3 currentPos = enemyController.transform.position;
        Vector3 direction = targetPos - currentPos;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        
        if (horizontalDistance < arrivalThreshold)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                OnReachTarget();
            }
            return;
        }
        
        // 水平移动
        Vector3 moveDir = direction.normalized;
        moveDir.y = 0;
        Vector3 nextPos = currentPos + moveDir * moveSpeed * Time.deltaTime;
        enemyController.transform.position = nextPos;
        
        // 旋转
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            enemyController.transform.rotation = Quaternion.Slerp(
                enemyController.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }
    
    private void OnReachTarget()
    {
        isMoving = false;
        isWaiting = true;
        waitTimer = 0f;
        _animator.CrossFade(idleHash, 0.1f);
    }
}
#endregion
#region 攻击

public class RollingState : IState
{
    bool isAttacking = false;
    BallAttackLogic ballAttackLogic;
    public BaseEnemyController enemyController;
    public StaggerState staggerState;
    public VisualEffect RollingVFX;
    // 缓存动画Hash，避免每帧计算
    private static readonly int _rollAnimHash = Animator.StringToHash("anim_open_GoToRoll");
    // 记录上次播放动画的时间，防止短时间内重复触发
    private float _lastAnimTime = -999f;
    private const float _animCooldown = 0.3f;

    public RollingState(BaseEnemyController enemy, VisualEffect rollingVFX = null)
    {
        enemyController = enemy;
        RollingVFX = rollingVFX;
    }
    
    public void OnEnter()
    {
        // 重置状态
        isAttacking = false;
        staggerState = new StaggerState(enemyController,enemyController._animator,new string("anim_closed_StopRoll"));
        staggerState.Configure(2f,enemyController.attackState);
        ballAttackLogic = new BallAttackLogic(enemyController.transform,enemyController.Target,enemyController._animator,LayerMask.GetMask("Obstacle"),2.0f);
        ballAttackLogic.OnAttackStart += ()=> { isAttacking = true; RollingVFX?.Play(); };
        ballAttackLogic.OnHit+= (damageable)=>{damageable.TakeDamage(new AttackData(enemyController.transform,10f,false,true));};
        ballAttackLogic.OnAttackEnd += ()=> 
        {
            ballAttackLogic.Cancel();
            isAttacking = false; 
            enemyController.StateMachine.ChangeState(staggerState);
            RollingVFX?.Stop();
        };
        
        // 检查冷却时间，防止动画频繁重置
        float currentTime = Time.time;
        if (currentTime - _lastAnimTime >= _animCooldown)
        {
            enemyController._animator.CrossFade(_rollAnimHash, 0.2f);
            _lastAnimTime = currentTime;
        }
        isAttacking = true;
    }
    
    public void OnExit()
    {
        ballAttackLogic.Cancel();
        isAttacking = false;  // 确保退出时重置状态
    }

    public void Tick()
    {
        if (ballAttackLogic == null)
        {
            return;
        }
        
        // 使用比 SightRange 更大的距离退出攻击，防止边缘震荡
        // （进入攻击需要 < SightRange - 1.5f，退出需要 > SightRange + 1.5f）
        float exitRange = enemyController.SightRange + 1.5f;

        ballAttackLogic.TryStartAttack(enemyController.Target);
        ballAttackLogic.Tick();

    }
}
#endregion

#region 停顿/硬直
/// <summary>
/// 硬直/停顿状态：播放受击动画后在指定时间内停顿，再恢复到给定状态
/// </summary>
public class StaggerState : IState
{
    private readonly BaseEnemyController _enemyController;
    private readonly Animator _animator;
    private readonly int _hitHash;

    private float _duration;
    private float _timer;
    private IState _resumeState;

    public StaggerState(BaseEnemyController enemyController, Animator animator, string hitTrigger = "anim_Hit")
    {
        _enemyController = enemyController;
        _animator = animator;
        _hitHash = Animator.StringToHash(hitTrigger);
        _duration = 0.5f;
    }

    /// <summary>
    /// 进入硬直前配置硬直时间与恢复的目标状态（默认回巡逻）
    /// </summary>
    public void Configure(float duration, IState resumeState = null)
    {
        _duration = Mathf.Max(0f, duration);
        _resumeState = resumeState;
    }

    public void OnEnter()
    {
        _timer = 0f;
        _animator?.Play(_hitHash);
    }

    public void Tick()
    {
        _timer += Time.deltaTime;
        if (_timer < _duration)
        {
            return;
        }
        Debug.Log("硬直结束");
        bool canattack = (_enemyController.transform.position-_enemyController.Target.transform.position).magnitude < _enemyController.SightRange;
        // 优先恢复到指定状态，其次是巡逻状态（如果存在）
        var targetState = canattack? _enemyController.attackState : _enemyController?.patrolState;
        if (targetState != null)
        {
            _enemyController.StateMachine.ChangeState(targetState);
        }
        else
        {
            Debug.LogWarning("硬直结束后没有可恢复的状态（没有设置resumeState，也没有patrolState）");
        }
    }

    public void OnExit()
    {
        _timer = 0f;
    }
}
#endregion