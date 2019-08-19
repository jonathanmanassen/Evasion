// /**
// *	Created by Guillaume HITIER (hit1097@gmail.com)
// *	4/9/2019 7:19 PM
// */
using System;
using UnityEngine;

public interface IWeapon
{
    void AttachToPlayer(Transform player);
    void AttachToNPC(Transform npc);

    float CooldownTime();

    void Shoot();
}
