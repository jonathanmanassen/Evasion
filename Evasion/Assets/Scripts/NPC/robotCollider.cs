using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class robotCollider : MonoBehaviour
{
    public int damage = 25;

    private NPCStateMachine npc;

    void Start()
    {
        npc = GetComponentInParent<NPCStateMachine>();
    }

    public void isShot(GameObject shooter)
    {
        npc.isShot(shooter, damage);
    }
}
