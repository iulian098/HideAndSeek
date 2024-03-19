using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    #region Class

    public enum EventCode : byte
    {
        RefreshTimer,
        RefreshStartingTimer,
        RefreshPlayers
    }

    [System.Serializable]
    public class Texts
    {
        public TMP_Text timerText;
        public TMP_Text startingTimerText;
        public TMP_Text winText;
        public TMP_Text debugText;
    }

    [System.Serializable]
    public class UIPanels
    {
        public GameObject usePanel;
        public GameObject transformPanel;
    }

    [System.Serializable]
    public class PlayerData
    {
        public PlayerController pc;
        public int playerID;

        public PlayerData(PlayerController _pc, int _id)
        {
            pc = _pc;
            playerID = _id;
        }
    }

    #endregion

    public static GameManager instance;
    public CinemachineVirtualCamera cvc;

    public UIPanels _panels;

    public List<PlayerData> hidersList;
    public List<PlayerData> seekersList;
    public byte hidersAlive;
    public List<Transform> spawnPointsHiders;
    public List<Transform> spawnPointsSeekers;

    public bool hidersCanTransform;
    public List<PlayerData> allPlayers;
    public int totalPlayers;
    public PlayerController _player;
    public bool spectateMode;
    public byte selectedPlayer;

    [Header("UI")]
    public Texts _text;
    public Image healthBar;
    public Transform killPanel;
    public GameObject[] masterClientObjects;
    public GameObject winPanel;
    public GameObject menuPanel;
    public GameObject seekerStartingTime;

    public PhotonView localPhotonView;

    public bool GameStarted = false;
    public bool GameEnded = false;

    int currentTime;
    public int startingTime;

    private Coroutine timerCoroutine;
    Coroutine startingTimerCoroutine;
    bool cursorLocked = true;
    
    public bool matchStarted = false;
    [HideInInspector]
    public GameSettings gs;
    bool joinAsSpectator;

    private void Awake()
    {
        instance = this;
        gs = Resources.Load("GameSettings") as GameSettings;
    }

    private void Start()
    {

        MenuState();
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("canTransform"))
        {
            hidersCanTransform = (bool)PhotonNetwork.CurrentRoom.CustomProperties["canTransform"];
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MatchStarted"))
        {
            joinAsSpectator = true;
        }

        if (!joinAsSpectator)
        {
            if ((int)PhotonNetwork.LocalPlayer.CustomProperties["Team"] == 0)
            {
                PhotonNetwork.Instantiate(gs.hiders[gs.selectedHiderIndex].name, spawnPointsHiders[Random.Range(0, spawnPointsHiders.Count)].position, Quaternion.identity);
            }
            else
            {
                PhotonNetwork.Instantiate(gs.seekers[0].name, spawnPointsSeekers[Random.Range(0, spawnPointsSeekers.Count)].position, Quaternion.identity);
            }

        }
        else
        {
            ChangePlayerCam();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (GameObject go in masterClientObjects)
            {
                go.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject go in masterClientObjects)
            {
                go.SetActive(false);
            }
        }

        Camera.main.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = gs.postProcessing;

        InitTimer();
        GetAllPlayers();
        photonView.RPC("GetAllPlayers", RpcTarget.All);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLocked = !cursorLocked;
            MenuState();
        }
        if (spectateMode)
        {
            if (selectedPlayer > allPlayers.Count - 1)
                selectedPlayer = System.Convert.ToByte(allPlayers.Count - 1);
            if (Input.GetButtonDown("Fire1"))
            {
                if (selectedPlayer != allPlayers.Count - 1)
                {
                    selectedPlayer++;
                    ChangePlayerCam();
                }
                else
                {
                    selectedPlayer = 0;
                    ChangePlayerCam();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (!matchStarted &&
        (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"] == 1 &&
        !seekerStartingTime.activeSelf)
        {
            seekerStartingTime.SetActive(true);
        }
        if (matchStarted && seekerStartingTime.activeSelf)
        {
            seekerStartingTime.SetActive(false);
        }

        if (_player != null)
            healthBar.fillAmount = _player.health / _player.maxHealth;
    }

    void MenuState()
    {
        if (cursorLocked)
        {
            ShowCursor(false);
            menuPanel.SetActive(false);
        }
        else
        {
            ShowCursor(true);
            menuPanel.SetActive(true);
        }
    }

    [PunRPC]
    public void AddKillStat(string killer, string hider)
    {
        Debug.Log($"AddKillStat {killer}, {hider}");
        GameObject go = Instantiate(gs.KillStat, killPanel);
        TMP_Text KillStat = go.GetComponent<TMP_Text>();
        if (killer != hider)
            KillStat.text = $"<color=red>{killer}</color> killed <color=blue>{hider}</color>";
        else
            KillStat.text = $"<color=red>{killer}</color> killed himself";
    }

    #region Timer

    void InitTimer()
    {
        currentTime = gs.matchTime;
        startingTime = gs.startingTime;
        RefreshStartingTimeUI();
        if (PhotonNetwork.IsMasterClient)
        {
            startingTimerCoroutine = StartCoroutine(startingTimer());
        }
    }

    void StopTimer()
    {
        if(timerCoroutine != null)
            StopCoroutine(timerCoroutine);
    }

    void RefreshTimer_Sender()
    {
        object[] package = new object[] { currentTime };

        PhotonNetwork.RaiseEvent(
            (byte)EventCode.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    void RefreshTimer_Receiver(object[] data)
    {
        currentTime = (int)data[0];
        RefreshTimerUI();
    }

    void RefreshTimerUI()
    {
        int seconds = (int)(currentTime % 60);
        int minutes = (int)(currentTime / 60);
        _text.timerText.text = $"{minutes}:{seconds}";
    }

    IEnumerator timer()
    {
        yield return new WaitForSeconds(1f);

        currentTime -= 1;

        if(currentTime > 0)
        {
            RefreshTimer_Sender();
            //photonView.RPC("CheckHidersAlive", RpcTarget.AllViaServer);
            timerCoroutine = StartCoroutine(timer());
        }
        else
        {
            timerCoroutine = null;
            photonView.RPC("ShowHidersWin", RpcTarget.AllBuffered);
        }
    }

    #endregion

    #region Starting match timer

    [PunRPC]
    void StartMatch()
    {
        matchStarted = true;
    }
    void RefreshStartingTimer_Sender()
    {
        object[] package = new object[] { startingTime, matchStarted };

        PhotonNetwork.RaiseEvent((byte)EventCode.RefreshStartingTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    void RefreshStartingTimer_Receiver(object[] data)
    {
        startingTime = (int)data[0];
        matchStarted = (bool)data[1];
        RefreshStartingTimeUI();
    }

    void RefreshStartingTimeUI()
    {
        _text.startingTimerText.text = "Time left\n" + startingTime.ToString();
        _text.timerText.text = startingTime.ToString();
    }

    IEnumerator startingTimer()
    {
        yield return new WaitForSeconds(1f);
        startingTime--;

        if(startingTime > 1)
        {
            startingTimerCoroutine = StartCoroutine(startingTimer());
        }
        else
        {
            seekerStartingTime.SetActive(false);
            matchStarted = true;
            if(PhotonNetwork.IsMasterClient)
                photonView.RPC("StartMatch", RpcTarget.AllBufferedViaServer);
            startingTimerCoroutine = null;
            timerCoroutine = StartCoroutine(timer());
        }
         RefreshStartingTimer_Sender();
    }

    #endregion

    #region PlayerList
    [PunRPC]
    void GetAllPlayers()
    {
        Debug.Log("New play has joined, refresh player list");
        hidersList.Clear();
        seekersList.Clear();
        allPlayers.Clear();
        GameObject[] hidersGo = GameObject.FindGameObjectsWithTag("Hider");
        GameObject[] seekersGo = GameObject.FindGameObjectsWithTag("Seeker");

        foreach(GameObject go in hidersGo)
        {
            hidersList.Add(new PlayerData(go.GetComponent<PlayerController>(), go.GetPhotonView().ViewID));
        }

        foreach (GameObject go in seekersGo)
        {
            seekersList.Add(new PlayerData(go.GetComponent<PlayerController>(), go.GetPhotonView().ViewID));
        }
        allPlayers.AddRange(hidersList);
        allPlayers.AddRange(seekersList);
    }

    [PunRPC]
    public void CheckHidersAlive()
    {
        Debug.Log("Check hiders alive");
        int Halive = 0;
        int Salive = 0;

        allPlayers.RemoveAll(item => item == null);
        hidersList.RemoveAll(item => item == null);
        seekersList.RemoveAll(item => item == null);

        foreach(PlayerData pd in hidersList)
        {
            if (!pd.pc.isDead || pd.pc != null)
                Halive++;
        }
        foreach(PlayerData pd in seekersList)
        {
            if (!pd.pc.isDead || pd.pc != null)
                Salive++;
        }
        //Debug.Log($"Hiders alive {alive}");
        _text.debugText.text = $"hiders alive {Halive}";
        if (Halive == 0 || hidersList.Count == 0)
            ShowSeekersWin();
        else if (Salive == 0 || seekersList.Count == 0)
            ShowHidersWin();
        else
            return;

    }

    [PunRPC]
    public void RemovePlayerFromList(int id)
    {
        Debug.Log($"Removing player with id {id}");
        allPlayers.RemoveAll(item => item.playerID == id);
        hidersList.RemoveAll(item => item.playerID == id);
        seekersList.RemoveAll(item => item.playerID == id);
        CheckHidersAlive();
    }

    #endregion

    #region Cursor

    void ShowCursor(bool show)
    {
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    #endregion

    public void ChangePlayerCam()
    {
        if (allPlayers.Count > 1)
        {
            cvc.Follow = allPlayers[selectedPlayer].pc.cameraTarget;
            _player = allPlayers[selectedPlayer].pc;
        }
    }

    #region WinPanels

    [PunRPC]
    void ShowHidersWin()
    {
        StopTimer();
#if !UNITY_EDITOR
        winPanel.SetActive(true);
#endif
        _text.winText.text = "Hiders won!";
        ShowCursor(true);
    }

    [PunRPC]
    void ShowSeekersWin()
    {
        StopTimer();
#if !UNITY_EDITOR
        winPanel.SetActive(true);
#endif
        _text.winText.text = "Seekers won!";
        ShowCursor(true);
    }

#endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach(Transform t in spawnPointsHiders)
        {
            Gizmos.DrawSphere(t.position, 0.5f);
        }
        Gizmos.color = Color.red;
        foreach (Transform t in spawnPointsSeekers)
        {
            Gizmos.DrawSphere(t.position, 0.5f);
        }
    }

    #region Button functions

    public void Resume()
    {
        cursorLocked = true;
        MenuState();
    }

    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel(0);
    }

    public void RestartGame()
    {
        PhotonNetwork.DestroyAll();
        PhotonNetwork.LoadLevel(0);
    }

    #endregion

    #region Room

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        photonView.RPC("GetAllPlayers", RpcTarget.All);
    }

    public override void OnJoinedRoom()
    {
        photonView.RPC("GetAllPlayers", RpcTarget.All);
    }

    #endregion

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCode e = (EventCode)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {
            case EventCode.RefreshTimer:
                RefreshTimer_Receiver(o);
                break;
            case EventCode.RefreshStartingTimer:
                RefreshStartingTimer_Receiver(o);
                break;
        }
    }


}
