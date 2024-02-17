using Photon.Pun;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    private IEnumerator Start()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }
        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
    }
}
