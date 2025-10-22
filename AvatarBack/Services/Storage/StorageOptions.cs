namespace Avatar_3D_Sentry.Services.Storage
{
    public class StorageOptions
    {
        public string Mode { get; set; } = "Auto"; // Auto | Azure | Local
        public string? AzureConnection { get; set; }

        public ContainerNames Containers { get; set; } = new();
        public int SasExpiryMinutes { get; set; } = 10;

        public LocalPaths Local { get; set; } = new();

        public class ContainerNames
        {
            public string Models { get; set; } = "models";
            public string Logos { get; set; } = "logos";
            public string Backgrounds { get; set; } = "backgrounds";
            public string Audio { get; set; } = "audio";
        }

        public class LocalPaths
        {
            public string Root { get; set; } = "wwwroot";
            public string ModelsPath { get; set; } = "wwwroot/models";
            public string LogosPath { get; set; } = "wwwroot/logos";
            public string BackgroundsPath { get; set; } = "wwwroot/backgrounds";
            public string AudioPath { get; set; } = "Resources/audio";
        }
    }
}
