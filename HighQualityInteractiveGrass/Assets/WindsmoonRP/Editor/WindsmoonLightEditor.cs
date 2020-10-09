using UnityEditor;
using UnityEngine;

namespace WindsmoonRP.Editor
{
    [CustomEditorForRenderPipeline(typeof(Light), typeof(WindsmoonRenderPipelineAsset))]
    public class WindsmoonLightEditor : LightEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
            {
                settings.DrawInnerAndOuterSpotAngle();
                settings.ApplyModifiedProperties();
            }
        }
    }
}