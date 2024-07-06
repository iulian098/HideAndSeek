using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectCharacterBtn : MonoBehaviour
{
    public int index;
    public CharacterType _type;
    public Button btn;
    public TMP_Text label;

    public void Setup(int _index, string _text)
    {
        index = _index;
        label.text = _text;
        btn.onClick.AddListener(Select);
    }

    private void Select()
    {
        Debug.Log("Selected character no." + index);
        MainMenu.instance.ChangeCharacter(index);
    }
}
