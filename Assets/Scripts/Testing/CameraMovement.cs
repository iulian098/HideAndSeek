using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraMovement : MonoBehaviour
{
    public float speed;

    float currentSpeed;
    public AxisState xAxis, yAxis;
    public bool disableRotation;

    private void Start()
    {
        ChangeCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            disableRotation = !disableRotation;
            ChangeCursor();
        }
    }

    void FixedUpdate()
    {
        if (!disableRotation)
        {
            xAxis.Update(Time.fixedDeltaTime);
            yAxis.Update(Time.fixedDeltaTime);

            Vector3 rotation = new Vector3(yAxis.Value, xAxis.Value, 0);
            transform.rotation = Quaternion.Euler(rotation);
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = speed * 2;
        }
        else
        {
            currentSpeed = speed;
        }

        transform.Translate(Vector3.forward * currentSpeed * v * Time.deltaTime);
        transform.Translate(Vector3.right * currentSpeed * h * Time.deltaTime);
    }

    void ChangeCursor()
    {
        if (disableRotation)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            TerrainModify.instance.canDraw = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            TerrainModify.instance.canDraw = false;
        }
    }
}
