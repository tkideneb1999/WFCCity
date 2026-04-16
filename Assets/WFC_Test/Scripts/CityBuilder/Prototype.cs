using UnityEngine;

namespace CityBuilder
{
    [CreateAssetMenu(fileName = "Prototype", menuName = "CityBuilder/Prototype")]
    public class Prototype : ScriptableObject
    {
        public GameObject ToInstantiate;
        // TODO: Make this an array once a proper editor is there
        public string PosXPosYPosZ;
        public string PosXPosYNegZ;
        public string PosXNegYPosZ;
        public string PosXNegYNegZ;
        public string NegXPosYPosZ;
        public string NegXPosYNegZ;
        public string NegXNegYPosZ;
        public string NegXNegYNegZ;
    }
}
