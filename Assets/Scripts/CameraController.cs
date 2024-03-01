using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject target;

    void Update()
    {
        var position = target.transform.position;
        transform.RotateAround(position, Vector3.up, 5 * Time.deltaTime);
        // transform.LookAt(position, Vector3.up);
    }
}
