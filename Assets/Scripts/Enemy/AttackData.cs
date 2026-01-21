using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackData 
{
    public Transform transform;
    public float damage;
    public bool KnockUp;
    public bool IsWeakPoint;
    public AttackData(Transform tr, float damage,bool isWeakPoint, bool knockUp = false)
    {
        this.transform = tr;
        this.damage = damage;
        this.KnockUp = knockUp;
        this.IsWeakPoint = isWeakPoint;
    }
}
/// <summary>
/// Boss攻击配置类
/// 用于初始化BossAttackState的参数
/// </summary>
[System.Serializable]
public class BossAttackConfig
{
    [Header("距离阈值")]
    public float meleeRange = 3f;
    public float midRange = 8f;
    public float farRange = 15f;
    
    [Header("背后检测")]
    public float behindAngleThreshold = 120f;
    
    [Header("攻击参数")]
    public float attackCooldown = 0.5f;
    public float rotationSpeed = 5f;
    
    [Header("子弹设置")]
    public GameObject bulletPrefab;
    public Transform[] bulletSpawnPoints;
    public float bulletSpeed = 15f;
    public float bulletLifeTime = 3f;
    public float bulletDamage = 10f;
    
    [Header("AOE设置")]
    public float aoeDamage = 20f;
    public float aoeRadius = 5f;
    public LayerMask playerLayer;
    
    [Header("近战设置")]
    public float meleeDamage = 15f;
    public float meleeCombo2Damage = 25f;
    public Collider[] meleeHitColliders;
    
    [Header("动画名称")]
    public string meleeCombo1Anim = "attack_melee1";
    public string meleeCombo2Anim = "attack_melee2";
    public string shootUpAnim = "attack_shoot_up";
    public string shootNormalAnim = "attack_shoot";
    public string jumpTurnAnim = "attack_jump_turn";
    public string idleAnim = "idle";
}
