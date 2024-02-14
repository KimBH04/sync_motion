using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;

    private void Start()
    {
        target = GameObject.Find("Player").transform;
    }

    private void Update()
    {
        Vector3 targetPos = target.TransformPoint(offset);
        Quaternion targetRot = Quaternion.LookRotation(target.position - transform.position);
        
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime),
            Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime));
    }
}
