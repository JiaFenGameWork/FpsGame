using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public interface IDamageable
{
    float CurrentHealth { get; }
    
    float MaxHealth { get; }

    bool IsDead { get; }
  
    event Action<AttackData> OnTakeDamage;
    
    event Action OnDeath;
    void RecoveryHealth(float r);
    void TakeDamage(AttackData attackData);
    
    void Die();
}
public enum DamageType
{
    normal,
    lighting,

}
public struct DamageInfo
{
    public float Damage;
    public GameObject Attacker;
    public bool IsWeakPoint;
    public DamageType DamageType;
}
public interface IEnemy
{
    string EnemyID { get; }
    
    EnemyState CurrentState { get; }
    
    void Initialize(int difficultyLevel);
    
    void PerformAttack(IDamageable target);
    
    void MoveTo(Vector3 targetPosition);
}

public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Dead
}
