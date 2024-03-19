using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[System.Serializable]
public class Player : MonoBehaviourPunCallbacks
{
    public bool isTransformed;
    public bool isDetected;
    [HideInInspector]
    public bool isDead;
    public float maxHealth = 100;
    public float maxStamina = 100;
    public float health = 100;
    public float stamina = 100;
}
