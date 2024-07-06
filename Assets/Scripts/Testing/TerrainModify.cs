using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainModify : MonoBehaviour
{
    public static TerrainModify instance;
    public enum drawType
    {
        Square,
        Circle,
        FilledCircle,
        Brush
    }
    public drawType _type;
    public bool canDraw;

    public TerrainData terrain;
    public Transform terrainTransform;
    public Transform gizmo;
    public int radius;
    public Texture2D brushTex;

    float[,] heights;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        heights = terrain.GetHeights(0, 0, terrain.heightmapResolution, terrain.heightmapResolution);
        for(int i = 0; i < terrain.heightmapResolution; i++)
        {
            for(int j = 0; j < terrain.heightmapResolution; j++)
            {
                heights[i, j] = 0;
            }
        }
        terrain.SetHeightsDelayLOD(0, 0, heights);
    }

    void Update()
    {
        
        Ray mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        

        if (Input.GetMouseButtonDown(0) && canDraw)
        {
            Vector3 tSize = terrain.size;

            if (Physics.Raycast(mousePos, out hit))
            {
                gizmo.position = hit.point;
            }
            heights = terrain.GetHeights(0, 0, terrain.heightmapResolution, terrain.heightmapResolution);
            Debug.Log($"Width = {terrain.heightmapResolution}, Height = {terrain.heightmapResolution}");
            int xPos = (int)((gizmo.position.z - terrainTransform.position.z) / tSize.z * terrain.heightmapResolution);
            int yPos = (int)((gizmo.position.x - terrainTransform.position.x) / tSize.x * terrain.heightmapResolution);

            if (xPos < 0)
                xPos = 0;
            if (xPos > terrain.heightmapResolution)
                xPos = terrain.heightmapResolution;
            if (yPos < 0)
                yPos = 0;
            if (yPos > terrain.heightmapResolution)
                yPos = terrain.heightmapResolution;
            switch (_type)
            {
                case drawType.Square:
                    DrawSquare(xPos, yPos);
                    break;
                case drawType.Circle:
                    DrawCircle(xPos, yPos);
                    break;
                case drawType.FilledCircle:
                    DrawFilledCircle(xPos, yPos);
                    break;
                case drawType.Brush:
                    DrawBrush(xPos, yPos);
                    break;
                    
            }


            //terrain.SetHeights(0, 0, heights);


            terrain.SetHeightsDelayLOD(0, 0, heights);
        }

    }

    void DrawSquare(int xPos, int yPos)
    {
        for (int i = -radius; i < radius; i++)
        {
            for (int j = -radius; j < radius; j++)
            {
                if(xPos + i >= 0 && xPos + i < terrain.heightmapResolution && yPos + i >= 0 && yPos + i < terrain.heightmapResolution)
                    heights[xPos + i, yPos + j] = 0.01f;
            }
        }
    }

    void DrawCircle(int xPos, int yPos)
    {
        for(int i = 0; i < 360; i += 5)
        {
            int x = Convert.ToInt32(xPos + radius * Mathf.Cos(i));
            int y = Convert.ToInt32(yPos + radius * Mathf.Sin(i));
            heights[x, y] = 0.01f;
        }
    }

    void DrawFilledCircle(int xPos, int yPos)
    {

        for (int i = 0; i < 360; i += 1)
        {
            int x = Convert.ToInt32(radius * Mathf.Cos(i));
            int y = Convert.ToInt32(yPos + radius * Mathf.Sin(i));

            for (int k = -x; k < x; k++)
            {
                float Xsmooth = Mathf.InverseLerp(xPos + radius, xPos, Math.Abs(k) + xPos);
                heights[xPos + k, y] = 0.1f;
            }
        }
    }

    void DrawBrush(int xPos, int yPos)
    {
        brushTex.Reinitialize(radius, radius);
        brushTex.Apply();
        int w = brushTex.width;
        int h = brushTex.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color col = brushTex.GetPixel(x, y);
                float avgCol = (col.r + col.g + col.b) / 3;
                heights[xPos + (x - w / 2), yPos + (y - h / 2)] = 0.01f * avgCol;
            }
        }
    }

    public void ChangeRadius(Slider s)
    {
        radius = (int)s.value;
    }

    public void ChangeType(Dropdown d)
    {
        _type = (drawType)d.value;
    }
}
