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
    
    // 是否由状态内部自动切换到攻击状态（设为false则由控制器统一管理）
    private bool _autoSwitchToAttack = true;

    // --- 卡住检测变量 ---
    private Vector3 _lastStuckCheckPos;
    private float _stuckCheckTimer = 0f;
    private const float STUCK_CHECK_INTERVAL = 1.0f; // 每1秒检测一次
    private const float STUCK_MIN_DIST = 0.2f;       // 1秒内移动少于0.2米视为卡住
    // -------------------

    public PatrolState(BaseEnemyController controller, Animator ani, float speed = 3f, bool autoSwitchToAttack = true)
    {
        this.enemyController = controller;
        this.moveSpeed = speed;
        this._animator = ani;
        this._autoSwitchToAttack = autoSwitchToAttack;
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
        // 自动切换到攻击状态（如果启用）
        if (_autoSwitchToAttack && enemyController.Target != null)
        {
            float dis = (enemyController.transform.position - enemyController.Target.position).magnitude;
            // 使用比 SightRange 更小的距离进入攻击，防止边缘震荡
            if (dis < enemyController.SightRange - _attackHysteresis)
            {
                if (enemyController.attackState != null)
                {
                    enemyController.StateMachine.ChangeState(enemyController.attackState);
                    return;
                }
            }
        }
        // 移动状态
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
        if (currentPath == null || currentPath.Count == 0)
        {
            isMoving = false;
            return;
        }
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
    
    // 是否由状态内部自动切换到攻击状态（设为false则由控制器统一管理）
    private bool _autoSwitchToAttack = true;
    
    public RandomPatrolState(BaseEnemyController enemy, Animator ani, float radius = 10f, float speed = 3f, string walkTrigger = "anim_Walk_Loop", string idleTrigger = "anim_Idle_Loop_S", bool autoSwitchToAttack = true)
    {
        enemyController = enemy;
        _animator = ani;
        _patrolRadius = radius;
        moveSpeed = speed;
        this.walkTrigger = walkTrigger;
        this.idleTrigger = idleTrigger;
        this.walkHash = Animator.StringToHash(walkTrigger);
        this.idleHash = Animator.StringToHash(idleTrigger);
        this._autoSwitchToAttack = autoSwitchToAttack;
    }
    
    public void OnEnter()
    {
        if (enemyController.nav == null)
        {
            Debug.LogError("需要先Baker一个octree，否则不能寻路");
            return;
        }
        
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
        
        // 自动切换到攻击状态（如果启用）
        if (_autoSwitchToAttack && enemyController.Target != null)
        {
            float dis = (enemyController.transform.position - enemyController.Target.position).magnitude;
            if (dis < enemyController.SightRange - _attackHysteresis)
            {
                if (enemyController.attackState != null)
                {
                    enemyController.StateMachine.ChangeState(enemyController.attackState);
                    return;
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
        float height = enemyController.terrain.SampleHeight(_randomTarget);
        _randomTarget.y = height;

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
            Debug.Log($"计算路径到随机目标成功: {enemyController.transform.position} -> {_randomTarget}");
            _animator.CrossFade(walkHash, 0.1f);
            currentPath = path;
            currentPathIndex = 0;
            isMoving = true;
            isWaiting = false;
            waitTimer = 0f;
        }
        else
        {
            Debug.Log($"计算路径到随机目标失败: {enemyController.transform.position} -> {_randomTarget}");
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
        enemyController._animator.SetFloat("Speed",moveSpeed);
        
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

#region 追逐
public class ChaseState : IState
{
    private BaseEnemyController enemyController;
    private Animator _animator;
    
    // 路径相关
    private List<Vector3> currentPath;
    private int currentPathIndex = 0;
    
    // 移动参数
    private float moveSpeed = 5f;
    private float arrivalThreshold = 0.5f;
    
    // 状态
    private bool isMoving = false;
    private bool _isPathPending = false;
    private bool _active = false;
    private int _pathRequestId = 0;
    
    // 路径更新间隔（玩家在移动，需要定期重新计算路径）
    private float _pathUpdateInterval = 0.5f;
    private float _pathUpdateTimer = 0f;
    
    // 动画Hash
    private int _runHash;
    private int _idleHash;
    
    // 是否由状态内部自动切换状态
    private bool _autoSwitchState = true;
    
    // 状态切换滞后量
    private float _attackHysteresis = 1.5f;
    
    // 旋转速度
    private float _rotationSpeed = 10f;
    
    // 上一次目标位置（用于判断是否需要重新寻路）
    private Vector3 _lastTargetPos;
    private float _targetMoveThreshold = 2f; // 目标移动超过此距离才重新寻路

    public ChaseState(
        BaseEnemyController enemy, 
        Animator animator, 
        float speed = 5f, 
        string runAnim = "anim_Run_Loop", 
        string idleAnim = "anim_Idle_Loop_S",
        bool autoSwitchState = true,
        float pathUpdateInterval = 0.5f)
    {
        enemyController = enemy;
        _animator = animator;
        moveSpeed = speed;
        _runHash = Animator.StringToHash(runAnim);
        _idleHash = Animator.StringToHash(idleAnim);
        _autoSwitchState = autoSwitchState;
        _pathUpdateInterval = pathUpdateInterval;
    }

    public void OnEnter()
    {
        if (enemyController.nav == null)
        {
            Debug.LogError("[ChaseState] 需要先Baker一个octree，否则不能寻路");
            return;
        }
        
        Debug.Log($"[ChaseState] OnEnter - 开始追逐玩家");
        _active = true;
        _pathUpdateTimer = 0f;
        currentPath = null;
        currentPathIndex = 0;
        isMoving = false;
        
        // 记录初始目标位置
        if (enemyController.Target != null)
        {
            _lastTargetPos = enemyController.Target.position;
        }
        
        // 播放奔跑动画
        if (_animator != null)
        {
            _animator.CrossFade(_runHash, 0.1f);
        }
        
        // 立即计算路径
        CalculatePathToTarget();
    }

    public void OnExit()
    {
        isMoving = false;
        currentPath = null;
        _active = false;
        
        if (_isPathPending)
        {
            enemyController.CancelPathRequest(_pathRequestId);
        }
        _isPathPending = false;
        
        // 播放待机动画
        if (_animator != null)
        {
            _animator.CrossFade(_idleHash, 0.1f);
        }
        
        Debug.Log("[ChaseState] OnExit - 停止追逐");
    }

    public void Tick()
    {
        if (!_active || enemyController.Target == null) return;
        
        Transform target = enemyController.Target;
        float distanceToTarget = Vector3.Distance(enemyController.transform.position, target.position);
        
        // 自动状态切换逻辑（如果启用）
        if (_autoSwitchState)
        {
            // 检查是否超出视野范围，需要退出追逐
            float exitRange = enemyController.SightRange + _attackHysteresis;
            if (distanceToTarget > exitRange)
            {
                if (enemyController.patrolState != null)
                {
                    enemyController.StateMachine.ChangeState(enemyController.patrolState);
                    return;
                }
            }
            
            // 检查是否进入攻击范围
            if (distanceToTarget < enemyController.AttackRange)
            {
                if (enemyController.attackState != null)
                {
                    enemyController.StateMachine.ChangeState(enemyController.attackState);
                    return;
                }
            }
        }
        
        // 等待寻路完成
        if (_isPathPending) return;
        
        // 定期更新路径（玩家在移动）
        _pathUpdateTimer += Time.deltaTime;
        bool targetMoved = Vector3.Distance(target.position, _lastTargetPos) > _targetMoveThreshold;
        
        if (_pathUpdateTimer >= _pathUpdateInterval && targetMoved)
        {
            _pathUpdateTimer = 0f;
            _lastTargetPos = target.position;
            CalculatePathToTarget();
            return;
        }
        
        // 沿路径移动
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            MoveAlongPath();
        }
        else if (!isMoving && currentPath == null)
        {
            // 没有路径时，尝试重新计算
            CalculatePathToTarget();
        }
    }

    private void CalculatePathToTarget()
    {
        if (!_active || enemyController.Target == null) return;
        
        Vector3 start = enemyController.transform.position;
        Vector3 end = enemyController.Target.position;
        
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
            currentPath = path;
            currentPathIndex = 0;
            isMoving = true;
            
            // 确保播放奔跑动画
            if (_animator != null)
            {
                _animator.CrossFade(_runHash, 0.1f);
            }
        }
        else
        {
            Debug.LogWarning($"[ChaseState] 寻路失败: {enemyController.transform.position} -> {enemyController.Target?.position}");
            // 寻路失败时，稍后重试
            _pathUpdateTimer = _pathUpdateInterval - 0.2f;
        }
    }

    private void MoveAlongPath()
    {
        if (currentPathIndex >= currentPath.Count)
        {
            // 到达路径终点，重新计算路径
            isMoving = false;
            currentPath = null;
            return;
        }
        
        Vector3 targetPos = currentPath[currentPathIndex];
        Vector3 currentPos = enemyController.transform.position;
        Vector3 direction = targetPos - currentPos;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        
        // 到达当前路径点
        if (horizontalDistance < arrivalThreshold)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                // 到达终点
                isMoving = false;
                currentPath = null;
            }
            return;
        }
        
        // 移动
        Vector3 moveDir = direction.normalized;
        moveDir.y = 0;
        Vector3 nextPos = currentPos + moveDir * moveSpeed * Time.deltaTime;
        enemyController.transform.position = nextPos;
        
        // 旋转面向移动方向
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            enemyController.transform.rotation = Quaternion.Slerp(
                enemyController.transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }
    
    /// <summary>
    /// 强制重新计算路径
    /// </summary>
    public void RecalculatePath()
    {
        CalculatePathToTarget();
    }
}
#endregion

#region 射击
public class ShootingState : IState
{
    private BaseEnemyController enemyController;
    private Animator _animator;
    
    // 子弹相关配置
    private GameObject bulletPrefab;
    // 兼容旧的单枪管字段（如果未提供多枪管数组，会回退使用它）
    private Transform bulletSpawnPoint;
    // 多枪管：枪口列表（可由动画事件指定索引；若不指定可轮转/回退）
    private Transform[] bulletSpawnPoints;
    private int _spawnPointIndex = 0;
    private float bulletSpeed = 10f;
    private float bulletLifeTime = 3f;
    private float bulletDamage = 10f;
    private float bulletInterval = 0.5f;
    
    // 是否由动画事件控制射击时机（动作片段事件触发），而不是用 Tick 的定时器
    private bool _useAnimationEventTiming = false;
    private float _lastShootTime = -999f;
    
    // 对象池
    private GameObjectPool _bulletPool;
    
    // 计时器
    private float _shootTimer = 0f;
    private bool _isActive = false;
    
    // 动画Hash
    private int _shootHash;
    private int _idleHash;
    
    // 状态切换滞后量，防止边缘震荡
    private float _exitHysteresis = 1.5f;
    
    // 旋转速度
    private float _rotationSpeed = 10f;

    public ShootingState(
        BaseEnemyController enemy, 
        Animator animator,
        GameObject bulletPrefab, 
        Transform bulletSpawnPoint, 
        float bulletSpeed = 10f, 
        float bulletLifeTime = 3f, 
        float bulletDamage = 10f, 
        float bulletInterval = 0.5f,
        string shootAnim = "anim_Shoot",
        string idleAnim = "anim_Idle_Loop_S")
    {
        this.enemyController = enemy;
        this._animator = animator;
        this.bulletPrefab = bulletPrefab;
        this.bulletSpawnPoint = bulletSpawnPoint;
        this.bulletSpawnPoints = (bulletSpawnPoint != null) ? new[] { bulletSpawnPoint } : null;
        this.bulletSpeed = bulletSpeed;
        this.bulletLifeTime = bulletLifeTime;
        this.bulletDamage = bulletDamage;
        this.bulletInterval = bulletInterval;
        
        _shootHash = Animator.StringToHash(shootAnim);
        _idleHash = Animator.StringToHash(idleAnim);
        
        // 初始化子弹对象池
        if (bulletPrefab != null)
        {
            _bulletPool = new GameObjectPool(
                bulletPrefab, 
                initialSize: 10, 
                parent: null,
                onGet: (go) => {
                    var bullet = go.GetComponent<BulletBehaviour>();
                    bullet?.ResetBullet();
                },
                onReturn: null
            );
        }
    }

    /// <summary>
    /// 多枪管构造：支持在一个敌人上配置多个枪口（Transform），每次射击轮转使用。
    /// 可选由动画事件驱动射击时机（_useAnimationEventTiming=true）。
    /// </summary>
    public ShootingState(
        BaseEnemyController enemy,
        Animator animator,
        GameObject bulletPrefab,
        Transform[] bulletSpawnPoints,
        float bulletSpeed = 10f,
        float bulletLifeTime = 3f,
        float bulletDamage = 10f,
        float bulletInterval = 0.5f,
        string shootAnim = "anim_Shoot",
        string idleAnim = "anim_Idle_Loop_S",
        bool useAnimationEventTiming = false)
    {
        this.enemyController = enemy;
        this._animator = animator;
        this.bulletPrefab = bulletPrefab;
        this.bulletSpawnPoints = bulletSpawnPoints;
        this.bulletSpawnPoint = (bulletSpawnPoints != null && bulletSpawnPoints.Length > 0) ? bulletSpawnPoints[0] : null;
        this.bulletSpeed = bulletSpeed;
        this.bulletLifeTime = bulletLifeTime;
        this.bulletDamage = bulletDamage;
        this.bulletInterval = bulletInterval;
        this._useAnimationEventTiming = useAnimationEventTiming;

        _shootHash = Animator.StringToHash(shootAnim);
        _idleHash = Animator.StringToHash(idleAnim);

        // 初始化子弹对象池
        if (bulletPrefab != null)
        {
            _bulletPool = new GameObjectPool(
                bulletPrefab,
                initialSize: 10,
                parent: null,
                onGet: (go) => {
                    var bullet = go.GetComponent<BulletBehaviour>();
                    bullet?.ResetBullet();
                },
                onReturn: null
            );
        }
    }

    public void OnEnter()
    {
        _isActive = true;
        _shootTimer = 0f; // 立即开始射击
        
        // 播放射击动画
        if (_animator != null)
        {
            _animator.CrossFade(_shootHash, 0.1f);
        }
        
        Debug.Log("[ShootingState] OnEnter - 进入射击状态");
    }

  
    public void Tick()
    {
        if (!_isActive || enemyController == null || enemyController.Target == null)
            return;
        
        Transform target = enemyController.Target;
        Vector3 directionToTarget = target.position - enemyController.transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        // 检查是否超出视野范围，需要退出攻击状态
        float exitRange = enemyController.SightRange + _exitHysteresis;
        if (distanceToTarget > exitRange)
        {
            // 切换回巡逻状态
            if (enemyController.patrolState != null)
            {
                enemyController.StateMachine.ChangeState(enemyController.patrolState);
            }
            return;
        }
        
        // 面向目标旋转
        FaceTarget(directionToTarget);

        // 如果由动画事件控制射击时机，这里不再用定时器开火
        if (_useAnimationEventTiming) return;

        // 射击计时（旧逻辑：按间隔自动开火）
        _shootTimer += Time.deltaTime;
        if (_shootTimer >= bulletInterval)
        {
            _shootTimer = 0f;
            ShootOnce();
        }
    }

    /// <summary>
    /// 面向目标旋转
    /// </summary>
    private void FaceTarget(Vector3 direction)
    {
        // 只在水平面上旋转
        direction.y = 0;
        
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemyController.transform.rotation = Quaternion.Slerp(
                enemyController.transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    /// <summary>
    /// 供动画事件调用：按动作片段的射击时机触发开火（可传入连发次数）。
    /// 注意：动画事件函数本身必须在 MonoBehaviour 上（见 BaseEnemyController.AE_Shoot），这里是状态内部的处理。
    /// </summary>
    public void RequestShootFromAnimationEvent(int count = 1)
    {
        if (!_isActive) return;
        if (!_useAnimationEventTiming) _useAnimationEventTiming = true; // 一旦使用事件驱动，自动切换到事件模式

        // 防抖：避免同一帧/极短间隔被重复调用导致射速异常
        // 这里用 bulletInterval 作为最小间隔基准（允许更高频可自行调小 bulletInterval）
        if (Time.time - _lastShootTime < Mathf.Max(0.01f, bulletInterval * 0.1f))
            return;

        _lastShootTime = Time.time;
        int n = Mathf.Max(1, count);
        for (int i = 0; i < n; i++)
        {
            ShootOnce();
        }
    }

    /// <summary>
    /// 供动画事件调用：指定枪口索引开火（muzzleIndex 对应 EnemyController 上 bulletSpawnPoints 的数组下标）。
    /// </summary>
    public void RequestShootFromAnimationEventMuzzle(int muzzleIndex)
    {
        if (!_isActive) return;
        if (!_useAnimationEventTiming) _useAnimationEventTiming = true;

        // 防抖：避免同一帧/极短间隔被重复调用导致射速异常
        if (Time.time - _lastShootTime < Mathf.Max(0.01f, bulletInterval * 0.1f))
            return;

        _lastShootTime = Time.time;
        ShootOnce(muzzleIndex);
    }

    /// <summary>
    /// 发射 1 发子弹（多枪管轮转取枪口）
    /// </summary>
    private void ShootOnce()
    {
        Transform spawn = GetNextSpawnPoint();
        ShootOnce(spawn);
    }

    /// <summary>
    /// 指定枪口索引发射 1 发；索引无效/枪口为空则回退到默认逻辑。
    /// </summary>
    private void ShootOnce(int muzzleIndex)
    {
        Transform spawn = GetSpawnPointByIndex(muzzleIndex) ?? GetNextSpawnPoint();
        ShootOnce(spawn);
    }

    private void ShootOnce(Transform spawn)
    {
        if (spawn == null || enemyController.Target == null)
            return;
        
        // 计算射击方向（朝向玩家）
        Vector3 targetPosition = enemyController.Target.position;
        // 瞄准目标中心偏上位置（更自然的瞄准点）
        targetPosition.y += 1.0f;
        Vector3 shootDirection = (targetPosition - spawn.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDirection);
        
        // 从对象池获取子弹
        GameObject bulletGO = null;
        if (_bulletPool != null)
        {
            bulletGO = _bulletPool.Get(spawn.position, bulletRotation);
        }
        else
        {
            // 如果没有对象池，直接实例化
            bulletGO = UnityEngine.Object.Instantiate(bulletPrefab, spawn.position, bulletRotation);
        }
        
        if (bulletGO == null) return;
        
        // 设置子弹属性
        BulletBehaviour bullet = bulletGO.GetComponent<BulletBehaviour>();
        if (bullet != null)
        {
            bullet.owner = enemyController.transform;
            bullet.SetReturn((go) => {
                if (_bulletPool != null)
                {
                    _bulletPool.Return(go);
                }
                else
                {
                    UnityEngine.Object.Destroy(go);
                }
            }, bulletLifeTime);
        }
        
        // 给子弹添加速度（使用Rigidbody）
        Rigidbody rb = bulletGO.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = shootDirection * bulletSpeed;
        }
        else
        {
            // 如果没有Rigidbody，可以添加一个简单的移动脚本或者使用transform移动
            // 这里假设子弹有自己的移动逻辑，或者添加一个临时的移动组件
            var mover = bulletGO.GetComponent<BulletMover>();
            if (mover == null)
            {
                mover = bulletGO.AddComponent<BulletMover>();
            }
            mover.SetDirection(shootDirection, bulletSpeed);
        }
        
        Debug.Log($"[ShootingState] 发射子弹 -> 枪口: {(spawn != null ? spawn.name : "null")} -> 目标: {enemyController.Target.name}");
    }

    private Transform GetNextSpawnPoint()
    {
        // 优先用多枪管列表
        if (bulletSpawnPoints != null && bulletSpawnPoints.Length > 0)
        {
            // 找一个非空的枪口（最多尝试 Length 次）
            for (int i = 0; i < bulletSpawnPoints.Length; i++)
            {
                int idx = (_spawnPointIndex + i) % bulletSpawnPoints.Length;
                var sp = bulletSpawnPoints[idx];
                if (sp != null)
                {
                    _spawnPointIndex = (idx + 1) % bulletSpawnPoints.Length;
                    return sp;
                }
            }
        }

        // 回退到单枪口
        return bulletSpawnPoint;
    }

    private Transform GetSpawnPointByIndex(int muzzleIndex)
    {
        if (bulletSpawnPoints == null || bulletSpawnPoints.Length == 0) return null;
        if (muzzleIndex < 0 || muzzleIndex >= bulletSpawnPoints.Length) return null;
        return bulletSpawnPoints[muzzleIndex];
    }

    public void OnExit()
    {
        _isActive = false;
        _shootTimer = 0f;
        
        // 播放待机动画
        if (_animator != null)
        {
            _animator.CrossFade(_idleHash, 0.1f);
        }
        
        Debug.Log("[ShootingState] OnExit - 退出射击状态");
    }
}

/// <summary>
/// 简单的子弹移动组件（当子弹没有Rigidbody时使用）
/// </summary>
public class BulletMover : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    
    public void SetDirection(Vector3 direction, float speed)
    {
        _direction = direction.normalized;
        _speed = speed;
    }
    
    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }
}
#endregion
#region Boss出场
public class BossstartState : IState,IAnimationEventReceiver
{
    private readonly BaseEnemyController _enemyController;
    private readonly Animator _animator;
    private readonly int _startAnimHash;
    private readonly string _eventKey;
    private readonly int _eventInt;
    private readonly float _crossFade;
    private bool _hasSwitched;

    public BossstartState(
        BaseEnemyController enemy,
        Animator animator,
        string startAnim = "attack",
        float crossFade = 0.1f)
    {
        _enemyController = enemy;
        _animator = animator;
        _startAnimHash = Animator.StringToHash(startAnim);
        _crossFade = Mathf.Max(0f, crossFade);
    }

    public void OnEnter()
    {
        _hasSwitched = false;

    }
     public void OnAnimationEvent(string key)
    {
        if(key =="zzboss_start")
        {
            _enemyController.StateMachine.ChangeState(_enemyController.ChaseState);
        }
    }

    public void OnAnimationEventInt(int value)
    {

    }
    public void Tick()
    {
        float distance = Vector3.Distance(_enemyController.transform.position, _enemyController.Target.position);
        if (_animator != null && !_hasSwitched && distance < _enemyController.SightRange)
        {
            _animator.CrossFade(_startAnimHash, _crossFade);
            _hasSwitched = true;
        }
        // 出场动作期间不做逻辑，等待动画事件回调切换
    }

    public void OnExit()
    {
        _hasSwitched = false;
    }


}
#endregion
#region Boss攻击状态
/// <summary>
/// Boss通用攻击状态
/// 根据距离和玩家位置选择不同的攻击动作
/// 伤害判定由动画事件驱动
/// </summary>
public class BossAttackState : IState, IAnimationEventReceiver
{
    /// <summary>
    /// 攻击类型枚举
    /// </summary>
    public enum AttackType
    {
        None,
        MeleeCombo1,      // 近战一连击
        MeleeCombo2,      // 近战二连击+开炮
        ShootUp,          // 向上开炮（AOE）
        ShootNormal,      // 向玩家普通射击
        JumpTurn,         // 被绕后跳起转身
    }

    #region 配置参数
    private readonly BaseEnemyController _enemyController;
    private readonly Animator _animator;
    
    // 攻击距离阈值
    private float _meleeRange = 3f;           // 近战范围
    private float _midRange = 8f;             // 中距离范围（普通射击）
    private float _farRange = 15f;            // 远距离范围（向上开炮）
    
    // 背后检测参数
    private float _behindAngleThreshold = 120f;  // 判定为绕后的角度阈值（背后120度范围）
    
    // 攻击冷却
    private float _attackCooldown = 0.5f;     // 攻击间隔
    private float _lastAttackTime = -999f;
    
    // 旋转速度
    private float _rotationSpeed = 5f;
    
    // 动画Hash缓存
    private int _meleeCombo1Hash;
    private int _meleeCombo2Hash;
    private int _shootUpHash;
    private int _shootNormalHash;
    private int _jumpTurnHash;
    private int _idleHash;
    
    // 子弹相关（射击用）
    private GameObject _bulletPrefab;
    private Transform[] _bulletSpawnPoints;
    private float _bulletSpeed = 15f;
    private float _bulletLifeTime = 3f;
    private float _bulletDamage = 10f;
    private GameObjectPool _bulletPool;
    private int _spawnPointIndex = 0;
    
    // AOE相关
    private float _aoeDamage = 20f;
    private float _aoeRadius = 5f;
    private LayerMask _playerLayer;
    
    // 近战伤害
    private float _meleeDamage = 15f;
    private float _meleeCombo2Damage = 25f;
    
    // 近战碰撞体检测（由外部设置）
    private Collider[] _meleeHitColliders;  // 用于近战伤害检测的碰撞体（手、武器等）
    #endregion

    #region 状态变量
    private bool _isActive = false;
    private AttackType _currentAttack = AttackType.None;
    private bool _isAttacking = false;        // 是否正在执行攻击动画
    private bool _canDealDamage = false;      // 伤害判定窗口是否开启
    private HashSet<IDamageable> _hitTargets; // 当前攻击已命中的目标（防止重复伤害）
    private float _exitHysteresis = 2f;       // 状态切换滞后量
    #endregion

    #region 构造函数
    /// <summary>
    /// Boss攻击状态构造函数
    /// </summary>
    /// <param name="enemy">敌人控制器</param>
    /// <param name="animator">动画控制器</param>
    /// <param name="config">可选配置</param>
    public BossAttackState(BaseEnemyController enemy, Animator animator, BossAttackConfig config = null)
    {
        _enemyController = enemy;
        _animator = animator;
        _hitTargets = new HashSet<IDamageable>();
        
        // 应用配置
        if (config != null)
        {
            ApplyConfig(config);
        }
        
        // 初始化动画Hash（使用默认值，可通过配置覆盖）
        InitializeAnimationHashes(config);
        
        // 初始化子弹对象池
        if (_bulletPrefab != null)
        {
            _bulletPool = new GameObjectPool(
                _bulletPrefab,
                initialSize: 10,
                parent: null,
                onGet: (go) => {
                    var bullet = go.GetComponent<BulletBehaviour>();
                    bullet?.ResetBullet();
                },
                onReturn: null
            );
        }
    }

    private void ApplyConfig(BossAttackConfig config)
    {
        _meleeRange = config.meleeRange;
        _midRange = config.midRange;
        _farRange = config.farRange;
        _behindAngleThreshold = config.behindAngleThreshold;
        _attackCooldown = config.attackCooldown;
        _rotationSpeed = config.rotationSpeed;
        
        _bulletPrefab = config.bulletPrefab;
        _bulletSpawnPoints = config.bulletSpawnPoints;
        _bulletSpeed = config.bulletSpeed;
        _bulletLifeTime = config.bulletLifeTime;
        _bulletDamage = config.bulletDamage;
        
        _aoeDamage = config.aoeDamage;
        _aoeRadius = config.aoeRadius;
        _playerLayer = config.playerLayer;
        
        _meleeDamage = config.meleeDamage;
        _meleeCombo2Damage = config.meleeCombo2Damage;
        _meleeHitColliders = config.meleeHitColliders;
    }

    private void InitializeAnimationHashes(BossAttackConfig config)
    {
        string meleeCombo1Anim = config?.meleeCombo1Anim ?? "attack_melee1";
        string meleeCombo2Anim = config?.meleeCombo2Anim ?? "attack_melee2";
        string shootUpAnim = config?.shootUpAnim ?? "attack_shoot_up";
        string shootNormalAnim = config?.shootNormalAnim ?? "attack_shoot";
        string jumpTurnAnim = config?.jumpTurnAnim ?? "attack_jump_turn";
        string idleAnim = config?.idleAnim ?? "idle";
        
        _meleeCombo1Hash = Animator.StringToHash(meleeCombo1Anim);
        _meleeCombo2Hash = Animator.StringToHash(meleeCombo2Anim);
        _shootUpHash = Animator.StringToHash(shootUpAnim);
        _shootNormalHash = Animator.StringToHash(shootNormalAnim);
        _jumpTurnHash = Animator.StringToHash(jumpTurnAnim);
        _idleHash = Animator.StringToHash(idleAnim);
    }
    #endregion

    #region IState 实现
    public void OnEnter()
    {
        _isActive = true;
        _isAttacking = false;
        _canDealDamage = false;
        _currentAttack = AttackType.None;
        _hitTargets.Clear();
        
        Debug.Log("[BossAttackState] OnEnter - 进入Boss攻击状态");
    }

    public void OnExit()
    {
        _isActive = false;
        _isAttacking = false;
        _canDealDamage = false;
        _currentAttack = AttackType.None;
        _hitTargets.Clear();
        
        if (_animator != null)
        {
            _animator.CrossFade(_idleHash, 0.1f);
        }
        
        Debug.Log("[BossAttackState] OnExit - 退出Boss攻击状态");
    }

    public void Tick()
    {
        if (!_isActive || _enemyController == null || _enemyController.Target == null)
            return;
        
        Transform target = _enemyController.Target;
        Vector3 toTarget = target.position - _enemyController.transform.position;
        float distance = toTarget.magnitude;
        
        // 检查是否超出攻击范围，需要切换状态
        if (distance > _farRange + _exitHysteresis)
        {
            // 切换到追逐状态
            if (_enemyController.ChaseState != null)
            {
                _enemyController.StateMachine.ChangeState(_enemyController.ChaseState);
                return;
            }
            else if (_enemyController.patrolState != null)
            {
                _enemyController.StateMachine.ChangeState(_enemyController.patrolState);
                return;
            }
        }
        // 如果不在攻击动画中，选择下一个攻击
        if (!_isAttacking)
        {
            // 检查攻击冷却
            if (Time.time - _lastAttackTime < _attackCooldown)
            {
                // 冷却期间面向目标
                return;
            }
            FaceTarget(toTarget);
            // 选择攻击类型
            AttackType attackToUse = SelectAttackType(distance, toTarget);
            if (attackToUse != AttackType.None)
            {
                StartAttack(attackToUse);
            }
        }
        
        // 根据攻击类型处理旋转
        if (_isAttacking)
        {
            // 射击类攻击需要持续面向目标
            if (_currentAttack == AttackType.ShootNormal || _currentAttack == AttackType.ShootUp)
            {
                FaceTarget(toTarget);
            }
            
            // 近战伤害检测（在伤害窗口内）
            if (_canDealDamage && (_currentAttack == AttackType.MeleeCombo1 || 
                                   _currentAttack == AttackType.MeleeCombo2))
            {
                CheckMeleeHits();
            }
        }
    }
    #endregion

    #region 攻击选择逻辑
    /// <summary>
    /// 根据距离和位置选择攻击类型
    /// </summary>
    private AttackType SelectAttackType(float distance, Vector3 toTarget)
    {
        // 优先检测是否被绕后
        if (IsPlayerBehind(toTarget))
        {
            return AttackType.JumpTurn;
        }
        
        // 根据距离选择攻击
        if (distance <= _meleeRange)
        {
            // 近战范围：随机选择一连击或二连击
            return UnityEngine.Random.value > 0.5f ? AttackType.MeleeCombo1 : AttackType.MeleeCombo2;
        }
        else if (distance <= _midRange)
        {
            // 中距离：普通射击
            return AttackType.ShootNormal;
        }
        else if (distance <= _farRange)
        {
            // 远距离：向上开炮（AOE）或普通射击
            return UnityEngine.Random.value > 0.4f ? AttackType.ShootUp : AttackType.ShootNormal;
        }
        
        return AttackType.None;
    }

    /// <summary>
    /// 检测玩家是否在Boss背后
    /// </summary>
    private bool IsPlayerBehind(Vector3 toTarget)
    {
        Vector3 forward = _enemyController.transform.forward;
        toTarget.y = 0;
        forward.y = 0;
        
        float angle = Vector3.Angle(forward, toTarget);
        return angle > _behindAngleThreshold;
    }
    #endregion

    #region 攻击执行
    /// <summary>
    /// 开始执行攻击
    /// </summary>
    private void StartAttack(AttackType attackType)
    {
        _currentAttack = attackType;
        _isAttacking = true;
        _canDealDamage = false;
        _hitTargets.Clear();
        _lastAttackTime = Time.time;
        
        int animHash = GetAnimationHash(attackType);
        if (_animator != null && animHash != 0)
        {
            // 获取当前动画状态信息用于调试
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            bool isInTransition = _animator.IsInTransition(0);
            
            Debug.Log($"[BossAttackState] 开始攻击: {attackType}, " +
                      $"当前状态Hash: {stateInfo.shortNameHash}, " +
                      $"normalizedTime: {stateInfo.normalizedTime:F2}, " +
                      $"正在过渡: {isInTransition}");
            
            // 使用 CrossFadeInFixedTime 替代 CrossFade
            // CrossFadeInFixedTime 使用绝对时间（秒）而不是归一化时间，更可靠
            // 第三个参数 layer=0，第四个参数 fixedTime=0 表示从动画开头开始播放
            _animator.CrossFadeInFixedTime(animHash, 0.1f, 0, 0f);
        }
        else
        {
            Debug.LogWarning($"[BossAttackState] 无法播放动画: animator={_animator != null}, animHash={animHash}");
        }
    }

    private int GetAnimationHash(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.MeleeCombo1: return _meleeCombo1Hash;
            case AttackType.MeleeCombo2: return _meleeCombo2Hash;
            case AttackType.ShootUp: return _shootUpHash;
            case AttackType.ShootNormal: return _shootNormalHash;
            case AttackType.JumpTurn: return _jumpTurnHash;
            default: return _idleHash;
        }
    }

    /// <summary>
    /// 面向目标旋转
    /// </summary>
    private void FaceTarget(Vector3 direction)
    {
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _enemyController.transform.rotation = Quaternion.Slerp(
                _enemyController.transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }
    #endregion

    #region 伤害处理
    /// <summary>
    /// 检测近战命中（碰撞体检测）
    /// </summary>
    private void CheckMeleeHits()
    {
        if (_meleeHitColliders == null || _meleeHitColliders.Length == 0)
            return;
        
        float damage = _currentAttack == AttackType.MeleeCombo2 ? _meleeCombo2Damage : _meleeDamage;
        
        foreach (var hitCollider in _meleeHitColliders)
        {
            if (hitCollider == null) continue;
            
            // 获取碰撞体范围内的所有碰撞
            Collider[] overlaps = Physics.OverlapBox(
                hitCollider.bounds.center,
                hitCollider.bounds.extents,
                hitCollider.transform.rotation,
                _playerLayer
            );
            
            foreach (var overlap in overlaps)
            {
                IDamageable damageable = overlap.GetComponent<IDamageable>();
                if (damageable != null && !_hitTargets.Contains(damageable))
                {
                    _hitTargets.Add(damageable);
                    damageable.TakeDamage(new AttackData(
                        _enemyController.transform,
                        damage,
                        false,
                        true
                    ));
                    Debug.Log($"[BossAttackState] 近战命中: {overlap.name}, 伤害: {damage}");
                }
            }
        }
    }

    /// <summary>
    /// 执行AOE伤害（向上开炮落地时触发）
    /// </summary>
    private void ExecuteAoeDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, _aoeRadius, _playerLayer);
        
        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new AttackData(
                    _enemyController.transform,
                    _aoeDamage,
                    false,
                    true
                ));
                Debug.Log($"[BossAttackState] AOE命中: {hit.name}, 伤害: {_aoeDamage}");
            }
        }
    }
   
    #endregion

    #region IAnimationEventReceiver 实现
    /// <summary>
    /// 接收动画事件（string参数）
    /// 支持的事件key:
    /// - "HitStart": 开启伤害判定窗口
    /// - "HitEnd": 关闭伤害判定窗口
    /// - "AttackEnd": 攻击动画结束
    /// - "Shoot": 发射子弹
    /// - "Shoot:N": 发射N发子弹
    /// - "ShootMuzzle:N": 从第N个枪口发射
    /// - "ShootUp": 向上开炮
    /// - "AOE": 触发AOE伤害
    /// - "AOE:X,Y,Z": 在指定位置触发AOE
    /// </summary>
    public void OnAnimationEvent(string key)
    {
        if (!_isActive) return;
        
        Debug.Log($"[BossAttackState] 收到动画事件: {key}");
        
        // 解析带参数的事件
        string[] parts = key.Split(':');
        string eventName = parts[0];
        string eventParam = parts.Length > 1 ? parts[1] : "";
        
        switch (eventName)
        {
            case "HitStart":
                _canDealDamage = true;
                _hitTargets.Clear();
                break;
                
            case "HitEnd":
                _canDealDamage = false;
                break;
                
            case "AttackEnd":
                _isAttacking = false;
                _canDealDamage = false;
                _currentAttack = AttackType.None;
                _lastAttackTime = Time.time;
                break;
                
            case "Shoot":
                AttackStyle.ShootBullet(_enemyController.Target, _bulletSpawnPoints, _bulletPrefab, _bulletPool, _bulletSpeed, _bulletLifeTime);
                break;
                
            case "ShootMuzzle":
                int muzzleIdx = 0;
                if (!string.IsNullOrEmpty(eventParam))
                    int.TryParse(eventParam, out muzzleIdx);
                break;
                
            case "ShootUp":
                break;
                
            case "AOE":
                if (!string.IsNullOrEmpty(eventParam))
                {
                    // 解析位置参数 "X,Y,Z"
                    string[] posStrs = eventParam.Split(',');
                    if (posStrs.Length == 3)
                    {
                        Vector3 aoePos = new Vector3(
                            float.Parse(posStrs[0]),
                            float.Parse(posStrs[1]),
                            float.Parse(posStrs[2])
                        );
                        // 转换为世界坐标（相对于Boss位置）
                        aoePos = _enemyController.transform.TransformPoint(aoePos);
                        ExecuteAoeDamage(aoePos);
                    }
                }
                else
                {
                    // 默认在Boss前方触发AOE
                    Vector3 aoeCenter = _enemyController.transform.position + 
                                        _enemyController.transform.forward * _aoeRadius;
                    ExecuteAoeDamage(aoeCenter);
                }
                break;
        }
    }

    /// <summary>
    /// 接收动画事件（int参数）
    /// 0: 攻击结束
    /// 1: 开启伤害判定
    /// 2: 关闭伤害判定
    /// 3: 发射子弹
    /// 4: 向上开炮
    /// 5: 触发AOE
    /// 10+N: 从第N个枪口发射
    /// </summary>
    public void OnAnimationEventInt(int value)
    {
        if (!_isActive) return;
        
        Debug.Log($"[BossAttackState] 收到动画事件(int): {value}");
        
        switch (value)
        {
            case 0:
                _isAttacking = false;
                _canDealDamage = false;
                _currentAttack = AttackType.None;
                break;
            case 1:
                _canDealDamage = true;
                _hitTargets.Clear();
                break;
            case 2:
                _canDealDamage = false;
                break;
            case 3:
                AttackStyle.ShootBullet(_enemyController.Target, _bulletSpawnPoints, _bulletPrefab, _bulletPool, _bulletSpeed, _bulletLifeTime);
                break;
            case 4:
                AttackStyle.ShootUpBullet(_enemyController.Target, _bulletSpawnPoints[0], _bulletPrefab, _bulletPool, _bulletSpeed, _bulletLifeTime);
                break;
            case 5:
                Vector3 aoeCenter = _enemyController.transform.position + 
                                    _enemyController.transform.forward * _aoeRadius;
                ExecuteAoeDamage(aoeCenter);
                break;
            default:
                // 10+N: 从第N个枪口发射
                if (value >= 10)
                {
                    AttackStyle.ShootBullet(_enemyController.Target, _bulletSpawnPoints, _bulletPrefab, _bulletPool, _bulletSpeed, _bulletLifeTime);
                }
                break;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 强制切换攻击类型（可由外部调用）
    /// </summary>
    public void ForceAttack(AttackType attackType)
    {
        if (_isActive && !_isAttacking)
        {
            StartAttack(attackType);
        }
    }

    /// <summary>
    /// 设置近战检测碰撞体
    /// </summary>
    public void SetMeleeHitColliders(Collider[] colliders)
    {
        _meleeHitColliders = colliders;
    }

    /// <summary>
    /// 设置子弹发射点
    /// </summary>
    public void SetBulletSpawnPoints(Transform[] spawnPoints)
    {
        _bulletSpawnPoints = spawnPoints;
    }
    
    /// <summary>
    /// 获取当前攻击类型
    /// </summary>
    public AttackType CurrentAttackType => _currentAttack;
    
    /// <summary>
    /// 是否正在攻击中
    /// </summary>
    public bool IsAttacking => _isAttacking;
    #endregion
}
#endregion