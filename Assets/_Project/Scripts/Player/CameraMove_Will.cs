using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove_Will : MonoBehaviour
{
    [SerializeField] private float cameraMoveSpeed = 1f;
    private void Update()
    {
        transform.position += new Vector3(cameraMoveSpeed * Time.deltaTime,0, 0);
    }
}
