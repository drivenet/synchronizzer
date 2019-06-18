namespace Synchronizzer.Composition
{
    internal sealed class HostingOptions
    {
        private string? _listen;

        public string? Listen
        {
            get => _listen;
            set
            {
                var listen = value?.Trim();
                if (listen?.Length == 0)
                {
                    listen = null;
                }

                _listen = listen;
            }
        }

        public bool ForceConsoleLogging { get; set; }
    }
}
