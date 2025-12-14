using System;
using UnityEngine;

public class AttackModule : MonoBehaviour, AttackIstate
{
    [SerializeField]
    private AttackData[] attacks;
    [SerializeField]
    private Animator animator;

    public event Action<AttackData> OnAttackStart;
    public event Action<AttackData> OnAttackEnd;
    public event Action<AttackData> OnHit;
    enum phase { Idle,Windup,Active,Recovery}

    public bool IsAttacking => throw new System.NotImplementedException();

    public bool CanStartAttack => throw new System.NotImplementedException();

    public void Cancel()
    {
        throw new System.NotImplementedException();
    }

    public void Tick(float dt)
    {
        
    }

    public bool TryStartAttack(Transform target)
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
