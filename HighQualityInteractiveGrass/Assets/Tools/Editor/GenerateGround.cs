using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateGround
{
    #region methods
    [MenuItem("HighQualityInteravtiveGrass/GenerateGround")]
    public static void Generate()
    {
        Texture2D heightMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/Textures/HeightMap");
        List<Vector3> vertList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        
        for (int i = 0; i < 250; i++)
        {
            for (int j = 0; j < 250; j++)
            {
                vertList.Add(new Vector3(i, heightMap.GetPixel(i, j).grayscale * 5 , j));
                if (i == 0 || j == 0) continue;
                triangleList.Add(250 * i + j); 
                triangleList.Add(250 * i + j - 1);
                triangleList.Add(250 * (i - 1) + j - 1);
                triangleList.Add(250 * (i - 1) + j - 1);
                triangleList.Add(250 * (i - 1) + j);
                triangleList.Add(250 * i + j);
            }
        }        
        ...
        Mesh mesh = new Mesh();
        m.vertices = verts.ToArray(); 
        m.uv = uvs;
        m.triangles = tris.ToArray();
    }
    #endregion
}
