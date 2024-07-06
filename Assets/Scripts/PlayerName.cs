using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerName : MonoBehaviour
{
    public TMP_Text playerName;

    public void Setup(string _pN)
    {
        playerName.text = _pN;
    }
}
