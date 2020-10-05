using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Generator : MonoBehaviour
{
    #region fields
    [SerializeField]
    private Transform groundTransform;
    [SerializeField]
    private Mesh groundMesh;
    [SerializeField]
    private GameObject grassPrefab;
    [SerializeField]
    private GameObject grassLOD0;
    [SerializeField]
    private GameObject grassLOD1;
    [SerializeField]
    private GameObject grassLOD2;
    #endregion
    
    #region methods
    [ContextMenu("Generate Grass Sea")]
    public void GenerateGrassSea()
    {
        // Vector3[] vertices = groundMesh.vertices;
        // Matrix4x4 objectToWorldMatrix = groundTransform.localToWorldMatrix;
        // List<Vector3> worldPosList = new List<Vector3>(vertices.Length);
        // GameObject grassGroup = new GameObject("Grass Group");
        // Transform grassGroupTransform = grassGroup.transform;
        //
        //
        // for (int i = 1; i < vertices.Length / 4; ++i)
        // {
        //     // worldPosList.Add(objectToWorldMatrix * vertex);
        //     Vector3 vertex = vertices[i];
        //     Vector3 worldPos = objectToWorldMatrix * vertex;
        //     Instantiate<GameObject>(grassLOD0, worldPos, Quaternion.identity, grassGroupTransform);
        // }

        GameObject grassGroup = new GameObject("Grass Group");
        float grassWidth = 0.05f;

        for (int i = 0; i < 20; ++i)
        {
            for (int j = 0; j < 20; ++j)
            {
                GameObject lod0 = Instantiate<GameObject>(grassPrefab, new Vector3(i * 0.1f - 0.05f, 0, j * 0.1f - 0.05f), Quaternion.identity, grassGroup.transform);
            }
        }
    }
    
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
        AssetDatabase.CreateAsset(mesh, "Assets/Tools/Meshes/GroundMesh.mesh");
    }

    [MenuItem("HighQualityInteravtiveGrass/Generate Grass Mesh")]
    public static void GenerateGrassMesh()
    {
        // v10 v11
        // v8 v9
        // v6 v7
        // v4 v5
        // v2 v3
        // v0 v1
        float grassHeight = 1f;
        float halfGrassWidth = 0.05f;
        int lod0QuadCount = 5;
        int lod1QuadCount = 3;
        int lod2QuadCount = 1;
        float lod0QuadHeight = grassHeight / lod0QuadCount;
        float lod1QuadHeight = grassHeight / lod1QuadCount;
        float lod2QuadHeight = grassHeight / lod2QuadCount;
        float lod0UVHeight = 1f / lod0QuadCount;
        float lod1UVHeight = 1f / lod1QuadCount;
        float lod2UVHeight = 1f / lod2QuadCount;
        
        Vector3 v0 = new Vector3(-halfGrassWidth, 0f, 0f);
        Vector3 v1 = new Vector3(halfGrassWidth, 0f, 0f);
        Vector3 v2 = new Vector3(-halfGrassWidth, lod0QuadHeight, 0f);
        Vector3 v3 = new Vector3(halfGrassWidth, lod0QuadHeight, 0f);
        Vector3 v4 = new Vector3(-halfGrassWidth, lod0QuadHeight * 2, 0f);
        Vector3 v5 = new Vector3(halfGrassWidth, lod0QuadHeight * 2, 0f);
        Vector3 v6 = new Vector3(-halfGrassWidth, lod0QuadHeight * 3, 0f);
        Vector3 v7 = new Vector3(halfGrassWidth, lod0QuadHeight * 3, 0f);
        Vector3 v8 = new Vector3(-halfGrassWidth, lod0QuadHeight * 4, 0f);
        Vector3 v9 = new Vector3(halfGrassWidth, lod0QuadHeight * 4, 0f);
        Vector3 v10 = new Vector3(-halfGrassWidth, lod0QuadHeight * 5, 0f);
        Vector3 v11 = new Vector3(halfGrassWidth, lod0QuadHeight * 5, 0f);
        
        Mesh lod0 = new Mesh();
        lod0.vertices = new[] {v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11};
        lod0.triangles = new[] {3, 1, 0, 0, 2, 3, 5, 3, 2, 2, 4, 5, 7, 5, 4, 4, 6, 7, 9, 7, 6, 6, 8, 9, 11, 9, 8, 8, 10, 11};
        lod0.uv = new[]
        {
            new Vector2(0, 0), new Vector2(1f, 0),
            new Vector2(0, lod0UVHeight), new Vector2(1, lod0UVHeight),
            new Vector2(0, lod0UVHeight * 2), new Vector2(1, lod0UVHeight * 2),
            new Vector2(0, lod0UVHeight * 3), new Vector2(1, lod0UVHeight * 3),
            new Vector2(0, lod0UVHeight * 4), new Vector2(1, lod0UVHeight * 4),
            new Vector2(0, lod0UVHeight * 5), new Vector2(1, lod0UVHeight * 5),
        };
        
        AssetDatabase.CreateAsset(lod0, "Assets/Tools/Meshes/GrassLOD0.mesh");

        
        v0 = new Vector3(-halfGrassWidth, 0f, 0f);
        v1 = new Vector3(halfGrassWidth, 0f, 0f);
        v2 = new Vector3(-halfGrassWidth, lod1QuadHeight, 0f);
        v3 = new Vector3(halfGrassWidth, lod1QuadHeight, 0f);
        v4 = new Vector3(-halfGrassWidth, lod1QuadHeight * 2, 0f);
        v5 = new Vector3(halfGrassWidth, lod1QuadHeight * 2, 0f);
        v6 = new Vector3(-halfGrassWidth, lod1QuadHeight * 3, 0f);
        v7 = new Vector3(halfGrassWidth, lod1QuadHeight * 3, 0f);
        
        Mesh lod1 = new Mesh();
        lod1.vertices = new[] {v0, v1, v2, v3, v4, v5, v6, v7};
        lod1.triangles = new[] {3, 1, 0, 0, 2, 3, 5, 3, 2, 2, 4, 5, 7, 5, 4, 4, 6, 7};
        
        lod1.uv = new[]
        {
            new Vector2(0, 0), new Vector2(1f, 0),
            new Vector2(0, lod1UVHeight), new Vector2(1, lod1UVHeight),
            new Vector2(0, lod1UVHeight * 2), new Vector2(1, lod1UVHeight * 2),
            new Vector2(0, lod1UVHeight * 3), new Vector2(1, lod1UVHeight * 3),
        };
        
        AssetDatabase.CreateAsset(lod1, "Assets/Tools/Meshes/GrassLOD1.mesh");
        
        v0 = new Vector3(-halfGrassWidth, 0f, 0f);
        v1 = new Vector3(halfGrassWidth, 0f, 0f);
        v2 = new Vector3(-halfGrassWidth, lod2QuadHeight, 0f);
        v3 = new Vector3(halfGrassWidth, lod2QuadHeight, 0f);
        
        Mesh lod2 = new Mesh();
        lod2.vertices = new[] {v0, v1, v2, v3};
        lod2.triangles = new[] {3, 1, 0, 0, 2, 3};
        
        lod2.uv = new[]
        {
            new Vector2(0, 0), new Vector2(1f, 0),
            new Vector2(0, lod2UVHeight), new Vector2(1, lod2UVHeight),
        };
        
        AssetDatabase.CreateAsset(lod2, "Assets/Tools/Meshes/GrassLOD2.mesh");
    }
    #endregion
}
