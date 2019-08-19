using System;
using System.Collections;
using System.Collections.Generic;
using UdpKit;
using UnityEngine;
using UnityEngine.UI;

public class Menu : Bolt.GlobalEventListener
{
    [SerializeField] private GameObject _serverInteractions;
    [SerializeField] private InputField _serverNameInputField;

    [SerializeField] private GameObject _clientInteractions;
    [SerializeField] private GameObject _roomButtonPrefab;

    private List<Guid> _roomList;

    private void Awake()
    {
        _roomList = new List<Guid>();
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        BoltLauncher.SetUdpPlatform(new PhotonPlatform());
    }

    public void StartServer()
    {
        _serverInteractions.SetActive(true);
        _clientInteractions.SetActive(false);
    }

    public void CreateRoom()
    {
        if (!String.IsNullOrEmpty(_serverNameInputField.text))
        {
            BoltLauncher.StartServer();
        }
    }

    public void StartClient()
    {
        _serverInteractions.SetActive(false);
        _clientInteractions.SetActive(true);
        BoltLauncher.StartClient();
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
        {
            string matchName = _serverNameInputField.text;

            BoltNetwork.SetServerInfo(matchName, null);
            BoltNetwork.LoadScene("Level");
        }
    }

    public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
    {
        Debug.LogFormat("Session list updated: {0} total sessions", sessionList.Count);

        foreach (var session in sessionList)
        {
            if (!_roomList.Contains(session.Value.Id))
            {
                var roomButtonInstance = Instantiate(_roomButtonPrefab, _clientInteractions.transform);
                roomButtonInstance.GetComponent<RoomButtonManager>().Session = session.Value as UdpSession;
                _roomList.Add(session.Value.Id);
            }
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}