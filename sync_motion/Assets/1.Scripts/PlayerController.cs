//Remove unused private member
#pragma warning disable IDE0051

using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Photon.Pun;

public class PlayerController : MonoBehaviour, IPunObservable
{
    private AnimationController animationController;

    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float damp;

    private float targetHor;
    private float targetVer;
    private float hor;
    private float ver;

    private Transform modelTr;
    private Quaternion lastRot;     //Model's rotation


    //data relay
    private PhotonView pv;

    private Vector3 receivePos;
    private Quaternion receiveRot;


    private Transform camTr;

    private void Start()
    {
        animationController = GetComponent<AnimationController>();
        pv = GetComponent<PhotonView>();
        if (pv.IsMine)
        {
            var cfl = FindAnyObjectByType<CinemachineFreeLook>();
            cfl.Follow = transform;
            cfl.LookAt = transform;

            camTr = Camera.main.transform;
            modelTr = transform.GetChild(0);
        }
        else
        {
            GetComponent<PlayerInput>().enabled = false;
        }
    }

    private void Update()
    {
        if (pv.IsMine)
        {
            transform.rotation = Quaternion.Euler(0f, camTr.eulerAngles.y, 0f);
            modelTr.rotation = lastRot;

            if (targetHor != 0f || targetVer != 0f || Mathf.Abs(hor) > 0.0001f || Mathf.Abs(ver) > 0.0001f)
            {
                hor = Mathf.Lerp(hor, targetHor, damp * Time.deltaTime);
                ver = Mathf.Lerp(ver, targetVer, damp * Time.deltaTime);

                float absHor = Mathf.Abs(hor);
                float absVer = Mathf.Abs(ver);
                float normal = Mathf.Sqrt(absHor + absVer);
                Vector3 translate = new Vector3(hor / normal, 0f, ver / normal);

                if (targetHor != 0f || targetVer != 0f)
                {
                    modelTr.rotation = Quaternion.Lerp(
                        modelTr.rotation,
                        Quaternion.Euler(0f, camTr.eulerAngles.y, 0f) * Quaternion.LookRotation(translate, Vector3.up),
                        rotationSpeed * Time.deltaTime);
                }

                transform.Translate(speed * Time.deltaTime * translate);

                float walk = Mathf.Max(absHor, absVer);
                animationController.Walk(walk);
            }

            lastRot = modelTr.rotation;
        }
        else
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, receivePos, damp * Time.deltaTime),
                Quaternion.Lerp(transform.rotation, receiveRot, damp * Time.deltaTime));
        }
    }

    private void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        targetHor = v.x;
        targetVer = v.y;

        if (animationController.IsDance && (targetHor != 0f || targetVer != 0f))
        {
            animationController.IsDance = false;
        }
    }

    private void OnDance(InputValue value)
    {
        if (targetHor == 0f && targetVer == 0f)
        {
            animationController.PlayDance(value.Get<float>());
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(modelTr.position);
            stream.SendNext(modelTr.rotation);
        }
        else
        {
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
