using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public const string VERSION = "Lemon";

    [SerializeField] private Transform loadingImage;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = VERSION;

        PhotonNetwork.ConnectUsingSettings();
    }

    private IEnumerator Start()
    {
        while (!PhotonNetwork.InRoom)
        {
            yield return new WaitForSeconds(0.1f);
            loadingImage.eulerAngles += new Vector3(0f, 0f, -30f);
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected To Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }
}
