using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapButton : MonoBehaviour
{
    public int mapIndex;
    public Image image;
    public Button btn;
    public TMP_Text mapName;

    public void Setup(string _mapName, int _mapIndex, Sprite _icon)
    {
        mapIndex = _mapIndex;
        mapName.text = _mapName;
        image.sprite = _icon;

        btn.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        MainMenu.instance.mapsData.selectedMapIndex = mapIndex;
    }
}
