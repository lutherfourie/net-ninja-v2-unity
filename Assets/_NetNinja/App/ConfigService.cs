namespace NetNinja.Config
{
    public sealed class ConfigService
    {
        readonly NetNinjaConfigSO _so;
        public ConfigService(NetNinjaConfigSO so) { _so = so; }
        public double GetDouble(string key, double codeDefault) => _so != null ? _so.GetDouble(key, codeDefault) : codeDefault;
    }
}
