using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
public class RoomButton : MonoBehaviour
{
    string roomName;
    public TMP_Text roomNameText;
    public TMP_Text players;
    public TMP_Text map;

    public Button btn;
    MainMenu mm;
    public RoomInfo RoomInfo { get; private set; }

    public void SetupButton(RoomInfo info)
    {
        mm = MainMenu.instance;
        if (!btn)
            btn = GetComponent<Button>();
        RoomInfo = info;
        roomNameText.text = info.Name;
        roomName = info.Name;
        players.text = info.PlayerCount + "/" + info.MaxPlayers;
        if (info.CustomProperties.ContainsKey("Map"))
        {
            map.text = info.CustomProperties["Map"].ToString();
        }
        else
        {
            map.text = "";
        }

        btn.onClick.AddListener(Join);
    }

    void Join()
    {
        Photon.Pun.PhotonNetwork.JoinRoom(roomName);
        mm._panels.roomListPanel.SetActive(false);
    }
}
