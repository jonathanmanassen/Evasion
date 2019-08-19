using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager : Bolt.EntityBehaviour<IDoor>
{
    private Animator _animator;
    public bool _isOpened;
    private bool _isProcessing;
    private BoxCollider collider;

    [SerializeField] private AudioSource _doorOpenSound;
    [SerializeField] private AudioSource _doorCloseSound;
    private void Start()
    {
        _animator = GetComponent<Animator>();
        collider = transform.GetChild(0).GetComponent<BoxCollider>();
    }

    public override void Attached()
    {
        if (!entity.isOwner && state.isOpen)
            TriggerDoor();
    }

    public void TriggerDoor()
    {
        if (_animator && !_isProcessing)
        {
            if (!_isOpened)
            {
                _isProcessing = true;
                _animator.SetTrigger("OpenDoor");
                _doorOpenSound.Play();
                _isOpened = true;
                collider.enabled = false;
            } else
            {
                _isProcessing = true;
                _doorCloseSound.Play(1);
                _animator.enabled = true;
                _isOpened = false;
                collider.enabled = true;
            }
            if (entity.isOwner)
                state.isOpen = _isOpened;
            StartCoroutine(DeactivateIsProcessing());
        }
    }

    private IEnumerator DeactivateIsProcessing()
    {
        yield return new WaitForSeconds(1);
        _isProcessing = false;
    }

    public void PauseAnimation()
    {
        _animator.enabled = false;
    }

}