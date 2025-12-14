using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region 巡逻
public class PatrolState : IState
{
    private EnemyController enemyController;

    // 巡逻点索引
    private int currentPatrolIndex = 0;

    // 当前路径
    public List<Vector3> currentPath;
    private int currentPathIndex = 0;

    // 移动参数
    public float moveSpeed = 3f;
    private float arrivalThreshold = 0.5f;
    private float patrolPointThreshold = 1f;

    // 状态
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

    public PatrolState(EnemyController controller, Animator ani,float speed = 3f)
    {
        this.enemyController = controller;
        this.moveSpeed = speed;
        this._animator = ani;
    }

    public void OnEnter()
    {
        if (enemyController.nav == null)
        {
            Debug.LogError("你需要用Baker烘焙一个octree，然后拖入");
            return;
        }
        _animator.CrossFade(idleHash, 0.1f);
        _active = true;
        // 寻找最近的巡逻点作为起始目标
        currentPatrolIndex = FindNearestPatrolPointIndex();

        // 计算到第一个巡逻点的路径
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
            return;
        // 等待状态
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

        // 移动状态
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            MoveAlongPath();
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

        // 水平移动方向
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
    private void OnReachPatrolPoint()
    {
        isMoving = false;
        isWaiting = true;
        waitTimer = 0f;
        _animator.CrossFade(idleHash, 0.1f);
        Debug.Log($"到达巡逻点 {currentPatrolIndex}");
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
        //Debug.Log($"计算从{start}到{end}的路径");
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
        // --- 核心改动：调用寻路器 ---
        
    }
    public void callback(int id,List<Vector3> path)
    {
        if(!_active)return;
        if (id != _pathRequestId) return;
        _animator.CrossFade(walkHash, 0.1f);
        _isPathPending = false;
        if(path != null && path.Count > 0)
        {
            currentPath = path;
            currentPathIndex = 0;
            isMoving=true;
            isWaiting = false;
            waitTimer = 0f;
            Debug.Log($"路径计算成功: 点数 {currentPath.Count}");
        }
        else
        {
            // 找不到路径时，稍微等待一下再试下一个，防止死循环卡死帧率
            isWaiting = true;
            waitTimer = 0.5f;
            currentPatrolIndex = (currentPatrolIndex + 1) % enemyController.PatrolPoints.Length;
            Debug.Log("找不到啊");
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

#region 攻击

public class AttacoState : IState
{
    public void OnEnter()
    {
        throw new System.NotImplementedException();
    }

    public void OnExit()
    {
        throw new System.NotImplementedException();
    }

    public void Tick()
    {
        throw new System.NotImplementedException();
    }
}
#endregion