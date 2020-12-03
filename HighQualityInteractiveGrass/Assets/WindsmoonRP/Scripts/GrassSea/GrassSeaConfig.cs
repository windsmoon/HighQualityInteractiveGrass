using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WindsmoonRP.GrassSea
{
    [CreateAssetMenu(menuName = "WindsmoonRP/Create Grass Sea Config")]
    public class GrassSeaConfig : ScriptableObject
    {
        #region fields
        [SerializeField]
        private WindFieldSettings windFieldSettings;
        #endregion

        #region properties
        public WindFieldSettings WindFieldSettings
        {
            get => windFieldSettings;
        }
        #endregion
    }
}
