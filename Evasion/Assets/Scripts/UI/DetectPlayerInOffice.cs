using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayerInOffice : MonoBehaviour
{
    private GameManager _gameManager;
    public Action<DetectPlayerInOffice> OnPlayerEnter;

    void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            OnPlayerEnter?.Invoke(this);
        }
    }
}
