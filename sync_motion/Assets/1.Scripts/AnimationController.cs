using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AnimationController : MonoBehaviour, IPunObservable
{
    [SerializeField] private AnimationClip[] clips;
    private Animator animator;
    private float[] clipsLengths;

    private readonly int hashWalk = Animator.StringToHash("WalkValue");
    private readonly int hashIsDance = Animator.StringToHash("Dance");
    private readonly int hashMotionNumber = Animator.StringToHash("MotionNumber");
    private readonly int hashOffset = Animator.StringToHash("OffsetValue");

    /// <summary>
    /// triggerCollider.enable => m_isDance
    /// </summary>
    private SphereCollider triggerCollider;

    /// <summary>
    /// Hash set to handle when triggered objects are disabled or destroyed.<br/>
    /// object must have <see cref="AnimationController"/> component.<br/><br/>
    /// 트리거 된 오브젝트가 숨겨지거나 파괴되었을 때 처리할 해시 셋.<br/>
    /// 오브젝트에 <see cref="AnimationController"/> 컴포넌트가 반드시 있어야 함. 
    /// </summary>
    private readonly HashSet<GameObject> triggeredObjects = new HashSet<GameObject>();

    /// <summary>
    /// Event to handle when triggered objects are disabled or destroyed.<br/>
    /// 트리거 된 오브젝트가 숨겨지거나 파괴되었을 때 처리할 이벤트.
    /// </summary>
    private event Action<GameObject> OnDisableEvent;

    /// <summary>
    /// Object to synchronize.<br/>
    /// 동기화 할 오브젝트
    /// </summary>
    private AnimationController synchronizeTo;

    /// <summary>
    /// Event to synchronized player also change motion when I change my motion.<br/>
    /// 모션을 바꿀 때 동기화 된 플레이어도 같이 바뀌게 할 이벤트.
    /// </summary>
    private event Action<AnimationController> OnChangeMotion;

    //data relay
    private PhotonView pv;

    private float walk;
    private bool m_isDance;
    private int MotionNumber;

    private long motionStartTicks;

    private const string SET = nameof(Set);
    private const string DANCE = nameof(Dance);

    /// <summary>
    /// Set <see cref="m_isDance">m_isDance</see> and call <see cref="Set">setting method</see> throgh <see cref="PhotonView.RPC">RPC</see> at other clients.<br/>
    /// <see cref="m_isDance">m_isDance</see>를 설정하고 <see cref="PhotonView.RPC">RPC</see>를 통해 다른 클라이언트에서 <see cref="Set">Set 함수</see> 실행.
    /// </summary>
    /// <returns>
    /// <see cref="m_isDance">m_isDance</see>
    /// </returns>
    /// 
    //  Set m_isDance and call setting method throgh RPC at other clients.
    //  m_isDance를 설정하고 RPC를 통해 다른 클라이언트에서 Set 함수 실행.
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

        clipsLengths = clips.Select(x => x.length).ToArray();
    }

    private void Update()
    {
        if (!pv.IsMine)
        {
            animator.SetFloat(hashWalk, walk);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggeredObjects.Add(other.gameObject))
            {
                other.GetComponent<AnimationController>().OnDisableEvent += DisableEvent;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DisableEvent(other.gameObject);
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
        if (triggeredObjects.Remove(@object))
        {
            var aniCon = @object.GetComponent<AnimationController>();
            aniCon.OnDisableEvent -= DisableEvent;
            aniCon.OnChangeMotion -= Change;
        }
    }

    public void Walk(float value)
    {
        walk = value;
        animator.SetFloat(hashWalk, walk);
    }

    public void PlayDance(int number)
    {
        PhotonNetwork.RemoveBufferedRPCs(methodName: DANCE);

        if (synchronizeTo != null)
        {
            synchronizeTo.OnChangeMotion -= Change;
            synchronizeTo = null;
        }

        long ticks = DateTime.Now.Ticks;
        pv.RPC(DANCE, RpcTarget.AllBuffered, number, ticks);
    }

    private void OnSync()
    {
        if (!m_isDance && triggeredObjects.Count > 0)
        {
            synchronizeTo = triggeredObjects.First().GetComponent<AnimationController>();
            synchronizeTo.OnChangeMotion += Change;
            Change(synchronizeTo);
        }
    }

    private void Change(AnimationController aniCon)
    {
        PhotonNetwork.RemoveBufferedRPCs(methodName: DANCE);
        pv.RPC(DANCE, RpcTarget.AllBuffered, aniCon.MotionNumber, aniCon.motionStartTicks);
    }

    /// <summary>
    /// Play animation on all clients.<br/>
    /// 모든 클라이언트에서 애니메이션 실행.
    /// </summary>
    /// <param name="number">Animation number.<br/>애니메이션 번호.</param>
    /// <param name="ticks">Ticks of animation playback start time.<br/>애니메이션 시작 시간의 틱.</param>
    [PunRPC]
    private void Dance(int number, long ticks)
    {
        MotionNumber = number;
        if (number == -1f)
        {
            IsDance = false;
        }
        else
        {
            synchronizeTo = synchronizeTo == null ? this : synchronizeTo;

            animator.SetInteger(hashMotionNumber, number);

            motionStartTicks = ticks;
            float diff = DateTime.Now.Ticks - motionStartTicks;
            float diffSeconds = diff / TimeSpan.TicksPerSecond;
            animator.SetFloat(hashOffset, diffSeconds / clipsLengths[number] % 1f);

            IsDance = true;
        }

        OnChangeMotion?.Invoke(this);
        if (!m_isDance)
        {
            OnChangeMotion = null;
        }
    }

    [PunRPC]
    private void Set(bool enable)
    {
        animator.SetBool(hashIsDance, enable);
        triggerCollider.enabled = enable;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)   
        {
            stream.SendNext(walk);
        }
        else                    
        {
            walk = (float)stream.ReceiveNext();
        }
    }
}
