using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public interface IDamageable
{
    // 属性：当前生命值
    float CurrentHealth { get; }

    // 属性：最大生命值
    float MaxHealth { get; }

    // 属性：是否已死亡
    bool IsDead { get; }
    // 事件：当受到伤害时触发（可选，用于UI更新或特效）
    event Action<float> OnTakeDamage;

    // 事件：当死亡时触发
    event Action OnDeath;
    /// <summary>
    /// 承受伤害的方法
    /// </summary>
    /// <param name="amount">伤害数值</param>
    void TakeDamage(float amount);

    /// <summary>
    /// 死亡逻辑处理
    /// </summary>
    void Die();
}
public interface IEnemy
{
    // 敌人的唯一标识符或类型ID
    string EnemyID { get; }

    // 敌人的当前状态（例如：巡逻、追逐、攻击）
    EnemyState CurrentState { get; }

    /// <summary>
    /// 初始化/生成敌人
    /// </summary>
    void Initialize(int difficultyLevel);

    /// <summary>
    /// 执行攻击逻辑
    /// </summary>
    /// <param name="target">攻击目标（通常是实现了IDamageable的对象）</param>
    void PerformAttack(IDamageable target);

    /// <summary>
    /// 移动/寻路逻辑
    /// </summary>
    void MoveTo(Vector3 targetPosition);
}
// 简单的状态枚举
public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Dead
}
