using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AnimationController : MonoBehaviour, IPunObservable
{
    private Animator animator;

    private readonly int hashWalk = Animator.StringToHash("WalkValue");
    private readonly int hashIsDance = Animator.StringToHash("Dance");
    private readonly int hashAnimationNumber = Animator.StringToHash("AnimationNumber");
    private readonly int hashOffset = Animator.StringToHash("OffsetValue");

    private SphereCollider triggerCollider;

    //트리거 된 오브젝트가 Disable 되거나 Destroy 될 때 처리할 해시 셋과 이벤트
    private readonly HashSet<GameObject> triggeredObjects = new HashSet<GameObject>();
    private event Action<GameObject> OnDisableEvent;

    //data relay
    private PhotonView pv;

    private float walk;
    private bool m_isDance;
    private float animationNumber;

    private long motionStartTicks;

    private const string SET = nameof(SetDoDance);
    private const string DANCE = nameof(Dance);

    public bool IsDance
    {
        get
        {
            return m_isDance;
        }
        set
        {
            m_isDance = value;

            PhotonNetwork.RemoveBufferedRPCs(methodName: SET);
            pv.RPC(SET, RpcTarget.AllBuffered, value);
        }
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        triggerCollider = GetComponent<SphereCollider>();
        pv = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (!pv.IsMine)
        {
            animator.SetFloat(hashWalk, walk);
            animator.SetFloat(hashAnimationNumber, animationNumber);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!m_isDance)
            {
                if (triggeredObjects.Add(other.gameObject))
                {
                    other.GetComponent<AnimationController>().OnDisableEvent += DisableEvent;
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
                other.GetComponent<AnimationController>().OnDisableEvent -= DisableEvent;
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

    public void Walk(float value)
    {
        walk = value;
        animator.SetFloat(hashWalk, walk);
    }

    public void PlayDance(float number)
    {
        PhotonNetwork.RemoveBufferedRPCs(methodName: DANCE);

        long ticks = DateTime.Now.Ticks;
        pv.RPC(DANCE, RpcTarget.AllBuffered,
            number,
            animator.GetCurrentAnimatorStateInfo(0).length,
            ticks);
    }

    private void OnSync()
    {
        if (!m_isDance && triggeredObjects.Count > 0)
        {
            PhotonNetwork.RemoveBufferedRPCs(methodName: DANCE);

            AnimationController anotherCon = triggeredObjects.First().GetComponent<AnimationController>();
            pv.RPC(DANCE, RpcTarget.AllBuffered,
                anotherCon.animationNumber,
                anotherCon.animator.GetCurrentAnimatorStateInfo(0).length,
                anotherCon.motionStartTicks);
        }
    }

    [PunRPC]
    private void Dance(float number, float len, long ticks)
    {
        if (number == -1f)
        {
            IsDance = false;
        }
        else
        {
            animationNumber = number;
            animator.SetFloat(hashAnimationNumber, number);

            motionStartTicks = ticks;
            float diff = DateTime.Now.Ticks - motionStartTicks;
            float diffSeconds = diff / TimeSpan.TicksPerSecond;
            animator.SetFloat(hashOffset, diffSeconds / len % 1f);

            IsDance = true;
        }
    }

    [PunRPC]
    private void SetDoDance(bool enable)
    {
        animator.SetBool(hashIsDance, enable);
        triggerCollider.enabled = enable;
        if (!enable)
        {
            OnDisableEvent?.Invoke(gameObject);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //send
        if (stream.IsWriting)   
        {
            stream.SendNext(walk);
        }
        //receive
        else                    
        {
            walk = (float)stream.ReceiveNext();
        }
    }
}
