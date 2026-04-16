using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public class PartRegistry
    {
        public static PartRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PartRegistry();
                }
                return _instance;
            }
        }

        private static PartRegistry _instance = null;

        private Dictionary<string, ushort> _partIdMapping = new();
        private ushort _counter = 1;

        public static readonly string AirName = "air";
        public static readonly ushort AirId = 0;
        private PartRegistry()
        {

        }

        public void RegisterPart(string name)
        {
            if(_partIdMapping.ContainsKey(name))
            {
                Debug.LogError($"Part Name already registered: {name}");
                return;
            }

            _partIdMapping.Add(name, _counter++);
        }

        public ushort GetPartId(string name)
        {
            return _partIdMapping[name];
        }

        public bool TryGetPartId(string name, out ushort id)
        {
            return _partIdMapping.TryGetValue(name, out id);
        }
    }
}
