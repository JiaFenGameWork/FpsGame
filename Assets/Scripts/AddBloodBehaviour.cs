using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddBloodBehaviour : MonoBehaviour
{
    public PlayerState playerState;

    private Collider _collider;
    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponent<Collider>();
    }

    // Update is called once per frame
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                other.gameObject.GetComponent<IDamageable>().RecoveryHealth(5f);
            }
        }
    }
}
