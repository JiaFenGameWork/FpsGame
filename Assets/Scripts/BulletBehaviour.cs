using System;
using UnityEngine;
using UnityEngine.VFX;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject HitSplash;
    public Transform owner;
    public float lifeTime = 10f;
    public bool isVFX = false;
    // 对象池归还回调
    private Action<GameObject> _returnAction;
    private bool _hasHit = false;  // 防止重复触发
    private VisualEffect VFX;
    /// <summary>
    /// 设置归还到对象池的回调
    /// </summary>
    public void SetReturn(Action<GameObject> returnAction, float lifeTime)
    {
        _returnAction = returnAction;
        this.lifeTime = lifeTime;
    }
    void Update()
    {
        if(VFX == null && isVFX)
        {
            VFX = GetComponent<VisualEffect>();
        }
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            ReturnToPool();
        }
    }
    /// <summary>
    /// 重置子弹状态（从池中取出时调用）
    /// </summary>
    public void ResetBullet()
    {
        _hasHit = false;
        owner = null;
        if (VFX == null && isVFX)
        {
            VFX = GetComponent<VisualEffect>();
        }
        if (isVFX && VFX != null)
        {
            VFX.SetBool("Alive", true);
            VFX.Reinit();
            VFX.Play();
        }

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
        Debug.Log("Hit: " + other.name);
        // 检查是否命中可伤害对象
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(new AttackData(owner, 10f, false, false));
            if (isVFX && VFX != null)
            {
                VFX.SetBool("Alive", false);
            }
            SpawnHitEffect();
            ReturnToPool();
            return;
        }

        // 检查是否命中障碍物
        if (other.gameObject.CompareTag("Obstacle"))
        {
            if (isVFX && VFX != null)
            {
                VFX.SetBool("Alive", false);
            }
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
            Debug.Log("ggogoogogogoo");
            Instantiate(HitSplash, transform.position, transform.rotation);
        }
    }
}
