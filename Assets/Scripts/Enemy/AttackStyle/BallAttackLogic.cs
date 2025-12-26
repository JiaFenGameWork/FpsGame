using System;
using UnityEngine;

public class BallAttackLogic : AttackIstate
{
    private Animator animator;
    Transform target;
    public event Action OnAttackStart;
    public event Action OnAttackEnd;
    public event Action<IDamageable> OnHit;
    Transform transform;
    bool attacking = false;
    float rollingDistace = 100f;
    float distance = 0f;
    Vector3 velocity = Vector3.zero;
    // 碰撞检测相关
    private LayerMask obstacleLayerMask;
    private float checkRadius = 0.5f; // 球形检测半径
    private float rollSpeed = 0.04f;
    
    public BallAttackLogic(Transform transform, Transform target, Animator animator, LayerMask obstacleLayerMask, float checkRadius = 0.5f)
    {
        this.transform = transform;
        this.animator = animator;
        this.target = target;
        this.obstacleLayerMask = obstacleLayerMask;
        this.checkRadius = checkRadius;
    }

    public bool IsAttacking => attacking;

    public bool CanStartAttack => !IsAttacking;

    public void Cancel()
    {
        attacking = false;
        velocity = Vector3.zero;
        distance = 0f;
    }

    public void Tick()
    {
        // animator.CrossFade(Animator.StringToHash("anim_open_GoToRoll"), 0.2f);
        if (IsAttacking) {
            // 先检测碰撞，如果碰到东西就停止
            if (CheckHit()) 
            {
                return;
            }
            Vector3 dir = target.position-transform.position;
            dir = dir.normalized;
            dir.y = 0;
            velocity+=dir*rollSpeed*Time.deltaTime;
            distance+=velocity.magnitude;
            if(distance>=rollingDistace)
            {
                OnAttackEnd.Invoke();
                velocity = Vector3.zero;
                distance = 0f;
            }
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
                // 如果没v有 Rigidbody，回退到直接修改位置
                transform.position += velocity;
        }
    }
    
    /// <summary>
    /// 检测碰撞，过滤障碍物和IDamageable物体
    /// </summary>
    /// <returns>是否碰撞到物体</returns>
    private bool CheckHit()
    {
        // 获取所有在检测范围内的碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position+checkRadius*velocity.normalized, checkRadius);
        
        foreach (var hitCollider in hitColliders)
        {
            // 跳过自身
            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
                continue;
            
            // 检查是否是障碍物层
            if (((1 << hitCollider.gameObject.layer) & obstacleLayerMask) != 0)
            {
                // 撞到障碍物，停止攻击并重置状态
                attacking = false;
                velocity = Vector3.zero;
                distance = 0f;
                OnAttackEnd?.Invoke();
                return true;
            }
            
            // 检查是否有IDamageable接口（向上查找父物体）
            IDamageable damageable = hitCollider.gameObject.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // 撞到可伤害物体，停止攻击并触发OnHit，重置状态
                attacking = false;
                velocity = Vector3.zero;
                distance = 0f;
                OnAttackEnd?.Invoke();
                OnHit?.Invoke(damageable);
                return true;
            }
        }
        
        return false;
    }

    public bool TryStartAttack(Transform target)
    {
        if (!IsAttacking) {
            OnAttackStart?.Invoke();
        Vector3 foward = (target.position - transform.position).normalized;
        foward.y = 0;
        transform.rotation = Quaternion.LookRotation(foward);
            attacking = true;
        }
        return true;
    }

}
