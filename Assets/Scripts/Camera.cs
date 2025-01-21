using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Camera : MonoBehaviour
{
    public Transform PlayerObject;
    public Vector3 Offset = new Vector3(0f, 0f, -10f);
    public float Smoothing = 0.25f;
    public Vector3 Velocity = Vector3.zero;
    private void Update()
    {
        Vector3 targetCameraPosition = PlayerObject.position + Offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetCameraPosition, ref Velocity, Smoothing * Time.deltaTime);
    }
}