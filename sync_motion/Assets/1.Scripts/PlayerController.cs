using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Photon.Pun;

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


    [SerializeField] private AnimationClip[] animationClips;

    private Animator modelAni;
    private float walk = 0f;
    private float offset = 0f;
    private bool isDance = false;

    private readonly int hashWalk = Animator.StringToHash("WalkValue");
    private readonly int hashOffset = Animator.StringToHash("OffsetValue");
    private readonly int hashDance = Animator.StringToHash("Dance");
    private readonly int hashDanceIndex = Animator.StringToHash("DanceIndex");

    private SphereCollider triggerCollider;

    //트리거 된 오브젝트가 Disable 되거나 Destroy 될 때 처리할 해시 셋과 이벤트
    private readonly HashSet<GameObject> triggeredObjects = new HashSet<GameObject>();
    private event Action<GameObject> OnDisableEvent;


    //data relay
    private PhotonView pv;

    private Vector3 receivePos;
    private Quaternion receiveRot;

    private float receiveWalk;
    private float receiveOffset;
    private int danceIdx;
    private bool receiveIsDance;


    //rpc send message
    private readonly string COLLIDER = nameof(ColliderEnable);


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

            if (targetHor != 0f || targetVer != 0f || Mathf.Abs(hor) > 0.0001f || Mathf.Abs(ver) > 0.0001f)
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

            if (!isDance)
            {
                modelAni.SetFloat(hashWalk, receiveWalk);
                modelAni.SetFloat(hashOffset, receiveOffset);
                modelAni.SetFloat(hashDanceIndex, danceIdx);
            }

            isDance = receiveIsDance;
            pv.RPC(COLLIDER, RpcTarget.All, isDance);
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
            pv.RPC(COLLIDER, RpcTarget.All, false);
            modelAni.SetBool(hashDance, false);
        }
    }

    private void OnDance(InputValue value)
    {
        if (targetHor == 0f && targetVer == 0f)
        {
            float index = value.Get<float>();
            if (index == -1f)
            {
                isDance = true;
            }
            else
            {
                offset = 0f;
                modelAni.SetFloat(hashOffset, 0f);

                danceIdx = (int)index;
                modelAni.SetFloat(hashDanceIndex, index);

                isDance = false;
            }

            isDance = !isDance;
            pv.RPC(COLLIDER, RpcTarget.All, isDance);
            modelAni.SetBool(hashDance, isDance);
        }
    }

    private void OnSync()
    {
        if (!isDance && triggeredObjects.Count > 0)
        {
            //트리거 된 다른 플레이어의 애니메이션과 애니메이션 번호 가져오기
            GameObject anotherPlayer = triggeredObjects.First();
            
            Animator syncAni = anotherPlayer.GetComponentInChildren<Animator>();
            offset = (syncAni.GetCurrentAnimatorStateInfo(0).normalizedTime + syncAni.GetFloat(hashOffset)) % 1f;
            modelAni.SetFloat(hashOffset, offset);

            PlayerController syncCon = anotherPlayer.GetComponent<PlayerController>();
            danceIdx = syncCon.danceIdx;
            modelAni.SetFloat(hashDanceIndex, danceIdx);

            isDance = true;
            pv.RPC(COLLIDER, RpcTarget.All, true);
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
        if (stream.IsWriting)
        {
            stream.SendNext(modelTr.position);
            stream.SendNext(modelTr.rotation);

            stream.SendNext(walk);
            stream.SendNext(offset);
            stream.SendNext(danceIdx);
            stream.SendNext(isDance);
        }
        else
        {
            receivePos = (Vector3)stream.ReceiveNext();
            receiveRot = (Quaternion)stream.ReceiveNext();

            receiveWalk = (float)stream.ReceiveNext();
            receiveOffset = (float)stream.ReceiveNext();
            danceIdx = (int)stream.ReceiveNext();
            receiveIsDance = (bool)stream.ReceiveNext();

            //송수신 시간 차이 좁히기
            receiveOffset += (PhotonNetwork.ServerTimestamp - info.SentServerTimestamp) / 990f / animationClips[danceIdx].length;
            receiveOffset %= 1f;
        }
    }
}
