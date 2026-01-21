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
    
    [Header("瞄准设置")]
    public string aimBoolParameter = "IsAiming";
    public string aimAnimationName = "Aim";
    public string aimExitAnimationName = "";
    private bool isAiming;
    private bool hasAimBoolParameter;
    private int aimBoolHash;
    [Header("射线检测设置")]
    public LayerMask aimLayerMask = ~0;
    
    [Header("后坐力设置")]
    public float recoilAmount = 2f;        // 后坐力向上的角度
    public float recoilSpeed = 10f;        // 后坐力上升速度
    public float returnSpeed = 5f;         // 恢复速度

    [Header("音效设置")]
    public AudioClip shootSound;           // 开枪音效
    [Range(0f, 1f)]
    public float shootVolume = 0.8f;       // 开枪音量
    public float shootPitchVariation = 0.1f; // 音调随机变化（增加多样性）
    public bool use3DSound = true;         // 是否使用3D音效
    [Range(0f, 1f)]
    public float spatialBlend = 0.8f;      // 空间混合度（0=2D, 1=3D）
    public bool useLoopingAutoSound = true; // 自动武器使用循环音效
    public float loopingFadeIn = 0.05f;   // 循环音效淡入
    public float loopingFadeOut = 0.08f;  // 循环音效淡出

    // 子弹活动协程追踪，避免重复归还
    private Dictionary<GameObject, Coroutine> _bulletCoroutines = new Dictionary<GameObject, Coroutine>();
    private AudioChannel _loopingShootChannel;
    private bool _wasAutoShooting;

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
        aimBoolHash = Animator.StringToHash(aimBoolParameter);
        hasAimBoolParameter = HasAnimatorParameter(aimBoolParameter, AnimatorControllerParameterType.Bool);
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
        bullet.transform.position = bulletSpawn.position;
        bullet.transform.rotation = bulletSpawn.rotation;
    }

    void Update()
    {
        UpdateAimState();
        if (CurrentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (CurrentShootingMode == ShootingMode.Single || CurrentShootingMode == ShootingMode.Burst)
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        UpdateLoopingShootSound();
        if (readyToShoot && isShooting)
        {
            burstBulletLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        readyToShoot = false;
        if(CurrentShootingMode == ShootingMode.Single)
        {
            FireAnimationName = "Shot";
        }
        if(CurrentShootingMode == ShootingMode.Auto)
        {
            FireAnimationName = "Shot2";
        }
        animator.Play(FireAnimationName);   
        
        // 播放开枪音效
        PlayShootSound();
        
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

        // 修复：针对刚体插值(Interpolation)导致的视觉残留或位置跳变问题进行二次校准
        // 当快速移动镜头时，如果对象池先激活再设置位置，插值系统会产生错误的平滑运动
        var bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.velocity = Vector3.zero;
            bulletRb.angularVelocity = Vector3.zero;
            // 强制同步刚体位置，消除插值带来的"瞬移"视觉误差
            bulletRb.position = bulletSpawn.position;
            bulletRb.rotation = Quaternion.LookRotation(shootingDirection);
        }
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
            bulletBehaviour.SetReturn(ReturnBullet, bulletPrefabLifeTime);  // 注入归还方法
        }
        
        // 发射
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        }

        if (allowReset)
        {
            // 修复：如果是连发模式，重置时间需要涵盖整个连发过程，防止连发未结束时被打断或重置导致丢弹
            float resetTime = (CurrentShootingMode == ShootingMode.Burst) ? shootingDelay * bulletsPerBurst : shootingDelay;
            Invoke("ResetShot", resetTime);
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
    /// <summary>
    /// 播放开枪音效
    /// </summary>
    private void PlayShootSound()
    {
        if (shootSound == null) return;
        if (CurrentShootingMode == ShootingMode.Auto && useLoopingAutoSound) return;
        
        if (use3DSound)
        {
            // 3D音效：在枪口位置播放，有空间感
            AudioManager.Instance.PlaySFXAtPosition(
                shootSound, 
                bulletSpawn.position, 
                shootVolume, 
                spatialBlend,
                shootPitchVariation
            );
        }
        else
        {
            // 2D音效：全局播放
            AudioManager.Instance.PlaySFX(shootSound, shootVolume, shootPitchVariation);
        }
    }

    private void UpdateLoopingShootSound()
    {
        if (!useLoopingAutoSound || CurrentShootingMode != ShootingMode.Auto)
        {
            StopLoopingShootSound();
            _wasAutoShooting = false;
            return;
        }

        if (isShooting && !_wasAutoShooting)
        {
            StartLoopingShootSound();
        }
        else if (!isShooting && _wasAutoShooting)
        {
            StopLoopingShootSound();
        }

        _wasAutoShooting = isShooting;

        if (use3DSound && _loopingShootChannel != null && _loopingShootChannel.IsPlaying)
        {
            _loopingShootChannel.SetPosition(bulletSpawn.position);
        }
    }

    private void StartLoopingShootSound()
    {
        if (shootSound == null) return;
        if (_loopingShootChannel != null && _loopingShootChannel.IsPlaying) return;

        _loopingShootChannel = AudioManager.Instance.PlayLoopingSFX(shootSound, shootVolume, loopingFadeIn);
        if (_loopingShootChannel != null && use3DSound)
        {
            _loopingShootChannel.SetPosition(bulletSpawn.position);
            _loopingShootChannel.Source.spatialBlend = spatialBlend;
            _loopingShootChannel.Source.rolloffMode = AudioRolloffMode.Linear;
            _loopingShootChannel.Source.minDistance = 1f;
            _loopingShootChannel.Source.maxDistance = 50f;
        }
    }

    private void StopLoopingShootSound()
    {
        if (_loopingShootChannel != null)
        {
            AudioManager.Instance.StopLoopingSFX(_loopingShootChannel, loopingFadeOut);
            _loopingShootChannel = null;
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

    private void OnDisable()
    {
        StopLoopingShootSound();
        if (hasAimBoolParameter && animator != null)
        {
            animator.SetBool(aimBoolHash, false);
        }
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

        // 修复：如果目标点在枪口后方（例如贴脸射击时射线击中点非常近），方向会指向后方
        // 检查与摄像机前方的夹角，如果方向偏差太大，强制使用摄像机前方
        if (Vector3.Dot(playerCamera.transform.forward, direction) <= 0.1f)
        {
            direction = playerCamera.transform.forward;
        }

        return direction;
    }

    private void UpdateAimState()
    {
        bool aiming = Input.GetKey(KeyCode.Mouse1);
        if (aiming == isAiming) return;
        isAiming = aiming;

        if (animator == null) return;
        if (hasAimBoolParameter)
        {
            animator.SetBool(aimBoolHash, isAiming);
        }
        else if (isAiming)
        {
            if (!string.IsNullOrEmpty(aimAnimationName))
            {
                animator.Play(aimAnimationName);
            }
        }
        else if (!string.IsNullOrEmpty(aimExitAnimationName))
        {
            animator.Play(aimExitAnimationName);
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var param in animator.parameters)
        {
            if (param.type == type && param.name == paramName) return true;
        }
        return false;
    }
}
