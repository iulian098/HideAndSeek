using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon;
using UnityEngine.UI;
using TMPro;
using System;
using Michsky.UI.ModernUIPack;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviourPunCallbacks
{
    public static MainMenu instance;

    [System.Serializable]
    public class Panels
    {
        public GameObject nickNamePanel;
        public GameObject mainMenuPanel;
        public GameObject createRoomPanel;
        public GameObject inRoomPanel;
        public GameObject masterRoomPanel;
        public GameObject lobbyPanel;
        public GameObject connectingPanel;
        public GameObject roomListPanel;
        public Transform mapsContainer;
    }

    [System.Serializable]
    public class SettingsObjects
    {
        public TMP_InputField NickName;
        public TMP_Dropdown quality;
        public TMP_Dropdown vSync;
        public TMP_Dropdown antialiasing;

        public Slider masterVolume;
        public Slider shadowDistance;
        public Slider shadowCascades;
        public Slider renderScale;

        public Toggle postProcessing;
        public Toggle fullScreen;
    }

    [System.Serializable]
    public class PlayerList
    {
        public int lastPlayersNumber;
        public Transform hidersContainer;
        public Transform seekersContainer;
        public GameObject playerNamePrefab;
        public List<GameObject> players;
    }
    [System.Serializable]
    public class CharacterSelectionMenu
    {
        public GameObject buttonPrefab;
        public Transform buttonsContainer;
    }

    [System.Serializable]
    public class RoomListMenu
    {
        public Transform container;
        public GameObject roomButtonPrefab;
        public List<RoomButton> roomListObj;
    }

    [System.Serializable]
    public class VideoSettings
    {
        public TMP_Dropdown resolutionList;
        public TMP_Dropdown vsyncList;
    }


    #region Public
    public CharacterSelectionMenu characterSelection;
    public SettingsObjects _settingsObjects;
    public Panels _panels;
    public RoomListMenu roomListMenu;
    public VideoSettings settings;
    public PlayerList playerList;
    public TMP_Text debugTxt;
    public TMP_InputField roomName;
    public TMP_InputField maxPlayers;
    public TMP_InputField matchTime;
    public TMP_InputField startingTime;
    public Toggle playersCanTransform;
    
    public GameObject[] masterObjects;
    public GameObject[] masterObjectsDisable;

    #endregion

    #region Private

    GameSettings gs;
    [HideInInspector]
    public MapsData mapsData;
    GameObject selectedCharacterPreview;

    ExitGames.Client.Photon.Hashtable _customPropertiesPlayer = new ExitGames.Client.Photon.Hashtable();
    ExitGames.Client.Photon.Hashtable _customPropertiesRoom = new ExitGames.Client.Photon.Hashtable();

    bool isFullScreen;
    Resolution[] resolutions;

    #endregion

    private void Awake()
    {
        instance = this;
        
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            _panels.connectingPanel.SetActive(false);
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                SetupMasterServerObj();
            }
            _panels.mainMenuPanel.SetActive(false);
            _panels.lobbyPanel.SetActive(true);
            UpdatePlayerList();
        }
        resolutions = Screen.resolutions;
        
        InitResolutions();
        gs = Resources.Load("GameSettings") as GameSettings;
        mapsData = Resources.Load("MapsData") as MapsData;
        PhotonNetwork.AutomaticallySyncScene = true;
        Debug.Log("Connecting");
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("<color=green>Connected</color>");

        LoadSettigns();
        LoadData();

        SpawnCharacterSelectionButtons();
        SpawnMapsButtons();
    }

    #region Save/Load
    void LoadData()
    {
        //Load data
        if (PlayerPrefs.HasKey("MatchTime"))
            gs.matchTime = PlayerPrefs.GetInt("MatchTime");
        else
            gs.matchTime = 300;

        if (PlayerPrefs.HasKey("StartingTime"))
            gs.startingTime = PlayerPrefs.GetInt("StartingTime");
        else
            gs.startingTime = 10;

        roomName.text = PlayerPrefs.GetString("RoomName", $"Room_{UnityEngine.Random.Range(0, 100000)}");

        maxPlayers.text = PlayerPrefs.GetInt("MaxPlayers", 5).ToString();

        if (PlayerPrefs.HasKey("canTransform"))
            gs.playerCanTransform = Convert.ToBoolean(PlayerPrefs.GetInt("canTransform"));
        else
            gs.playerCanTransform = true;

        matchTime.text = gs.matchTime.ToString();
        startingTime.text = gs.startingTime.ToString();
        playersCanTransform.isOn = gs.playerCanTransform;

        if (PlayerPrefs.HasKey("PlayerName") && !PhotonNetwork.InRoom)
        {
            _panels.nickNamePanel.SetActive(false);
            _panels.mainMenuPanel.SetActive(true);
            PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
        }
        else {
            string nickname = $"Guest_{UnityEngine.Random.Range(0, 99999)}";
            PhotonNetwork.NickName = nickname;
            PlayerPrefs.SetString("PlayerName", nickname);
        }
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("canTransform", Convert.ToInt32(gs.playerCanTransform));
        PlayerPrefs.SetString("RoomName", roomName.text);
        PlayerPrefs.SetInt("MaxPlayers", Convert.ToInt32(maxPlayers.text));
        PlayerPrefs.SetInt("StartingTime", gs.startingTime);
        PlayerPrefs.SetInt("MatchTime", gs.matchTime);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("VSync", _settingsObjects.vSync.value);
        PlayerPrefs.SetString("PlayerName", _settingsObjects.NickName.text);
        PlayerPrefs.SetFloat("MasterVolume", _settingsObjects.masterVolume.value);
        PlayerPrefs.SetInt("Antialiasing", _settingsObjects.antialiasing.value);
        PlayerPrefs.SetInt("PostProcessing", Convert.ToInt32(gs.postProcessing));
        PlayerPrefs.SetInt("ShadowDistance", (int)_settingsObjects.shadowDistance.value);
        PlayerPrefs.SetInt("ShadowCascades", (int)_settingsObjects.shadowCascades.value);
        PlayerPrefs.SetFloat("RenderScale", _settingsObjects.renderScale.value);

        PhotonNetwork.NickName = _settingsObjects.NickName.text;

    }

    void LoadSettigns()
    {
        string nickName = PlayerPrefs.GetString("PlayerName");

        int vSync = PlayerPrefs.GetInt("VSync", 0);
        int shadowCascades = PlayerPrefs.GetInt("ShadowCascades", 3);
        int shadowDistance = PlayerPrefs.GetInt("ShadowDistance", 150);
        int antialiasing = PlayerPrefs.GetInt("Antialiasing", 3);
        bool postProcessing = Convert.ToBoolean(PlayerPrefs.GetInt("PostProcessing", 1));

        float renderScale = PlayerPrefs.GetFloat("RenderScale", 100);
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0);


        _settingsObjects.NickName.text = nickName;
        _settingsObjects.vSync.value = vSync;
        _settingsObjects.renderScale.value = renderScale;
        _settingsObjects.shadowCascades.value = shadowCascades;
        _settingsObjects.shadowDistance.value = shadowDistance;
        _settingsObjects.antialiasing.value = antialiasing;
        _settingsObjects.masterVolume.value = masterVolume;
        _settingsObjects.postProcessing.isOn = postProcessing;


        QualitySettings.vSyncCount = vSync;

        gs.postProcessing = postProcessing;

        gs.renderPipeline.shadowDistance = shadowDistance;
        gs.renderPipeline.shadowCascadeCount = shadowCascades;
        gs.renderPipeline.renderScale = renderScale / 100;
        ApplyAntialiasing(antialiasing);

        gs.mixer.SetFloat("MasterVolume", masterVolume);
    }

    #endregion

    #region Pun Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable { { "Team", 0 } };
        PhotonNetwork.JoinLobby();
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        _panels.connectingPanel.SetActive(false);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        PhotonNetwork.CreateRoom(null, new RoomOptions());
        _panels.mainMenuPanel.SetActive(false);
    }

    public override void OnLeftRoom()
    {
        _panels.lobbyPanel.SetActive(false);
        _panels.mainMenuPanel.SetActive(true);
        if (!PhotonNetwork.IsConnected)
        {
            _panels.connectingPanel.SetActive(true);
            PhotonNetwork.ConnectUsingSettings();
        }
        SetupMasterServerObj();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room. Room name: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.IsMasterClient)
        {
            _panels.lobbyPanel.SetActive(true);
            _panels.createRoomPanel.SetActive(false);

            foreach (GameObject go in masterObjects)
                go.SetActive(true);
            foreach (GameObject go in masterObjectsDisable)
                go.SetActive(false);

            UpdateCustomProperties();

        }
        else
        {
            foreach (GameObject go in masterObjects)
            {
                go.SetActive(false);
            }
            _panels.lobbyPanel.SetActive(true);
            _panels.mainMenuPanel.SetActive(false);
        }
        UpdatePlayerList();
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (newMasterClient.IsLocal)
        {
            SetupMasterServerObj();
        }
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Update room list");
        foreach(RoomInfo ri in roomList)
        {

            if (ri.RemovedFromList)
            {
                int index = roomListMenu.roomListObj.FindIndex(x => x.RoomInfo.Name == ri.Name);
                if(index != -1)
                {
                    Destroy(roomListMenu.roomListObj[index].gameObject);
                    roomListMenu.roomListObj.RemoveAt(index);
                }
            }
            else
            {

                GameObject go = Instantiate(roomListMenu.roomButtonPrefab, roomListMenu.container);
                RoomButton rb = go.GetComponent<RoomButton>();

                rb.SetupButton(ri);
                roomListMenu.roomListObj.Add(rb);

            }
        }
    }

    #endregion

    [PunRPC]
    public void UpdatePlayerList()
    {
        ClearPlayerList();
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            GameObject go = Instantiate(playerList.playerNamePrefab);
            if ((int)p.CustomProperties["Team"] == 0)
                go.transform.SetParent(playerList.hidersContainer);
            else
                go.transform.SetParent(playerList.seekersContainer);
            Debug.Log("Player " + p.NickName + " team " + (int)p.CustomProperties["Team"]);
            PlayerName pn = go.GetComponent<PlayerName>();
            pn.Setup(p.NickName);
            playerList.players.Add(go);
        }
        playerList.lastPlayersNumber = PhotonNetwork.CurrentRoom.PlayerCount;
    }

    void ClearPlayerList()
    {
        foreach(GameObject go in playerList.players)
        {
            Destroy(go);
        }
        playerList.players.Clear();
    }

    public void SetPlayerNickName(TMP_InputField input)
    {
        PhotonNetwork.NickName = input.text;
        PlayerPrefs.SetString("PlayerName", input.text);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    void UpdateCustomProperties()
    {
        gs.startingTime = Convert.ToInt32(startingTime.text);
        gs.matchTime = Convert.ToInt32(matchTime.text);
        gs.playerCanTransform = playersCanTransform.isOn;

        SaveData();

        _customPropertiesRoom["canTransform"] = gs.playerCanTransform;
        PhotonNetwork.CurrentRoom.SetCustomProperties(_customPropertiesRoom);
    }


    #region Map Selection

    void SpawnMapsButtons()
    {
        for(int i = 0; i < mapsData.maps.Count; i++)
        {
            GameObject go = Instantiate(mapsData.mapButtonPrefab, _panels.mapsContainer);
            MapButton mapBtn = go.GetComponent<MapButton>();
            mapBtn.Setup(mapsData.maps[i].mapName, mapsData.maps[i].mapIndex, mapsData.maps[i].mapImage);
        } 
    }

    #endregion

    #region Character selection

    void SpawnCharacterSelectionButtons()
    {
        for(int i = 0; i < gs.hiders.Length; i++)
        {
            GameObject btnObj = Instantiate(characterSelection.buttonPrefab, characterSelection.buttonsContainer);
            SelectCharacterBtn btnS = btnObj.GetComponent<SelectCharacterBtn>();
            btnS.Setup(i, gs.hiders[i].name);
        }
    }

    public void ChangeCharacter(int index)
    {
        if (selectedCharacterPreview != null)
            Destroy(selectedCharacterPreview);
        gs.selectedHiderIndex = index;
        selectedCharacterPreview = Instantiate(gs.hidersPreview[gs.selectedHiderIndex], Vector3.zero, Quaternion.identity);
    }

    #endregion

    #region Change Team

    public void MovePlayerToSeekers()
    {
        _customPropertiesPlayer["Team"] = 1;
        PhotonNetwork.LocalPlayer.CustomProperties = _customPropertiesPlayer;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_customPropertiesPlayer);

        Debug.Log("Selected team : " + (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"]);
        photonView.RPC("UpdatePlayerList", RpcTarget.AllBuffered);
    }

    public void MovePlayerToHiders()
    {

        _customPropertiesPlayer["Team"] = 0;
        PhotonNetwork.LocalPlayer.CustomProperties = _customPropertiesPlayer;
        PhotonNetwork.LocalPlayer.SetCustomProperties(_customPropertiesPlayer);

        Debug.Log("Selected team : " + (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"]);
        //UpdatePlayerList();
        photonView.RPC("UpdatePlayerList", RpcTarget.AllBuffered);
    }

    #endregion

    #region Main Functions

    public void CreateRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            UpdateCustomProperties();
            _panels.lobbyPanel.SetActive(true);
            _panels.createRoomPanel.SetActive(false);
        }
        else
            PhotonNetwork.CreateRoom(roomName.text, new RoomOptions { MaxPlayers = Convert.ToByte(maxPlayers.text), IsVisible = true });
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void SetupMasterServerObj()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            foreach (GameObject go in masterObjects)
                go.SetActive(true);
            foreach (GameObject go in masterObjectsDisable)
                go.SetActive(false);
        }
        else
        {
            foreach (GameObject go in masterObjects)
                go.SetActive(false);
            foreach (GameObject go in masterObjectsDisable)
                go.SetActive(true);
        }
    }

    public void CloseCreationMenu()
    {
            _panels.createRoomPanel.SetActive(false);
        if (PhotonNetwork.InRoom)
        {
            _panels.lobbyPanel.SetActive(true);
        }
        else
        {
            _panels.mainMenuPanel.SetActive(true);
        }
    }
    #endregion

    #region Settings

    void InitResolutions()
    {
        List<string> elementsList = new List<string>();
        foreach(Resolution r in resolutions)
        {
            elementsList.Add($"{r.width} x {r.height} {r.refreshRate}Hz");
        }
        settings.resolutionList.AddOptions(elementsList);
    }

    public void ChangeResolution(TMP_Dropdown drop)
    {
        Resolution r = resolutions[drop.value];
        Screen.SetResolution(r.width, r.height, isFullScreen);
    }

    public void ToggleFullscreen(Toggle t)
    {
        isFullScreen = t.isOn;
        Screen.fullScreen = t.isOn;
    }

    public void VSync(TMP_Dropdown d)
    {
        QualitySettings.vSyncCount = d.value;

    }

    public void RenderScale(Slider s)
    {
        gs.renderPipeline.renderScale = s.value / 100;
    }

    public void Antialiasing(TMP_Dropdown d)
    {
        ApplyAntialiasing(d.value);
    }

    void ApplyAntialiasing(int val)
    {
        switch (val)
        {
            case 0:
                gs.renderPipeline.msaaSampleCount = 1;
                break;
            case 1:
                gs.renderPipeline.msaaSampleCount = 2;
                break;
            case 2:
                gs.renderPipeline.msaaSampleCount = 4;
                break;
            case 3:
                gs.renderPipeline.msaaSampleCount = 8;
                break;
        }
    }

    #region Shadow

    public void ChangeShadowCascades(Slider s)
    {
        gs.renderPipeline.shadowCascadeCount = (int)s.value;
    }

    public void ChangeShadowDistance(Slider s)
    {
        gs.renderPipeline.shadowDistance = s.value;
    }

    #endregion

    public void ChangeMasterVolume(Slider s)
    {
        gs.mixer.SetFloat("MasterVolume", s.value);
    }

    public void ChangePostProcessing(Toggle t)
    {
        gs.postProcessing = t.isOn;
    }

    #endregion

    [ContextMenu("ResetSave")]
    void ResetSave() {
        PlayerPrefs.DeleteAll();
    }
}
