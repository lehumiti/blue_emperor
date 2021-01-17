using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TNet;

public class TNServer : TNEventReceiver
{
    public int S_Port = 2222;
    public int S_TCPPort = 2223;
    public string S_IP = "127.0.0.1";

    protected override void OnConnect(bool success, string msg)
    {
        base.OnConnect(success, msg);
        if(success)
        {
            Debug.Log("Server has been created");

        }
    }
    protected override void OnError(string msg)
    {
        base.OnError(msg);
        Debug.LogError(msg);
    }

    protected override void Awake()
    {
        base.Awake();
        TNManager.onPlayerJoin = OnPlayerJoin;
    }

    private void Start()
    {
        if (TNServerInstance.Start(S_TCPPort, S_Port, "ServerData.dat", false))
        {
            TNManager.Connect();
        }
    }

    protected override void OnPlayerJoin(int channelID, Player p)
    {
        base.OnPlayerJoin(channelID, p);
    }
}
