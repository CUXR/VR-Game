using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CameraFollow : MonoBehaviour
{
    public Transform cameraHolder;
    private float rotationX, rotationY;
    [SerializeField]
    private float lookSpeed;
    // Start is called before the first frame update
    void Start()
    {
        // Centers and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (XRSettings.isDeviceActive)
        {
            // The tracked pose driver updates the camera from the headset.
            // Do not replace its rotation with desktop mouse input.
            return;
        }

        Vector2 lookDirection = InputController.Instance.GetLookDirection();

        // Get mouse input with sensitivity
        float mouseX = lookSpeed * lookDirection.x * Time.fixedDeltaTime;
        float mouseY = lookSpeed * lookDirection.y * Time.fixedDeltaTime;
    
        // Weird but works (DON'T TOUCH)
        rotationY += mouseX;
        rotationX -= mouseY;

        // Clamp y-axis rotation
        rotationX = Math.Clamp(rotationX, -80f, 80f);

        cameraHolder.transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        
        // Update player rotation
        transform.rotation = Quaternion.Euler(0, rotationY, 0);
    }
}
