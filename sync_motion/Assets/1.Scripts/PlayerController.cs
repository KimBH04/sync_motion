using Cinemachine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IPunObservable
{
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float damp;

    private float targetHor;
    private float targetVer;
    private float hor;
    private float ver;

    private Transform modelTr;
    private Quaternion lastRot;     //Model's rotation

    private Animator modelAni;
    private float walk = 0f;
    private float offset = 0f;
    private bool isDance = false;
    private readonly int hashWalk = Animator.StringToHash("WalkValue");
    private readonly int hashOffset = Animator.StringToHash("OffsetValue");
    private readonly int hashDance = Animator.StringToHash("Dance");

    private SphereCollider triggerCollider;

    //트리거 된 오브젝트가 Disable 되거나 Destroy 될 때 처리할 해시 셋과 이벤트
    private readonly HashSet<GameObject> triggeredObjects = new HashSet<GameObject>();
    private event System.Action<GameObject> OnDisableEvent;

    //data relay
    private PhotonView pv;
    private Vector3 receivePos;
    private Quaternion receiveRot;
    private float receiveWalk;
    private float receiveOffset;
    private bool receiveIsDance;

    private Transform camTr;

    private void Start()
    {
        triggerCollider = GetComponent<SphereCollider>();

        pv = GetComponent<PhotonView>();

        modelAni = GetComponentInChildren<Animator>();

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

            if (targetHor != 0 || targetVer != 0 || Mathf.Abs(hor) > 0.0001f || Mathf.Abs(ver) > 0.0001f)
            {
                hor = Mathf.Lerp(hor, targetHor, damp * Time.deltaTime);
                ver = Mathf.Lerp(ver, targetVer, damp * Time.deltaTime);

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

                walk = Mathf.Max(Mathf.Abs(hor), Mathf.Abs(ver));
                modelAni.SetFloat(hashWalk, walk);
            }

            lastRot = modelTr.rotation;
        }
        else
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, receivePos, damp * Time.deltaTime),
                Quaternion.Lerp(transform.rotation, receiveRot, damp * Time.deltaTime));

            modelAni.SetFloat(hashWalk, receiveWalk);
            modelAni.SetFloat(hashOffset, receiveOffset);

            isDance = receiveIsDance;
            pv.RPC(nameof(ColliderEnable), RpcTarget.All, isDance);
            modelAni.SetBool(hashDance, isDance);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isDance)
            {
                if (triggeredObjects.Add(other.gameObject))
                {
                    other.GetComponent<PlayerController>().OnDisableEvent += DisableEvent;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggeredObjects.Remove(other.gameObject))
            {
                other.GetComponent<PlayerController>().OnDisableEvent -= DisableEvent;
            }
        }
    }

    private void OnDisable()
    {
        OnDisableEvent?.Invoke(gameObject);
    }

    private void OnDestroy()
    {
        OnDisableEvent?.Invoke(gameObject);
    }

    private void DisableEvent(GameObject @object)
    {
        triggeredObjects.Remove(@object);
    }

    private void OnMove(InputValue value)
    {
        Vector2 v = value.Get<Vector2>();
        targetHor = v.x;
        targetVer = v.y;

        if (isDance && (targetHor != 0f || targetVer != 0f))
        {
            isDance = false;
            pv.RPC(nameof(ColliderEnable), RpcTarget.All, false);
            modelAni.SetBool(hashDance, false);
        }
    }

    private void OnDance()
    {
        if (targetHor == 0f && targetVer == 0f)
        {
            offset = 0f;
            modelAni.SetFloat(hashOffset, 0f);

            isDance = !isDance;
            pv.RPC(nameof(ColliderEnable), RpcTarget.All, isDance);
            modelAni.SetBool(hashDance, isDance);
        }
    }

    private void OnSync()
    {
        if (!isDance && triggeredObjects.Count > 0)
        {
            Animator syncAni = triggeredObjects.First().GetComponentInChildren<Animator>();
            offset = (syncAni.GetCurrentAnimatorStateInfo(0).normalizedTime + syncAni.GetFloat(hashOffset)) % 1f;
            modelAni.SetFloat(hashOffset, offset);

            isDance = true;
            pv.RPC(nameof(ColliderEnable), RpcTarget.All, true);
            modelAni.SetBool(hashDance, true);
        }
    }

    [PunRPC]
    private void ColliderEnable(bool enable)
    {
        triggerCollider.enabled = enable;
        if (!enable)
        {
            OnDisableEvent?.Invoke(gameObject);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsReading)
        {
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();

            receiveWalk = (float)stream.ReceiveNext();
            receiveOffset = (float)stream.ReceiveNext();
            receiveIsDance = (bool)stream.ReceiveNext();
        }
        else if (stream.IsWriting)
        {
            stream.SendNext(modelTr.position);
            stream.SendNext(modelTr.rotation);

            stream.SendNext(walk);
            stream.SendNext(offset);
            stream.SendNext(isDance);
        }
    }
}
