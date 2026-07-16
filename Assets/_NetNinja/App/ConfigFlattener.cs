using NetNinja.Config;
using NetNinja.Core;

namespace NetNinja.Adapters
{
    public static class ConfigFlattener
    {
        public static CoreConfig Flatten(NetNinjaConfigSO so)
        {
            var cfg = CoreConfig.CreateDefault();
            if (so == null) return cfg;
            foreach (var e in so.keys)
                if (!string.IsNullOrEmpty(e.key))
                    cfg.Set(e.key, e.value);
            return cfg;
        }
    }
}
