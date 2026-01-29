using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddBloodBehaviour : MonoBehaviour
{
    public PlayerState playerState;
    private bool isPlayerInTrigger = false;
    private Collider _collider;

    void Start()
    {
        _collider = GetComponent<Collider>();
    }

    void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.F))
        {
            playerState.RecoveryHealth(5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }
}