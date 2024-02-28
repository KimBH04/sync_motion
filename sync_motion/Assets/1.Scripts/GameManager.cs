using System.Collections;
using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviour
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
