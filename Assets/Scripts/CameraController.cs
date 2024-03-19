using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    void FixedUpdate()
    {
        Vector3 targetPos = new Vector3(0, target.position.y, transform.position.z);

        this.transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);

    }
}
