using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigSphere.Configuration
{

    [Serializable]
    public class EMVDeviceSettings
    {
        [JsonProperty(PropertyName = "ModelFirmware", Order = 1)]
        public Dictionary<string, List<string>> ModelFirmware { get; set; }
        [JsonProperty(PropertyName = "GroupTags", Order = 2)]
        public Dictionary<string, List<string>> GroupTags { get; set; }
        [JsonProperty(PropertyName = "ContactDoNotSendTags", Order = 3)]
        public string[] ContactDoNotSendTags { get; set; }
        [JsonProperty(PropertyName = "ContactOverrideTags", Order = 4)]
        public SortedDictionary<string, string> ContactOverrideTags { get; set; }
        [JsonProperty(PropertyName = "DeviceConfigValues", Order = 5)]
        public Dictionary<string, string> DeviceConfigValues { get; set; }
    }
}
