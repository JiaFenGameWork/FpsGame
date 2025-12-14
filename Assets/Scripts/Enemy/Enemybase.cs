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
  
    event Action<float> OnTakeDamage;
    
    event Action OnDeath;
    
    void TakeDamage(float amount);
    
    void Die();
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
