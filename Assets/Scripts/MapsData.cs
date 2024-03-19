using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MapsData", menuName = "Maps Data")]
public class MapsData : ScriptableObject
{
    [System.Serializable]
    public class MapData
    {
        public string mapName;
        public int mapIndex;
        public Sprite mapImage;
    }

    public int selectedMapIndex;
    public GameObject mapButtonPrefab;
    public List<MapData> maps;

}
