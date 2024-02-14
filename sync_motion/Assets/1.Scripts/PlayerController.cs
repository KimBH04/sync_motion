using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float rotationSpeed;

    private Animator modelAni;

    private float hor;
    private float ver;

    private readonly int hashWalk = Animator.StringToHash("WalkValue");

    private void Start()
    {
        modelAni = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        hor = Input.GetAxis("Horizontal");
        ver = Input.GetAxis("Vertical");

        if (hor != 0f || ver != 0f)
        {
            float normal = 1f / Mathf.Sqrt(Mathf.Abs(hor) + Mathf.Abs(ver));
            transform.position += speed * Time.deltaTime * transform.forward;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f) *
                    Quaternion.LookRotation(new Vector3(hor * normal, 0f, ver * normal), Vector3.up),
                rotationSpeed * Time.deltaTime);

            modelAni.SetFloat(hashWalk, Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver)));
        }
    }
}
