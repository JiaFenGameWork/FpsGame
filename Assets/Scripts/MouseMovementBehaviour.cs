using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMovementBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    public float mouseSensitivity = 500f;
    float xRotation = 0f;
    float yRotation = 0f;
    public float topClamp = -90f;
    public float bottomClamp = 90f;

    // Recoil variables
    private float recoilPool = 0f;
    public float recoilSpeed = 10f;

    public void AddRecoil(float amount)
    {
        recoilPool += amount;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;     
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        if (recoilPool > 0)
        {
            float recoilStep = recoilPool * recoilSpeed * Time.deltaTime;
            recoilPool -= recoilStep;
            xRotation -= recoilStep;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);
        yRotation += mouseX;
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
