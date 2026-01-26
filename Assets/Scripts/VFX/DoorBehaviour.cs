using System;
using System.Collections;
using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        other.gameObject.transform.position = new Vector3(0f, 2f, 0f);
    }

    private void OnTriggerStay(Collider other)
    {
        other.gameObject.transform.position = new Vector3(0f, 2f, 0f);
    }
}
