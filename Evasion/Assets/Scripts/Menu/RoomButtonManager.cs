using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdpKit;
using UnityEngine.UI;

public class RoomButtonManager : Bolt.GlobalEventListener
{
    public UdpSession Session;

    private void Start()
    {
        GetComponentInChildren<Text>().text = "JOIN " + Session.HostName;
    }

    public void OnJoinClick()
    {
        if (Session != null && Session.Source == UdpSessionSource.Photon)
        {
            BoltNetwork.Connect(Session);
        }
    }
}
