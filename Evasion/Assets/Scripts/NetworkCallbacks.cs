using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Server, "Level")]
public class NetworkCallbacks : Bolt.GlobalEventListener
{
    BoltConnection client = null;
    BoltEntity clientPlayer = null;

    public override void SceneLoadLocalDone(string scene)
    {
        BoltNetwork.Instantiate(BoltPrefabs.Prisoner, new Vector3(8, 0, 18), Quaternion.Euler(0, 180, 0));
    }

    public override void SceneLoadRemoteDone(BoltConnection connection)
    {
        if (client == null)
        {
            client = connection;
            clientPlayer = BoltNetwork.Instantiate(BoltPrefabs.Prisoner, new Vector3(8, 0, 3), Quaternion.Euler(0, 0, 0));
            clientPlayer.AssignControl(client);
        }
        else
            connection.Disconnect();
    }

    public override void Disconnected(BoltConnection connection)
    {
        if (connection == client)
        {
            client = null;
            BoltNetwork.Destroy(clientPlayer);
            clientPlayer = null;
        }
    }
}