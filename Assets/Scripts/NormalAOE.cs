using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Collider))]
public class NormalAOE : MonoBehaviour
{
    Collider collider;
    public float lifetime = 2f;
    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<Collider>();
        Invoke("Destroy", lifetime);
    }
    void Destroy()
    {
        Destroy(gameObject);
    }
    void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(new AttackData(transform, 10, false, true));
        }
    }
}
