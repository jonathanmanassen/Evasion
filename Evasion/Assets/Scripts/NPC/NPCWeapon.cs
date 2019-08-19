using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCWeapon : Bolt.EntityBehaviour<IPlayerState>
{
    public GameObject WeaponPrefab;
    public IWeapon Weapon;

    public override void Attached()
    {
        if (entity.isOwner)
        {
            GameObject o = BoltNetwork.Instantiate(WeaponPrefab);
            Weapon = o.GetComponent<IWeapon>();
            Weapon.AttachToNPC(transform);
        }
    }
}
