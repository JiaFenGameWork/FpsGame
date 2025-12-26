using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerState : MonoBehaviour,IDamageable
{
    public float max_hp = 10;
    public float current_hp;
    public List<Image> images;

    public event Action<AttackData> OnTakeDamage;
    public event Action OnDeath;

    public float CurrentHealth => current_hp;
    public bool IsInvincible = false;
    public float MaxHealth => max_hp;
    private PlayerMovement movement;
    public bool IsDead => false;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        this.GetComponents<Image>();
        current_hp = max_hp;
    }

    // Update is called once per frame
    void Update()
    {
        float h = (float)current_hp / max_hp;
        HealthReduce(images.Count-1,h);
    }

    void Gameover()
    {
        
    }
    public void HealthReduce(int index,float value)
    {
        if (value <= 0f)
        {
            Gameover();
            return;
        }
        if (index < 0)
        {
            return;
        }
        float a = (float)(index) /images.Count;
        if (value< a)
        {
            images[index].enabled = false;
            HealthReduce(index-1,value);
        }
        else
        {
            for (int i = 0; i < index; i++)
            {
                images[i].enabled = true;
            }

        }
    }



    public void TakeDamage(AttackData attackData)
    {
        if (IsInvincible) return;

        current_hp-= attackData.damage;
        OnTakeDamage?.Invoke(attackData);

        // CharacterController 玩家不要用 Rigidbody.AddForce；击退/硬直交给 PlayerMovement
        if (movement != null&&attackData.KnockUp)
        {
            Vector3 attackerPos = attackData.transform != null ? attackData.transform.position : transform.position - transform.forward;
            movement.ApplyHit(attackerPos, attackData.KnockUp);
        }
        Debug.Log(current_hp);
    }

    public void Die()
    {
        throw new NotImplementedException();
    }
}
