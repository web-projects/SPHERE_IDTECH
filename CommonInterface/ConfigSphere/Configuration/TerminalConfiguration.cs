using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace IPA.CommonInterface.ConfigSphere.Configuration
{
    [Serializable]
    public class TerminalConfiguration
    {
        public List<DeviceConfiguration> Configuration { get; set; }
    }

    [Serializable]
    public class DeviceConfiguration
    {
        [JsonProperty(PropertyName = "ConfigurationID", Order = 1)]
        public ConfigurationID ConfigurationID { get; set; }
        [JsonProperty(PropertyName = "ContactEMVConfiguration", Order = 2)]
        public ContactEMVConfiguration ContactEMVConfiguration { get; set; }
        [JsonProperty(PropertyName = "CAPKList", Order = 3)]
        public Dictionary<string, CAPK> CAPKList { get; set; }
        [JsonProperty(PropertyName = "CRLList", Order = 4)]
        public Dictionary<string, CRL> CRLList { get; set; }
        [JsonProperty(PropertyName = "TransactionData", Order = 5)]
        public TransactionData TransactionData { get; set; }
        [JsonProperty(PropertyName = "EMVDeviceSettings", Order = 6)]
        public List<EMVDeviceSettings> EMVDeviceSettings { get; set; }
        [JsonProperty(PropertyName = "ContactlessConfiguration", Order = 7)]
        public ContactlessConfiguration ContactlessConfiguration { get; set; }
    }

    [Serializable]
    public class ContactEMVConfiguration
    {
        [JsonProperty(PropertyName = "AIDList", Order = 1)]
        public Dictionary<string, Dictionary<string, string>> AIDList { get; set; }
        [JsonProperty(PropertyName = "TerminalSettings", Order = 2)]
        public TerminalSettings TerminalSettings { get; set; }
    }

    [Serializable]
    public class AIDList
    {
        public Dictionary<string, Dictionary<string, string>> Aid { get; set; }
    }

    [Serializable]
    public class CAPKList : IEnumerator, IEnumerable
    {
        int position = -1;

        public Dictionary<string, CAPK> CAPK { get; set; }

        //IEnumerator and IEnumerable require these methods.
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        //IEnumerator
        public bool MoveNext()
        {
            position++;
            return (position < CAPK.Count());
        }

        //IEnumerable
        public void Reset()
        { position = 0; }

        //IEnumerable
        public object Current
        {
            get { return CAPK.ElementAt(position); }
        }
    }

    [Serializable]
    public class CAPK
    {
        [JsonProperty(PropertyName = "RID", Order = 1)]
        public string RID { get; set; }
        [JsonProperty(PropertyName = "Index", Order = 2)]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "Modulus", Order = 3)]
        public string Modulus { get; set; }
        [JsonProperty(PropertyName = "Exponent", Order = 4)]
        public string Exponent { get; set; }
        [JsonProperty(PropertyName = "Checksum", Order = 5)]
        public string Checksum { get; set; }
    }

    [Serializable]
    public class CRL
    {
        [JsonProperty(PropertyName = "RID", Order = 1)]
        public string RID { get; set; }
        [JsonProperty(PropertyName = "Index", Order = 2)]
        public string Index { get; set; }
        [JsonProperty(PropertyName = "SerialNumber", Order = 3)]
        public string SerialNumber { get; set; }
    }

    [Serializable]
    public class TerminalSettings
    {
        [JsonProperty(PropertyName = "MajorConfiguration", Order = 1)]
        public string MajorConfiguration { get; set; }
        public List<string> MajorConfigurationChecksum { get; set; }
        [JsonProperty(PropertyName = "SerialNumberTag", Order = 2)]
        public string SerialNumberTag { get; set; }
        [JsonProperty(PropertyName = "KernelVersionTag", Order = 3)]
        public string KernelVersionTag { get; set; }
        [JsonProperty(PropertyName = "TerminalData", Order = 4)]
        public SortedDictionary<string, string> TerminalData { get; set; }
        [JsonProperty(PropertyName = "TransactionTagsRequested", Order = 5)]
        public string [] TransactionTagsRequested { get; set; }
    }

    [Serializable]
    public class TransactionData
    {
        [JsonProperty(PropertyName = "EMVKernelMapping", Order = 1)]
        public Dictionary<string, string> EMVKernelMapping { get; set; }
        [JsonProperty(PropertyName = "TransactionStartTags", Order = 2)]
        public List<string> TransactionStartTags { get; set; }
        [JsonProperty(PropertyName = "TransactionAuthenticateTags", Order = 3)]
        public List<string> TransactionAuthenticateTags { get; set; }
        [JsonProperty(PropertyName = "TransactionCompleteTags", Order = 4)]
        public List<string> TransactionCompleteTags { get; set; }
    }

    [Serializable]
    public class EMVGroupTags : IEnumerable
    {
        public Dictionary<string, List<string>> Tags { get; set; }
        public EMVGroupTags(Dictionary<string, List<string>> tags)
        {
            Tags = tags;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)Tags).GetEnumerator();
        }
    }

    [Serializable]
    public class ContactlessConfiguration
    {
        [JsonProperty(PropertyName = "GroupModelFirmware", Order = 1)]
        public Dictionary<string, Dictionary<string, List<string>>> GroupModelFirmware { get; set; }
        [JsonProperty(PropertyName = "GroupList", Order = 2)]
        public Dictionary<string, ContactlessConfigurationGroup> GroupList { get; set; }
    }

    [Serializable]
    public class ContactlessConfigurationGroup
    {
        [JsonProperty(PropertyName = "Group", Order = 1)]
        public Dictionary<string, string> Group { get; set; }

        [JsonProperty(PropertyName = "AIDList", Order = 2)]
        public string [] AIDList { get; set; }
        [JsonProperty(PropertyName = "ApplicationFlow", Order = 3)]
        public string ApplicationFlow {  get; set; }
        [JsonProperty(PropertyName = "TagValues", Order = 4)]
        public Dictionary<string, string> TagValues { get; set; }
        [JsonProperty(PropertyName = "SuppressTagList", Order = 5)]
        public string [] SuppressTagList { get; set; }
    }
}
