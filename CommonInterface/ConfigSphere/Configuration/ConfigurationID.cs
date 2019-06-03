using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigSphere.Configuration
{
    [Serializable]
    public class ConfigurationID
    {
        [JsonProperty(PropertyName = "Version", Order = 1)]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "Manufacturer", Order = 2)]
        public string Manufacturer { get; set; }
        [JsonProperty(PropertyName = "Models", Order = 3)]
        public string[] Models { get; set; }
        [JsonProperty(PropertyName = "Platform", Order = 4)]
        public string Platform { get; set; }
        [JsonProperty(PropertyName = "CardEnvironment", Order = 5)]
        public string CardEnvironment { get; set; }
    }

}
