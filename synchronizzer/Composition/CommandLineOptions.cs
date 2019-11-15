namespace Synchronizzer.Composition
{
    internal sealed class CommandLineOptions
    {
        private string? _config;

        private string? _hostingConfig;

        public string Config
        {
            get => _config ?? "appsettings.json";
            set => _config = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public string HostingConfig
        {
            get => _hostingConfig ?? "hostingsettings.json";
            set => _hostingConfig = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
