using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AttackStyle
{
 public static void ShootBullet(Transform target,Transform[] spawnPoints, GameObject bulletPrefab, GameObjectPool bulletPool, float bulletSpeed, float bulletLifeTime)
    {
        if (target == null) return;
        if (spawnPoints == null) return;
        foreach (var spawn in spawnPoints)
        {
        Vector3 targetPos = target.position;
        targetPos.y += 1.0f; // 瞄准目标中心偏上

        Vector3 shootDir = (targetPos - spawn.position).normalized;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDir);
        
        GameObject bulletGO = null;
        if (bulletPool != null)
        {
            bulletGO = bulletPool.Get(spawn.position, bulletRotation);
        }
        else if (bulletPrefab != null)
        {
            bulletGO = UnityEngine.Object.Instantiate(bulletPrefab, spawn.position, bulletRotation);
        }
        
        if (bulletGO == null) return;
        
        // 设置子弹属性
        BulletBehaviour bullet = bulletGO.GetComponent<BulletBehaviour>();
        if (bullet != null)
        {
            bullet.owner = spawn.transform;
            bullet.SetReturn((go) => {
                if (bulletPool != null)
                    bulletPool.Return(go);
                else
                    UnityEngine.Object.Destroy(go);
            }, bulletLifeTime);
        }
        
        // 设置子弹速度
        Rigidbody rb = bulletGO.GetComponent<Rigidbody>();
            rb.velocity = shootDir * bulletSpeed;
            Debug.Log($"[BossAttackState] 发射子弹 -> 枪口: {spawn.name}");
        }
    }
    public static void ShootTraceingBullet(Transform target,Transform[] spawnPoints, GameObject bulletPrefab, float bulletSpeed)
    {
        foreach (var spawn in spawnPoints)
        {
        GameObject obj = GameObject.Instantiate(bulletPrefab, spawn.position, spawn.rotation);

        // 设置子弹速度
        Rigidbody rb = obj.GetComponent<Rigidbody>();
            rb.velocity = spawn.forward * bulletSpeed;
            Debug.Log($"[BossAttackState] 发射子弹 -> 枪口: {spawn.name}");
        }
    }

    /// <summary>
    /// 发射向上的炮弹（会落下造成AOE）
    /// </summary>
    public static void ShootUpBullet(Transform target,Transform spawn, GameObject bulletPrefab, GameObjectPool bulletPool, float bulletSpeed, float bulletLifeTime)
    {
        if (target == null) return;
        
        // 向上发射，预测落点为玩家当前位置
        Vector3 targetPos = target.position;
        Vector3 shootDir = Vector3.up;
        Quaternion bulletRotation = Quaternion.LookRotation(shootDir);
        
        GameObject bulletGO = null;
        if (bulletPool != null)
        {
            bulletGO = bulletPool.Get(spawn.position, bulletRotation);
        }
        else if (bulletPrefab != null)
        {
            bulletGO = UnityEngine.Object.Instantiate(bulletPrefab, spawn.position, bulletRotation);
        }
        
        if (bulletGO == null) return;
        
        // 设置子弹属性并添加抛物线行为
        BulletBehaviour bullet = bulletGO.GetComponent<BulletBehaviour>();
        if (bullet != null)
        {
            bullet.owner = spawn.transform;
            bullet.SetReturn((go) => {
                // 子弹落地时触发AOE
                //ExecuteAoeDamage(go.transform.position);
                
                if (bulletPool != null)
                    bulletPool.Return(go);
                else
                    UnityEngine.Object.Destroy(go);
            }, bulletLifeTime);
        }
        
        // 给子弹一个向上的初速度和抛物线轨迹
        Rigidbody rb = bulletGO.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            // 计算抛射角度以命中目标位置
            Vector3 toTarget = targetPos - spawn.position;
            float horizontalDist = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
            float verticalDist = toTarget.y;
            
            // 简化的抛物线计算：向上发射后落到目标位置
            float launchSpeed = Mathf.Sqrt(horizontalDist * Physics.gravity.magnitude);
            Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z).normalized;
            
            rb.velocity = Vector3.up * launchSpeed + horizontalDir * (horizontalDist / bulletLifeTime);
        }
        
        Debug.Log($"[BossAttackState] 向上开炮 -> 目标落点: {targetPos}");
    }

}
