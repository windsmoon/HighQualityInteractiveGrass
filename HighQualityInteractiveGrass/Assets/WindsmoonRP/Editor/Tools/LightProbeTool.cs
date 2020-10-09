using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WindsmoonRP.Editor.Tools
{
    public static class LightProbeTool
    {
        #region fileds
        [MenuItem("WindsmoonRP/Light Probe Tool/Set Light Probe Group To All Dynamic Game objects")]
        public static void SetLightProbeGroupToAllDynamicGameObjects()
        {
            var scene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = scene.GetRootGameObjects();

            foreach (GameObject gameObject in gameObjects)
            {
                MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    if (GameObjectUtility.AreStaticEditorFlagsSet(meshRenderer.gameObject, StaticEditorFlags.ContributeGI) == false)
                    {
                        GameObject lightProbeGroupGO = new GameObject("Light Probe Group GO", typeof(LightProbeGroup));
                        lightProbeGroupGO.transform.position = meshRenderer.gameObject.transform.position;
                    }
                }
            }
        }

        [MenuItem("WindsmoonRP/Light Probe Tool/Remove All Light Probe Group")]
        public static void RemoveAllLightProbeGroup()
        {
            var scene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = scene.GetRootGameObjects();

            foreach (GameObject gameObject in gameObjects)
            {
                LightProbeGroup[] lightProbeGroups = gameObject.GetComponentsInChildren<LightProbeGroup>();

                foreach (LightProbeGroup lightProbeGroup in lightProbeGroups)
                {
                    UnityEngine.Object.DestroyImmediate(lightProbeGroup);
                }
            }
        }
        #endregion
    }
}
