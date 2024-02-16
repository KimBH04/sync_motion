#pragma warning disable IDE0051

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float rotationSpeed;

    private Animator modelAni;
    private Transform modelTr;

    private float targetHor;
    private float targetVer;
    private float hor;
    private float ver;

    private readonly int hashWalk = Animator.StringToHash("WalkValue");

    private Transform camTr;

    private Quaternion lastRot;

    private void Start()
    {
        camTr = Camera.main.transform;
        modelAni = GetComponentInChildren<Animator>();
        modelTr = transform.GetChild(0);
    }

    private void Update()
    {
        transform.rotation = Quaternion.Euler(0f, camTr.eulerAngles.y, 0f);
        modelTr.rotation = lastRot;

        hor = Mathf.Lerp(hor, targetHor, 10f * Time.deltaTime);
        ver = Mathf.Lerp(ver, targetVer, 10f * Time.deltaTime);

        if (Mathf.Abs(hor) > 0.0001f || Mathf.Abs(ver) > 0.0001f)
        {
            Vector3 translate = new Vector3(hor, 0f, ver);

            modelTr.rotation = Quaternion.Lerp(
                modelTr.rotation,
                Quaternion.Euler(0f, camTr.eulerAngles.y, 0f) *
                    Quaternion.LookRotation(translate, Vector3.up),
                rotationSpeed * Time.deltaTime);

            transform.Translate(speed * Time.deltaTime * translate);

            modelAni.SetFloat(hashWalk, Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver)));
        }

        lastRot = modelTr.rotation;
    }

    private void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        targetHor = v.x;
        targetVer = v.y;
    }
}
