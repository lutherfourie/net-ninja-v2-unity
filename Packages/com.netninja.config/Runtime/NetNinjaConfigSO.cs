using System.Collections.Generic;
using UnityEngine;

namespace NetNinja.Config
{
    [CreateAssetMenu(menuName = "Net Ninja/Config", fileName = "NetNinjaConfig")]
    public class NetNinjaConfigSO : ScriptableObject
    {
        public List<KeyEntry> keys = new List<KeyEntry>();
        [SerializeField] string configHashBadge = "";

        public string ConfigHashBadge
        {
            get => configHashBadge;
            set => configHashBadge = value;
        }

        public double GetDouble(string key, double codeDefault)
        {
            for (int i = 0; i < keys.Count; i++)
                if (keys[i].key == key) return keys[i].value;
            return codeDefault;
        }
    }
}
