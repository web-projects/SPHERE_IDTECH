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
        [JsonProperty(PropertyName = "Manufacturer", Order = 1)]
        public string Manufacturer { get; set; }
        [JsonProperty(PropertyName = "Models", Order = 2)]
        public string[] Models { get; set; }
        [JsonProperty(PropertyName = "Platform", Order = 3)]
        public string Platform { get; set; }
        [JsonProperty(PropertyName = "CardEnvironment", Order = 4)]
        public string CardEnvironment { get; set; }
        [JsonProperty(PropertyName = "EntryModes", Order = 5)]
        public string[] EntryModes { get; set; }
    }

}
