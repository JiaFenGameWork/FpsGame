using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface AttackIstate 
{
   bool IsAttacking { get; }
    bool CanStartAttack {  get; }

    bool TryStartAttack(Transform target);

    void Tick(float dt);

    void Cancel();
}
