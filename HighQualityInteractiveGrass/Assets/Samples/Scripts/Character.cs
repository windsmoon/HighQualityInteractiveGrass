using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Windsmoon.HightQualityInteractiveGrass.Samples
{
    public class Character : MonoBehaviour
    {
        #region fields
        [SerializeField] 
        private float speed = 2f;
        #endregion

        #region unity methods
        private void Update()
        {
            Vector3 translation = Vector3.zero;
        
            if (Input.GetKey(KeyCode.W))
            {
                translation.z = speed * Time.deltaTime;
            }

            else if (Input.GetKey(KeyCode.S))
            {
                translation.z = -speed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.A))
            {
                translation.x = -speed * Time.deltaTime;
            }

            else if (Input.GetKey(KeyCode.D))
            {
                translation.x = speed * Time.deltaTime;
            }
        
            transform.Translate(translation, Space.World);
            Camera.main.transform.Translate(translation, Space.World);
            Vector4[] positions = new Vector4[] {transform.position};
            Shader.SetGlobalInt("_InteracitveObjectsCount", 1);
            Shader.SetGlobalVectorArray("_InteractiveObjects", positions);
        }
        #endregion
    }
}


