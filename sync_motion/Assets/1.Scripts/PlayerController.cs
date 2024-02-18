#pragma warning disable IDE0051

using Cinemachine;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IPunObservable
{
    [SerializeField] float speed;
    [SerializeField] float rotationSpeed;
    [SerializeField] float damping;

    private Transform modelTr;

    private Animator modelAni;
    private readonly int hashWalk = Animator.StringToHash("WalkValue");
    private readonly int hashDance = Animator.StringToHash("Dance");
    private bool isDance = false;

    private float targetHor;
    private float targetVer;
    private float hor;
    private float ver;

    private Transform camTr;

    private PhotonView pv;
    private Vector3 receivePos;
    private Quaternion receiveRot;

    private Quaternion lastRot;

    private void Start()
    {
        pv = GetComponent<PhotonView>();

        if (pv.IsMine)
        {
            var cfl = FindAnyObjectByType<CinemachineFreeLook>();
            cfl.Follow = transform;
            cfl.LookAt = transform;

            camTr = Camera.main.transform;
            modelAni = GetComponentInChildren<Animator>();
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

            hor = Mathf.Lerp(hor, targetHor, 10f * Time.deltaTime);
            ver = Mathf.Lerp(ver, targetVer, 10f * Time.deltaTime);

            if (Mathf.Abs(hor) > 0.0001f || Mathf.Abs(ver) > 0.0001f)
            {
                float normal = Mathf.Sqrt(Mathf.Abs(hor) + Mathf.Abs(ver));
                Vector3 translate = new Vector3(hor / normal, 0f, ver / normal);

                if (targetHor != 0f || targetVer != 0f)
                {
                    modelTr.rotation = Quaternion.Lerp(
                        modelTr.rotation,
                        Quaternion.Euler(0f, camTr.eulerAngles.y, 0f) * Quaternion.LookRotation(translate, Vector3.up),
                        rotationSpeed * Time.deltaTime);
                }

                transform.Translate(speed * Time.deltaTime * translate);

                modelAni.SetFloat(hashWalk, Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver)));
            }

            lastRot = modelTr.rotation;
        }
        else
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, receivePos, damping * Time.deltaTime),
                Quaternion.Lerp(transform.rotation, receiveRot, damping * Time.deltaTime));
        }
    }

    private void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        targetHor = v.x;
        targetVer = v.y;

        if (isDance && (targetHor != 0f || targetVer != 0f))
        {
            isDance = false;
            modelAni.SetBool(hashDance, false);
        }
    }

    private void OnDance()
    {
        if (targetHor == 0f && targetVer == 0f)
        {
            isDance = !isDance;
            modelAni.SetBool(hashDance, isDance);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsReading)
        {
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();
        }
        else if (stream.IsWriting)
        {
            stream.SendNext(modelTr.position);
            stream.SendNext(modelTr.rotation);
        }
    }
}
