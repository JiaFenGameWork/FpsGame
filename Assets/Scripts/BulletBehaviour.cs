using System;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject HitSplash;
    public Transform owner;
    
    // 对象池归还回调
    private Action<GameObject> _returnAction;
    private bool _hasHit = false;  // 防止重复触发

    /// <summary>
    /// 设置归还到对象池的回调
    /// </summary>
    public void SetReturnAction(Action<GameObject> returnAction)
    {
        _returnAction = returnAction;
    }

    /// <summary>
    /// 重置子弹状态（从池中取出时调用）
    /// </summary>
    public void ResetBullet()
    {
        _hasHit = false;
        owner = null;
    }

    /// <summary>
    /// 归还子弹到对象池
    /// </summary>
    private void ReturnToPool()
    {
        if (_hasHit) return;  // 已处理过，跳过
        _hasHit = true;
        
        if (_returnAction != null)
        {
            _returnAction.Invoke(gameObject);
        }
        else
        {
            // 兜底：如果没有设置回调，直接销毁
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;  // 防止重复触发
        
        // 忽略发射者自身的碰撞
        if (owner != null && other.transform.root == owner.root)
        {
            return;
        }
        
        // 检查是否命中可伤害对象
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            Debug.Log("hit");
            damageable.TakeDamage(new AttackData(owner, 10f, false, false));
            SpawnHitEffect();
            ReturnToPool();
            return;
        }

        // 检查是否命中障碍物
        if (other.gameObject.CompareTag("Obstacle"))
        {
            SpawnHitEffect();
            ReturnToPool();
        }
    }

    /// <summary>
    /// 生成命中特效
    /// </summary>
    private void SpawnHitEffect()
    {
        if (HitSplash != null)
        {
            Instantiate(HitSplash, transform.position, transform.rotation);
        }
    }
}
