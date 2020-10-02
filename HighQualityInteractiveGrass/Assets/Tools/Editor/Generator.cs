using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Generator
{
    #region methods
    [MenuItem("HighQualityInteravtiveGrass/GenerateGround")]
    public static void GenerateGround()
    {
        Texture2D heightMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tools/Textures/HeightMap.png");
        List<Vector3> vertList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        
        for (int i = 0; i < 250; i++)
        {
            for (int j = 0; j < 250; j++)
            {
                vertList.Add(new Vector3(i, heightMap.GetPixel(i, j).grayscale * 20 , j));
                if (i == 0 || j == 0) continue;
                triangleList.Add(250 * i + j); 
                triangleList.Add(250 * i + j - 1);
                triangleList.Add(250 * (i - 1) + j - 1);
                triangleList.Add(250 * (i - 1) + j - 1);
                triangleList.Add(250 * (i - 1) + j);
                triangleList.Add(250 * i + j);
            }
        }        

        Mesh mesh = new Mesh();
        mesh.vertices = vertList.ToArray();
        mesh.triangles = triangleList.ToArray();
        AssetDatabase.CreateAsset(mesh, "Assets/Tools/Meshes/Mesh.mesh");
    }
    #endregion
}
