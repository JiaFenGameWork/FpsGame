using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{
    public Camera playerCamera;
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 0.6f;
    public int bulletsPerBurst = 3;
    public int burstBulletLeft;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 3f;
    public GameObjectPool bulletPool;
    Animator animator;
    string FireAnimationName = "Shot";
    [Header("射线检测设置")]
    public LayerMask aimLayerMask = ~0;
    
    [Header("后坐力设置")]
    public float recoilAmount = 2f;        // 后坐力向上的角度
    public float recoilSpeed = 10f;        // 后坐力上升速度
    public float returnSpeed = 5f;         // 恢复速度

    // 子弹活动协程追踪，避免重复归还
    private Dictionary<GameObject, Coroutine> _bulletCoroutines = new Dictionary<GameObject, Coroutine>();

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode CurrentShootingMode;
    private MouseMovementBehaviour mouseMovement;
    
    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;
        animator = GetComponent<Animator>();
        mouseMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<MouseMovementBehaviour>();
        // 初始化对象池，带回调处理
        bulletPool = new GameObjectPool(
            bulletPrefab, 
            bulletsPerBurst * 2,  // 预热多一些，避免频繁扩容
            null,
            OnBulletGet,    // 获取时重置状态
            OnBulletReturn  // 归还时清理
        );
        
        // 强制排除 Player 层
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            aimLayerMask &= ~(1 << playerLayer);
        }
    }

    /// <summary>
    /// 子弹从池中取出时的回调 - 重置物理状态
    /// </summary>
    private void OnBulletGet(GameObject bullet)
    {
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 重置子弹组件状态
        var bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
        if (bulletBehaviour != null)
        {
            bulletBehaviour.ResetBullet();
        }
    }

    /// <summary>
    /// 子弹归还到池时的回调 - 清理协程追踪
    /// </summary>
    private void OnBulletReturn(GameObject bullet)
    {
        // 移除协程追踪
        _bulletCoroutines.Remove(bullet);
    }

    void Update()
    {
        if (CurrentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (CurrentShootingMode == ShootingMode.Single || CurrentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        if (readyToShoot && isShooting)
        {
            burstBulletLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        readyToShoot = false;
        animator.Play(FireAnimationName);   
        
        // 计算射击方向
        Vector3 shootingDirection = CalcuateDirectionAndSpread().normalized;
        // 修复：如果计算出的方向为零向量（例如射线击中点正好在枪口），LookRotation会失败导致子弹无法生成或方向错误
        if (shootingDirection == Vector3.zero)
            shootingDirection = transform.forward;
        
        // 应用后坐力
        ApplyRecoil();

        // 从池中获取子弹（已在回调中重置状态）
        // 注意：如果 bulletSpawn 位置在墙体或障碍物内部，子弹可能会在生成瞬间触发碰撞并被立即销毁（现象为有动画无子弹）
        GameObject bullet = bulletPool.Get(bulletSpawn.position, Quaternion.LookRotation(shootingDirection));
        
        // 修复：确保清理旧的协程记录（防止对象池复用时，旧的协程仍在引用该子弹）
        if (_bulletCoroutines.ContainsKey(bullet))
        {
            if (_bulletCoroutines[bullet] != null) StopCoroutine(_bulletCoroutines[bullet]);
            _bulletCoroutines.Remove(bullet);
        }

        // 设置子弹属性
        var bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
        if (bulletBehaviour != null)
        {
            bulletBehaviour.owner = transform;
            bulletBehaviour.SetReturnAction(ReturnBullet);  // 注入归还方法
        }
        
        // 发射
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        }
        
        // 启动生命周期协程，并追踪
        var coroutine = StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));
        _bulletCoroutines[bullet] = coroutine;
        
        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        if (CurrentShootingMode == ShootingMode.Burst && burstBulletLeft > 1)
        {
            burstBulletLeft--;
            Invoke("FireWeapon", shootingDelay);
        }
    }

    /// <summary>
    /// 安全归还子弹（防止重复归还）
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null || !bullet.activeInHierarchy) return;
        
        // 停止该子弹的生命周期协程
        if (_bulletCoroutines.TryGetValue(bullet, out var coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        
        bulletPool.Return(bullet);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 超时自动归还
        if (bullet != null && bullet.activeInHierarchy)
        {
            bulletPool.Return(bullet);
        }
    }
    /// <summary>
    /// 应用后坐力 - 在开火时调用
    /// </summary>
    private void ApplyRecoil()
    {
        if (mouseMovement != null)
        {
            mouseMovement.recoilSpeed = recoilSpeed;
            mouseMovement.AddRecoil(recoilAmount);
        }
    }

    /// <summary>
    /// 在 LateUpdate 中处理后坐力和恢复
    /// </summary>
    private void LateUpdate()
    {
        // Deprecated: Recoil handled by MouseMovementBehaviour
    }
    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }
    
    public Vector3 CalcuateDirectionAndSpread()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetpoint;
        
        // 调试：显示 LayerMask 值
        Debug.Log($"LayerMask value: {aimLayerMask.value}, Player Layer: {LayerMask.NameToLayer("Player")}");
        
        // 使用 LayerMask 来忽略玩家层
        if (Physics.Raycast(ray, out hit, 1000f, aimLayerMask))
        {
            // 调试：显示击中对象的层
            Debug.Log($"Hit: {hit.collider.name}, Layer: {hit.collider.gameObject.layer} ({LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            targetpoint = hit.point;
        }
        else
        {
            targetpoint = ray.GetPoint(1000f);
        }
        
        Vector3 direction = (targetpoint - bulletSpawn.position).normalized;
        return direction;
    }
}
