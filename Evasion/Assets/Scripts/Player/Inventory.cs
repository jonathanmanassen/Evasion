// /**
// *	Created by Guillaume HITIER (hit1097@gmail.com)
// *	4/9/2019 7:19 PM
// */
using System;
using System.Linq;
using UnityEngine;

public class Inventory: Bolt.EntityBehaviour<IPlayerState>
{
    public float pickupRange;
    public GameObject WeaponPrefab;

    public IWeapon Weapon;

    public override void Attached()
    {
        if (entity.isOwner)
        {
            GameObject o = BoltNetwork.Instantiate(WeaponPrefab);
            Weapon = o.GetComponent<IWeapon>();
            Weapon.AttachToPlayer(transform);
        }
    }

    void PickupWeapon()
    {
        Collider[] collisions = Physics.OverlapSphere(transform.position, pickupRange, LayerMask.GetMask("Weapon"));
        Debug.Log($"Hit {collisions.Length} weapons.");

        if (collisions.Length > 0)
        {
            collisions[0].GetComponent<IWeapon>().AttachToPlayer(transform);
        }
    }

    void Update()
    {
        /*
        if (!CompareTag("Player")) return;

        if (Weapon == null && Input.GetKeyUp(KeyCode.E))
        {
            PickupWeapon();
        }
        */
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
