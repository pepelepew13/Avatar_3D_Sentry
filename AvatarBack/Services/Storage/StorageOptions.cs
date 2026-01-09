namespace Avatar_3D_Sentry.Services.Storage
{
    public class StorageOptions
    {
        public string Mode { get; set; } = "Auto"; // Auto | Azure | Local
        public string? AzureConnection { get; set; }

        public ContainerNames Containers { get; set; } = new();
        public int SasExpiryMinutes { get; set; } = 10;
        public int AudioRetentionDays { get; set; } = 7;

        public LocalPaths Local { get; set; } = new();

        public class ContainerNames
        {
            public string Models { get; set; } = "public";
            public string Logos { get; set; } = "public";
            public string Backgrounds { get; set; } = "public";
            public string Videos { get; set; } = "public";
            public string Audio { get; set; } = "tts";
        }

        public class LocalPaths
        {
            public string Root { get; set; } = "wwwroot";
            public string ModelsPath { get; set; } = "wwwroot/models";
            public string LogosPath { get; set; } = "wwwroot/logos";
            public string BackgroundsPath { get; set; } = "wwwroot/backgrounds";
            public string VideosPath { get; set; } = "wwwroot/videos";
            public string AudioPath { get; set; } = "Resources/audio";
        }
    }
}
