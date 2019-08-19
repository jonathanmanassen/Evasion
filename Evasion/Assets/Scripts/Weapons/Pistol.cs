using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Pistol : Bolt.EntityBehaviour<IWeaponState>, IWeapon
{
    public Transform suppressor;
    public bool hasSuppressor;

    public float soundDistance, suppressedSoundDistance;

    public AudioClip shot, suppressedShot;

    public override void Attached()
    {
        state.OnShoot = OnShoot;
        if (!entity.isOwner)
        {
            if (state.IsPlayer)
                AttachToPlayer(findClosestWithTag("Player"));
            else
                AttachToNPC(findClosestWithTag("NPCparent"));
        }
    }

    public void Awake()
    {
        if (hasSuppressor)
            AttachSilencer();
        else
            DetachSilencer();
    }

    public float CooldownTime()
    {
        return .5f;
    }

    public void AttachSilencer()
    {
        suppressor.gameObject.SetActive(true);
        GetComponent<AudioSource>().maxDistance = suppressedSoundDistance;
        hasSuppressor = true;
    }

    public void DetachSilencer()
    {
        suppressor.gameObject.SetActive(false);
        GetComponent<AudioSource>().maxDistance = soundDistance;
        hasSuppressor = false;
    }

    public void AttachToPlayer(Transform player)
    {
        if (entity.isOwner)
            state.IsPlayer = true;
        GetComponent<Rigidbody>().isKinematic = true;
        Debug.Log(player.gameObject.name);
        Transform hand = player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
        transform.parent = hand;
        transform.localPosition = new Vector3(0, -.04f, -.03f);
        transform.localRotation = Quaternion.Euler(-155, -80, -100);
        player.GetComponent<Inventory>().Weapon = this;
    }

    public void AttachToNPC(Transform npc)
    {
        if (entity.isOwner)
            state.IsPlayer = false;
        GetComponent<Rigidbody>().isKinematic = true;
        Transform hand = npc.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
        transform.parent = hand;
        transform.localPosition = new Vector3(.08f, -.02f, -.012f);
        transform.localRotation = Quaternion.Euler(195, -3, -190);
        npc.GetComponent<Inventory>().Weapon = this;
    }

    public void DetachFromPlayer()
    {
        transform.parent = null;
        GetComponent<Rigidbody>().isKinematic = false;
    }

    public void Shoot()
    {
        if (entity.isOwner)
            state.Shoot();
    }

    private void OnShoot()
    {
        GetComponent<AudioSource>().PlayOneShot(hasSuppressor ? suppressedShot : shot);
    }

    private Transform findClosestWithTag(string tag)
    {
        Transform closest = null;
        foreach (GameObject gm in GameObject.FindGameObjectsWithTag(tag))
        {
            if (closest == null || Vector3.Distance(transform.position, gm.transform.position) <
                Vector3.Distance(transform.position, closest.position))
                closest = gm.transform;
        }
        return (closest);
    }
}
