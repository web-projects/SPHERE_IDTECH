using IDTechSDK;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigIDTech.Factory;
using IPA.LoggerManager;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services.Devices.IDTech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IPA.CommonInterface.ConfigSphere;
using IPA.CommonInterface.ConfigSphere.Configuration;

namespace IPA.DAL.RBADAL.Services
{
    class Device_VP5300 : Device_IDTech
    {
        /********************************************************************************************************/
        // ATTRIBUTES
        /********************************************************************************************************/
        #region -- attributes --
        internal static string _HASH_SHA1_ID_STR = "01";
        internal static string _ENC_RSA_ID_STR   = "01";

        private IDTechSDK.IDT_DEVICE_Types deviceType;
        private DEVICE_INTERFACE_Types     deviceConnect;
        //private DEVICE_PROTOCOL_Types      deviceProtocol;
        private IDTECH_DEVICE_PID          deviceMode;

        private string serialNumber = "";
        private string EMVKernelVer = "";
        private static DeviceInfo deviceInfo = null;
        #endregion

        public Device_VP5300(IDTECH_DEVICE_PID mode) : base(mode)
        {
            deviceType = IDT_DEVICE_Types.IDT_DEVICE_NEO2;
            deviceMode = mode;
            Debug.WriteLine("device: VP5300 instantiated with PID={0}", deviceMode);
            Logger.debug( "device: August instantiated with PID={0}", deviceMode);
        }

        public override void Configure(object[] settings)
        {
            deviceType    = (IDT_DEVICE_Types) settings[0];
            deviceConnect = (DEVICE_INTERFACE_Types) settings[1];

            // Create Device info object
            deviceInfo = new DeviceInfo();

            PopulateDeviceInfo();
        }

        public byte[] CommandRawMode(string command)
        {
            byte[] response = null;
            RETURN_CODE rt = IDT_SpectrumPro.SharedController.device_sendDataCommand(command, true, ref response);
            return response;
        }

        private bool PopulateDeviceInfo()
        {
            serialNumber = "";
            RETURN_CODE rt = IDT_NEO2.SharedController.config_getSerialNumber(ref serialNumber);

            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.SerialNumber = serialNumber;
                Debug.WriteLine("device INFO[Serial Number]     : {0}", (object) deviceInfo.SerialNumber);
            }
            else
            {
                Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get serialNumber reason={0}", rt);
            }

            string firmwareVersion = "";
            rt = IDT_NEO2.SharedController.device_getFirmwareVersion(ref firmwareVersion);

            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);

                //deviceInfo.Port = firmwareVersion.Substring(firmwareVersion.IndexOf("USB", StringComparison.Ordinal), 7);
                deviceInfo.Port = "USB-HID";
                Debug.WriteLine("device INFO[Port]              : {0}", (object) deviceInfo.Port);
            }
            else
            {
                Debug.WriteLine("DeviceCfg::PopulateDeviceInfo(): failed to get Firmware version reason={0}", rt);
            }

            deviceInfo.ModelName = IDTechSDK.Profile.IDT_DEVICE_String(deviceType, deviceConnect);
            Debug.WriteLine("device INFO[Model Name]        : {0}", (object) deviceInfo.ModelName);

            rt = IDT_SpectrumPro.SharedController.config_getModelNumber(ref deviceInfo.ModelNumber);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.ModelNumber = deviceInfo?.ModelNumber?.Split(' ')[0] ?? "";
                Debug.WriteLine("device INFO[Model Number]      : {0}", (object) deviceInfo.ModelNumber);
            }
            else
            {
                deviceInfo.ModelNumber = deviceInfo.ModelName.Substring(0, 6);
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get Model number reason={0}", rt);
            }

            EMVKernelVer = "";
            rt = IDT_SpectrumPro.SharedController.emv_getEMVKernelVersion(ref EMVKernelVer);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                deviceInfo.EMVKernelVersion = EMVKernelVer;
                Debug.WriteLine("device INFO[EMV KERNEL V.]     : {0}", (object) deviceInfo.EMVKernelVersion);
            }
            else
            {
                Debug.WriteLine("device: PopulateDeviceInfo() - failed to get Model number reason={0}", rt);
            }

            return true;
        }

        private IDTSetStatus DeviceReset()
        {
            var configStatus = new IDTSetStatus { Success = true };
            // WIP: no resets for these device types
            return configStatus;
        }

        public override string ParseFirmwareVersion(string firmwareInfo)
        {
            // Augusta format has no space after V: V1.00
            // Validate the format firmwareInfo see if the version # exists
            var version = firmwareInfo.Substring(firmwareInfo.IndexOf('V') + 1,
                                                 firmwareInfo.Length - firmwareInfo.IndexOf('V') - 1).Trim();
            var mReg = Regex.Match(version, @"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+.S");

            // If the parse succeeded 
            if (mReg.Success)
            {
                version = mReg.Value;
            }
            else
            {
                mReg = Regex.Match(version, @"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+");
                if (mReg.Success)
                {
                    version = mReg.Value;
                }
            }

            return version;
        }

        public override string GetSerialNumber()
        {
           string serialNumber = "";
           RETURN_CODE rt = IDT_VP8800.SharedController.config_getSerialNumber(ref serialNumber);

          if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
          {
              deviceInfo.SerialNumber = serialNumber;
              Debug.WriteLine("device::GetSerialNumber(): {0}", (object) deviceInfo.SerialNumber);
          }
          else
          {
            Debug.WriteLine("device::GetSerialNumber(): failed to get serialNumber e={0}", rt);
          }

          return serialNumber;
        }

        public override string GetFirmwareVersion()
        {
            string firmwareVersion = "";
            RETURN_CODE rt = IDT_NEO2.SharedController.device_getFirmwareVersion(ref firmwareVersion);
            if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
            {
                //deviceInfo.FirmwareVersion = ParseFirmwareVersion(firmwareVersion);
                //firmwareVersion = deviceInfo.FirmwareVersion;
                deviceInfo.FirmwareVersion = firmwareVersion;
                Debug.WriteLine("device INFO[Firmware Version]  : {0}", (object) deviceInfo.FirmwareVersion);
            }
            else
            {
                Debug.WriteLine("device: GetDeviceFirmwareVersion() - failed to get Firmware version reason={0}", rt);
            }
            return firmwareVersion;
        }

        public override DeviceInfo GetDeviceInfo()
        {
            if(deviceMode == IDTECH_DEVICE_PID.VP5300_HID)
            {
                return deviceInfo;
            }

            return base.GetDeviceInfo();
        }

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        public void GetTerminalInfo(ConfigIDTechSerializer serializer)
        {
            try
            {
                string response = null;
                RETURN_CODE rt = IDT_SpectrumPro.SharedController.device_getFirmwareVersion(ref response);

                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.firmware_ver = response;
                }
                response = "";
                rt = IDT_SpectrumPro.SharedController.emv_getEMVKernelVersion(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_ver = response;
                }
                response = "";
                rt = IDT_SpectrumPro.SharedController.emv_getEMVKernelCheckValue(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_checksum = response;
                }
                response = "";
                rt = IDT_SpectrumPro.SharedController.emv_getEMVConfigurationCheckValue(ref response);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && !string.IsNullOrWhiteSpace(response))
                {
                    serializer.terminalCfg.general_configuration.Terminal_info.contact_emv_kernel_configuration_checksum = response;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetTerminalInfo() - exception={0}", (object)ex.Message);
            }
        }

        #region --- IDTECH SERIALIZER ---
        public byte [] GetTerminalData(ConfigIDTechSerializer serializer, ref int exponent)
        {
            byte [] data = null;

            try
            {
                if(serializer.terminalCfg != null)
                {
                    //int id = IDT_SpectrumPro.SharedController.emv_retrieveTerminalID();

                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveTerminalData(ref data);
            
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS && data != null)
                    {
                        CommonInterface.ConfigIDTech.Configuration.TerminalData td = new CommonInterface.ConfigIDTech.Configuration.TerminalData(data);
                        string text = td.ConvertTLVToValuePairs();
                        serializer.terminalCfg.general_configuration.Contact.terminal_data = td.ConvertTLVToString();
                        serializer.terminalCfg.general_configuration.Contact.tags = td.GetTags();
                        // Information From Terminal Data
                        string language = td.GetTagValue("DF10");
                        language = (language.Length > 1) ? language.Substring(0, 2) : "";
                        string merchantName = td.GetTagValue("9F4E");
    ///                merchantName = CardReader.ConvertHexStringToAscii(merchantName);
                        string merchantID = td.GetTagValue("9F16");
    ///                merchantID = CardReader.ConvertHexStringToAscii(merchantID);
                        string terminalID = td.GetTagValue("9F1C");
    ///                terminalID = CardReader.ConvertHexStringToAscii(terminalID);
    ///                AUGUSTA SRED FAILS HERE !!! --- CONFIG ISSUE
                       string exp = td.GetTagValue("5F36");
                       if(exp.Length > 0)
                       {
                          exponent = Int32.Parse(exp);
                       }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetTerminalData() - exception={0}", (object)ex.Message);
            }

            return data;
        }

        public void GetCapkList(ref ConfigIDTechSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [] keys = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPKList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<CommonInterface.ConfigIDTech.Configuration.Capk> CAPKList = new List<CommonInterface.ConfigIDTech.Configuration.Capk>();

                        foreach(byte[] capk in keys.Split(6))
                        {
                            byte[] key = null;

                            rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPK(capk, ref key);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                CommonInterface.ConfigIDTech.Configuration.Capk apk = new CommonInterface.ConfigIDTech.Configuration.Capk(key);
                                CAPKList.Add(apk);
                            }
                        }

                        // Write to Configuration File
                        if(CAPKList.Count > 0)
                        {
                            serializer.terminalCfg.general_configuration.Contact.capk = CAPKList;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetCapkList() - exception={0}", (object)ex.Message);
            }
        }
        
        public void GetAidList(ConfigIDTechSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [][] keys = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveAIDList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<CommonInterface.ConfigIDTech.Configuration.Aid> AidList = new List<CommonInterface.ConfigIDTech.Configuration.Aid>();

                        foreach(byte[] aidName in keys)
                        {
                            byte[] value = null;

                            rt = IDT_SpectrumPro.SharedController.emv_retrieveApplicationData(aidName, ref value);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                CommonInterface.ConfigIDTech.Configuration.Aid aid = new CommonInterface.ConfigIDTech.Configuration.Aid(aidName, value);
                                aid.ConvertTLVToValuePairs();
                                AidList.Add(aid);
                            }
                        }

                        // Write to Configuration File
                        if(AidList.Count > 0)
                        {
                            serializer.terminalCfg.general_configuration.Contact.aid = AidList;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetAidList() - exception={0}", (object)ex.Message);
            }
        }
        #endregion

        #region --- SPHERE SERIALIZER ---

        public override int SetTerminalConfiguration(int majorcfg)
        {
            RETURN_CODE rt = RETURN_CODE.RETURN_CODE_DO_SUCCESS;
            try
            {
                rt = IDT_SpectrumPro.SharedController.emv_setTerminalMajorConfiguration(majorcfg);
                if(rt != RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    Logger.error("DeviceCfg::SetTerminalMajorConfiguration(): failed Error Code=0x{0:X}", (ushort)rt);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: SetTerminalConfiguration() - exception={0}", (object)ex.Message);
            }

            return (int) rt;
        }

        public override int SetTerminalConfiguration(ConfigSphereSerializer serializer)
        {
            RETURN_CODE rt = RETURN_CODE.RETURN_CODE_DO_SUCCESS;
            try
            {
                if(serializer != null)
                {
                    TerminalSettings termsettings = serializer.GetTerminalSettings();
                    string workerstr = termsettings.MajorConfiguration;
                    string majorcfgstr = Regex.Replace(workerstr, "[^0-9.]", string.Empty);
                    if(Int32.TryParse(majorcfgstr, out ref int majorcfgint))
                    {
                        rt = IDT_SpectrumPro.SharedController.emv_setTerminalMajorConfiguration(majorcfgint);
                        if(rt != RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                        {
                            Logger.error("DeviceCfg::SetTerminalMajorConfiguration(): failed Error Code=0x{0:X}", (ushort)rt);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: SetTerminalConfiguration() - exception={0}", (object)ex.Message);
            }
            return (int) rt;
        }

        public override string [] GetTerminalData(int majorcfg)
         {
            string [] data = null;

            try
            {
                RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_setTerminalMajorConfiguration(majorcfg);
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                { 
                    byte [] tlv = null;

                    rt = IDT_SpectrumPro.SharedController.emv_retrieveTerminalData(ref tlv);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<string> collection = new List<string>();

                        Debug.WriteLine("DEVICE TERMINAL DATA ----------------------------------------------------------------------");
                        Dictionary<string, Dictionary<string, string>> dict = Common.processTLV(tlv);
                        foreach(Dictionary<string, string> devCollection in dict.Where(x => x.Key.Equals("unencrypted")).Select(x => x.Value))
                        {
                            foreach(var devTag in devCollection)
                            {
                                // TAG 9F1E: compression support (default: "Terminal")
                                if(devTag.Key.Equals("9F1E", StringComparison.CurrentCultureIgnoreCase) && !devTag.Value.Equals("5465726D696E616C", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    string [] tagValue = new string[devTag.Value.Length / 2];
                                
                                    for(int i = 0, j = 0; i < devTag.Value.Length; i +=2)
                                    { 
                                        tagValue[j++] = devTag.Value.Substring(i, 2);
                                    }
                                    byte [] bytes = tagValue.Select(value => Convert.ToByte(value, 16)).ToArray();
                                    string compressed =  Encoding.GetEncoding(437).GetString(bytes, 0, bytes.Length);
                                    string decompressed = Utils.Decompress(compressed);
                                    collection.Add(string.Format("{0}:{1}", devTag.Key, decompressed));
                                }
                                else
                                { 
                                    collection.Add(string.Format("{0}:{1}", devTag.Key, devTag.Value).ToUpper());
                                }
                            }
                        }
                        data = collection.ToArray();
                    }
                    else
                    {
                        Debug.WriteLine("TERMINAL DATA: emv_retrieveTerminalData() - ERROR={0}", rt);
                    }
                }
                else
                {
                    Debug.WriteLine("TERMINAL DATA: emv_retrieveTerminalData() - ERROR={0}", rt);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetTerminalData() - exception={0}", (object)ex.Message);
            }

            return data;
        }

        public override void ValidateTerminalData(ref ConfigSphereSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [] tlv = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveTerminalData(ref tlv);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("VALIDATE TERMINAL DATA ----------------------------------------------------------------------");

                        // Get Configuration File AID List
                        SortedDictionary<string, string> cfgTerminalData = serializer.GetTerminalData(serialNumber, EMVKernelVer);
                        Dictionary<string, Dictionary<string, string>> dict = Common.processTLV(tlv);

                        bool update = false;

                        // TAGS from device
                        foreach(Dictionary<string, string> devCollection in dict.Where(x => x.Key.Equals("unencrypted")).Select(x => x.Value))
                        {
                            foreach(var devTag in devCollection)
                            {
                                string devTagName = devTag.Key;
                                string cfgTagValue = "";
                                bool tagfound = false;
                                bool tagmatch = false;
                                foreach(var cfgTag in cfgTerminalData)
                                {
                                    // Matching TAGNAME: compare keys
                                    if(devTag.Key.Equals(cfgTag.Key, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        tagfound = true;
                                        //Debug.Write("key: " + devTag.Key);

                                        // Compare value
                                        if(cfgTag.Value.Equals(devTag.Value, StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            tagmatch = true;
                                            //Debug.WriteLine(" matches value: {0}", (object) devTag.Value);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" DOES NOT match value: {0}!={1}", devTag.Value, cfgTag.Value);
                                            cfgTagValue = cfgTag.Value;
                                            update = true;
                                        }
                                        break;
                                    }
                                    if(tagfound)
                                    {
                                        break;
                                    }
                                }
                                if(tagfound)
                                {
                                    Debug.WriteLine("TAG: {0} FOUND AND IT {1}", devTagName.PadRight(6,' '), (tagmatch ? "MATCHES" : "DOES NOT MATCH"));
                                    if(!tagmatch)
                                    {
                                        Debug.WriteLine("{0}!={1}", devTag.Value, cfgTagValue);
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("TAG: {0} NOT FOUND", (object) devTagName.PadRight(6,' '));
                                    update = true;
                                }
                            }
                        }

                        // Update Terminal Data
                        if(update)
                        {
                            try
                            {
                                List<byte[]> collection = new List<byte[]>();
                                foreach(var item in cfgTerminalData)
                                {
                                    byte [] bytes = null;
                                    string payload = string.Format("{0}{1:X2}{2}", item.Key, item.Value.Length / 2, item.Value).ToUpper();
                                    if (System.Text.RegularExpressions.Regex.IsMatch(item.Value, @"[g-zG-Z\x20\x2E]+"))
                                    {
                                        List<byte> byteArray = new List<byte>();
                                        byteArray.AddRange(Device_IDTech.HexStringToByteArray(item.Key));
                                        byte [] item1 = Encoding.ASCII.GetBytes(item.Value);
                                        // TAG 9F1E: compression support
                                        if(item.Key.Equals("9F1E", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            //item1 = Encoding.GetEncoding(437).GetBytes(item.Value);
                                            string compressed = Utils.Compress(item.Value ?? "");
                                            item1 = Encoding.GetEncoding(437).GetBytes(compressed);
                                        }
                                        byte itemLen = Convert.ToByte(item1.Length);
                                        byte [] item2 = new byte[]{ itemLen };
                                        byteArray.AddRange(item2);
                                        byteArray.AddRange(item1);
                                        bytes = new byte[byteArray.Count];
                                        byteArray.CopyTo(bytes);
                                        //Logger.debug( "device: ValidateTerminalData() DATA={0}", BitConverter.ToString(bytes).Replace("-", string.Empty));
                                    }
                                    else
                                    {
                                        bytes = Device_IDTech.HexStringToByteArray(payload);
                                    }
                                    collection.Add(bytes);
                                }

                                var flattenedList = collection.SelectMany(bytes => bytes);
                                byte [] terminalData = flattenedList.ToArray();

                                rt = IDT_SpectrumPro.SharedController.emv_setTerminalData(terminalData);
                                if(rt != RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                                {
                                    Debug.WriteLine("emv_setTerminalMajorConfiguration() error: {0}", rt);
                                    Logger.error( "device: ValidateTerminalData() error={0} DATA={1}", rt, BitConverter.ToString(terminalData).Replace("-", string.Empty));
                                }
                            }
                            catch(Exception ex)
                            {
                                Debug.WriteLine("device: ValidateTerminalData() - exception={0}", (object)ex.Message);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("TERMINAL DATA: emv_retrieveTerminalData() - ERROR=error: {0}", rt);
                    }
                }
                else
                {
                    Debug.WriteLine("TERMINAL DATA: ValidateTerminalData() - SERIALIZER IS NULL");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: ValidateTerminalData() - exception={0}", (object)ex.Message);
            }
        }
        
        public override string [] GetAidList()
         {
            string [] data = null;

                try
                {
                    byte [][] keys = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveAIDList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        List<string> collection = new List<string>();

                        Debug.WriteLine("DEVICE AID LIST ----------------------------------------------------------------------");

                        foreach(byte[] aidName in keys)
                        {
                            byte[] value = null;

                            rt = IDT_SpectrumPro.SharedController.emv_retrieveApplicationData(aidName, ref value);

                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                string devAidName = BitConverter.ToString(aidName).Replace("-", string.Empty).ToUpper();
                                Debug.WriteLine("AID: {0} ===============================================", (object) devAidName);

                                Dictionary<string, Dictionary<string, string>> dict = Common.processTLV(value);
                                List<string> valCollection = new List<string>();

                                // Compare values and replace if not the same
                                foreach(Dictionary<string, string> devCollection in dict.Where(x => x.Key.Equals("unencrypted")).Select(x => x.Value))
                                {
                                    foreach(var devTag in devCollection)
                                    {
                                        valCollection.Add(string.Format("{0}:{1}", devTag.Key, devTag.Value).ToUpper());
                                    }
                                }
                                collection.Add(string.Format("{0}#{1}", devAidName, String.Join(" ", valCollection.ToArray())));
                            }
                        }
                        data = collection.ToArray();
                    }
                    else
                    {
                        Debug.WriteLine("TERMINAL DATA: emv_retrieveAIDList() - ERROR={0}", rt);
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("device: GetTerminalData() - exception={0}", (object)ex.Message);
                }

            return data;
         }

        public override void ValidateAidList(ref ConfigSphereSerializer serializer)
         {
            try
            {
                if(serializer != null)
                {
                    byte [][] keys = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveAIDList(ref keys);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("VALIDATE AID LIST ----------------------------------------------------------------------");

                        // Get Configuration File AID List
                        AIDList aid = serializer.GetAIDList();

                        List<CommonInterface.ConfigSphere.Configuration.Aid> AidList = new List<CommonInterface.ConfigSphere.Configuration.Aid>();

                        foreach(byte[] aidName in keys)
                        {
                            bool delete = true;
                            bool found  = false;
                            bool update = false;
                            KeyValuePair<string, Dictionary<string, string>> cfgCurrentItem = new KeyValuePair<string, Dictionary<string, string>>();
                            string devAidName = BitConverter.ToString(aidName).Replace("-", string.Empty);

                            Debug.WriteLine("AID: {0} ===============================================", (object) devAidName);

                            // Is this item in the approved list?
                            foreach(var cfgItem in aid.Aid)
                            {
                                cfgCurrentItem = cfgItem;
                                if(cfgItem.Key.Equals(devAidName, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    found  = true;
                                    byte[] value = null;

                                    rt = IDT_SpectrumPro.SharedController.emv_retrieveApplicationData(aidName, ref value);

                                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                                    {
                                        Dictionary<string, Dictionary<string, string>> dict = Common.processTLV(value);

                                        // Compare values and replace if not the same
                                        foreach(Dictionary<string, string> devCollection in dict.Where(x => x.Key.Equals("unencrypted")).Select(x => x.Value))
                                        {
                                            foreach(var cfgTag in cfgItem.Value)
                                            {
                                                bool tagfound = false;
                                                string cfgTagName = cfgTag.Key;
                                                string cfgTagValue = cfgTag.Value;
                                                foreach(var devTag in devCollection)
                                                {
                                                    // Matching TAGNAME: compare keys
                                                    if(devTag.Key.Equals(cfgTag.Key, StringComparison.CurrentCultureIgnoreCase))
                                                    {
                                                        tagfound = true;
                                                        //Debug.Write("key: " + devTag.Key);
                                                        update = !cfgTag.Value.Equals(devTag.Value, StringComparison.CurrentCultureIgnoreCase);

                                                        // Compare value and fix it if mismatched
                                                        if(cfgTag.Value.Equals(devTag.Value, StringComparison.CurrentCultureIgnoreCase))
                                                        {
                                                            //Debug.WriteLine("TAG: {0} FOUND AND IT MATCHES", (object) cfgTagName.PadRight(6,' '));
                                                            //Debug.WriteLine(" matches value: {0}", (object) devTag.Value);
                                                        }
                                                        else
                                                        {
                                                            Debug.WriteLine("TAG: {0} FOUND AND IT DOES NOT match value: {1}!={2}", cfgTagName.PadRight(6,' '), cfgTag.Value, devTag.Value);
                                                        }
                                                        break;
                                                    }
                                                }
                                                // No need to continue validating the remaing tags
                                                if(!tagfound || update)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    delete = false;

                                    if(update)
                                    {
                                        byte[] tagCfgName = Device_IDTech.HexStringToByteArray(cfgCurrentItem.Key);

                                        List<byte[]> collection = new List<byte[]>();
                                        foreach(var item in cfgCurrentItem.Value)
                                        {
                                            string payload = string.Format("{0}{1:X2}{2}", item.Key, item.Value.Length / 2, item.Value).ToUpper();
                                            byte [] bytes = Device_IDTech.HexStringToByteArray(payload);
                                            collection.Add(bytes);
                                        }
                                        var flattenedList = collection.SelectMany(bytes => bytes);
                                        byte [] tagCfgValue = flattenedList.ToArray();
                                        CommonInterface.ConfigSphere.Configuration.Aid cfgAid = new CommonInterface.ConfigSphere.Configuration.Aid(tagCfgName, tagCfgValue);
                                        AidList.Add(cfgAid);
                                    }
                                }
                            }

                            // DELETE THIS AID
                            if(delete)
                            {
                                Debug.WriteLine("AID: {0} - DELETE (NOT FOUND)", (object)devAidName.PadRight(14,' '));
                                byte[] tagName = Device_IDTech.HexStringToByteArray(devAidName);
                                rt = IDT_SpectrumPro.SharedController.emv_removeApplicationData(tagName);
                                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                                {
                                    Debug.WriteLine("AID: {0} DELETED", (object) devAidName.PadRight(6,' '));
                                }
                            }
                            else if(!found)
                            {
                                byte[] tagCfgName = Device_IDTech.HexStringToByteArray(cfgCurrentItem.Key);

                                List<byte[]> collection = new List<byte[]>();
                                foreach(var item in cfgCurrentItem.Value)
                                {
                                    string payload = string.Format("{0}{1:X2}{2}", item.Key, item.Value.Length / 2, item.Value).ToUpper();
                                    byte [] bytes = Device_IDTech.HexStringToByteArray(payload);
                                    collection.Add(bytes);
                                }
                                var flattenedList = collection.SelectMany(bytes => bytes);
                                byte [] tagCfgValue = flattenedList.ToArray();
                                CommonInterface.ConfigSphere.Configuration.Aid cfgAid = new CommonInterface.ConfigSphere.Configuration.Aid(tagCfgName, tagCfgValue);
                                AidList.Add(cfgAid);
                            }
                        }

                        // Add missing AID(s)
                        foreach(var aidElement in AidList)
                        {
                            byte [] aidName = aidElement.GetAidName();
                            byte [] aidValue = aidElement.GetAidValue();
                            rt = IDT_SpectrumPro.SharedController.emv_setApplicationData(aidName, aidValue);
                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                string cfgTagName = BitConverter.ToString(aidName).Replace("-", string.Empty);
                                string cfgTagValue = BitConverter.ToString(aidValue).Replace("-", string.Empty);
                                Debug.WriteLine("AID: {0} UPDATED WITH VALUE: {1}", cfgTagName.PadRight(6,' '), cfgTagValue);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("TERMINAL DATA: emv_retrieveAIDList() - ERROR={0}", rt);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: ValidateAidList() - exception={0}", (object)ex.Message);
            }
         }
    
        public override string [] GetCapKList()
         {
            string [] data = null;

            try
            {
                byte [] keys = null;
                RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPKList(ref keys);

                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    List<string> collection = new List<string>();

                    Debug.WriteLine("DEVICE CAPK LIST ----------------------------------------------------------------------");

                    List<byte[]> capkNames = new List<byte[]>();

                    // Convert array to array of arrays
                    for(int i = 0; i < keys.Length; i += 6)
                    {
                        byte[] result = new byte[6];
                        Array.Copy(keys, i, result, 0, 6);
                        capkNames.Add(result); 
                    }

                    foreach(byte[] capkName in capkNames)
                    {
                        string devCapKName = BitConverter.ToString(capkName).Replace("-", string.Empty);
                        Debug.WriteLine("CAPK: {0} ===============================================", (object) devCapKName);

                        byte[] key = null;
                        rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPK(capkName, ref key);
                        if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                        {
                            CommonInterface.ConfigSphere.Configuration.Capk capk = new CommonInterface.ConfigSphere.Configuration.Capk(key);
                            string RID = devCapKName.Substring(0, 10);
                            string Idx = devCapKName.Substring(10, 2);
                            string payload = string.Format("{0}:{1} ", "RID", RID).ToUpper();
                            payload += string.Format("{0}:{1} ", "INDEX", Idx).ToUpper();
                            payload += string.Format("{0}:{1} ", "MODULUS", capk.GetModulus()).ToUpper();
                            collection.Add(string.Format("{0}#{1}", (RID + "-" + Idx), payload).ToUpper());
                            Debug.WriteLine("MODULUS: {0}", (object) capk.GetModulus().ToUpper());
                        }
                    }

                    data = collection.ToArray();
                }
                else
                {
                    Debug.WriteLine("device: emv_retrieveCAPKList() - ERROR={0}", rt);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetTerminalData() - exception={0}", (object)ex.Message);
                throw;
            }

            return data;
         }

        public override void ValidateCapKList(ref ConfigSphereSerializer serializer)
        {
            try
            {
                if(serializer != null)
                {
                    byte [] keys = null;
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPKList(ref keys);
                
                    if (rt == RETURN_CODE.RETURN_CODE_NO_CA_KEY)
                    {
                        keys = new byte[0];
                        rt = RETURN_CODE.RETURN_CODE_DO_SUCCESS;
                    }
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("VALIDATE CAPK LIST ----------------------------------------------------------------------");

                        // Get Configuration File AID List
                        CAPKList capK = serializer.GetCapKList();

                        List<CommonInterface.ConfigSphere.Configuration.Capk> CapKList = new List<CommonInterface.ConfigSphere.Configuration.Capk>();
                        List<byte[]> capkNames = new List<byte[]>();

                        // Convert array to array of arrays
                        for(int i = 0; i < keys.Length; i += 6)
                        {
                            byte[] result = new byte[6];
                            Array.Copy(keys, i, result, 0, 6);
                            capkNames.Add(result); 
                        }

                        foreach(byte[] capkName in capkNames)
                        {
                            bool delete = true;
                            bool found  = false;
                            bool update = false;
                            KeyValuePair<string, CAPK> cfgCurrentItem = new KeyValuePair<string, CAPK>();
                            string devCapKName = BitConverter.ToString(capkName).Replace("-", string.Empty);

                            Debug.WriteLine("CAPK: {0} ===============================================", (object) devCapKName);

                            // Is this item in the approved list?
                            foreach(var cfgItem in capK.CAPK)
                            {
                                cfgCurrentItem = cfgItem;
                                string devRID = cfgItem.Value.RID;
                                string devIdx = cfgItem.Value.Index;
                                string devItem = devRID + devIdx;
                                if(devItem.Equals(devCapKName, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    found  = true;
                                    byte[] value = null;
                                    CommonInterface.ConfigSphere.Configuration.Capk capk = null;

                                    rt = IDT_SpectrumPro.SharedController.emv_retrieveCAPK(capkName, ref value);

                                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                                    {
                                        capk = new CommonInterface.ConfigSphere.Configuration.Capk(value);

                                        // compare modulus
                                        string modulus = cfgItem.Value.Modulus;
                                        update = !modulus.Equals(capk.GetModulus(), StringComparison.CurrentCultureIgnoreCase);
                                        if(!update)
                                        {
                                            // compare exponent
                                            string exponent = cfgItem.Value.Exponent;
                                            update = !exponent.Equals(capk.GetExponent(), StringComparison.CurrentCultureIgnoreCase);
                                        }
                                    }

                                    delete = false;

                                    if(update && capk != null)
                                    {
                                        CapKList.Add(capk);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("    : UP-TO-DATE");
                                    }
                                }
                            }

                            // DELETE CAPK(s)
                            if(delete)
                            {
                                byte[] tagName = Device_IDTech.HexStringToByteArray(devCapKName);
                                rt = IDT_SpectrumPro.SharedController.emv_removeCAPK(tagName);
                                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                                {
                                    Debug.WriteLine("CAPK: {0} DELETED (NOT FOUND)", (object) devCapKName);
                                }
                                else
                                {
                                    Debug.WriteLine("CAPK: {0} DELETE FAILED, ERROR={1}", devCapKName, rt);
                                }
                            }
                            else if(!found)
                            {
                                byte[] tagCfgName = Device_IDTech.HexStringToByteArray(cfgCurrentItem.Key);

                                List<byte[]> collection = new List<byte[]>();
                                string payload = string.Format("{0}{1}{2}{3}{4}{5}{6:X2}{7:X2}{8}",
                                                                cfgCurrentItem.Value.RID, cfgCurrentItem.Value.Index,
                                                                _HASH_SHA1_ID_STR, _ENC_RSA_ID_STR,
                                                                cfgCurrentItem.Value.Checksum, cfgCurrentItem.Value.Exponent,
                                                                (cfgCurrentItem.Value.Modulus.Length / 2) % 256, (cfgCurrentItem.Value.Modulus.Length / 2) / 256,
                                                                cfgCurrentItem.Value.Modulus);
                                byte[] tagCfgValue = Device_IDTech.HexStringToByteArray(payload);
                                CommonInterface.ConfigSphere.Configuration.Capk cfgCapK = new CommonInterface.ConfigSphere.Configuration.Capk(tagCfgValue);
                                CapKList.Add(cfgCapK);
                            }
                        }

                        // Add/Update CAPK(s)
                        foreach(var capkElement in CapKList)
                        {
                            //capkElement.ShowCapkValues();
                            byte [] capkValue = capkElement.GetCapkValue();
                            rt = IDT_SpectrumPro.SharedController.emv_setCAPK(capkValue);
                            if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                            {
                                Debug.WriteLine("CAPK: {0} UPDATED", (object) capkElement.GetCapkName());
                            }
                            else
                            {
                                Debug.WriteLine("CAPK: {0} FAILED TO UPDATE - ERROR={1}", capkElement.GetCapkName(), rt);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("CAPK: emv_retrieveCAPKList() - ERROR={0}", rt);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: ValidateAidList() - exception={0}", (object)ex.Message);
            }
        }

        public override string [] GetConfigGroup(int group)
        {
            string [] data = null;
            return data;
        }

        public override void ValidateConfigGroup(ConfigSphereSerializer serializer, int group)
        {
        }

        #endregion

        public override void GetMSRSettings(ref ConfigIDTechSerializer serializer)
        {
            //TODO
            /*try
            {
                CommonInterface.ConfigIDTech.Configuration.Msr msr = new CommonInterface.ConfigIDTech.Configuration.Msr();
                List<CommonInterface.ConfigIDTech.Configuration.MSRSettings> msr_settings =  new List<CommonInterface.ConfigIDTech.Configuration.MSRSettings>();; 

                foreach(var setting in msr.msr_settings)
                {
                    byte value   = 0;
                    //RETURN_CODE rt = IDT_SpectrumPro.SharedController.msr_getSetting((byte)setting.function_value, ref value);
                    RETURN_CODE rt = IDT_SpectrumPro.SharedController.msr_getSetting(Convert.ToByte(setting.function_id, 16), ref value);

                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        setting.value = value.ToString("x");
                        msr_settings.Add(setting);
                    }
                }

                serializer.terminalCfg.general_configuration.msr_settings = msr_settings;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetMSRSettings() - exception={0}", (object)ex.Message);
            }*/
        }

        public void GetEncryptionControl(ConfigIDTechSerializer serializer)
        {
            //TODO
            /*try
            {
                bool msr = false;
                bool icc = false;
                RETURN_CODE rt = IDT_SpectrumPro.SharedController.config_getEncryptionControl(ref msr, ref icc);
            
                if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    serializer.terminalCfg.general_configuration.Encryption.msr_encryption_enabled = msr;
                    serializer.terminalCfg.general_configuration.Encryption.icc_encryption_enabled = icc;
                    byte format = 0;
                    rt = IDT_SpectrumPro.SharedController.icc_getKeyFormatForICCDUKPT(ref format);
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        string key_format = "None";
                        switch(format)
                        {
                            case 0x00:
                            {
                                key_format = "TDES";
                                break;
                            }
                            case 0x01:
                            {
                                key_format = "AES";
                                break;
                            }
                        }
                        serializer.terminalCfg.general_configuration.Encryption.data_encryption_type = key_format;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: GetEncryptionControl() - exception={0}", (object)ex.Message);
            }*/
        }

        public override void CloseDevice()
         {
            if (Profile.deviceIsInitialized(IDT_DEVICE_Types.IDT_DEVICE_NEO2, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB))
            {
                Profile.closeDevice(IDT_DEVICE_Types.IDT_DEVICE_NEO2, DEVICE_INTERFACE_Types.DEVICE_INTERFACE_USB);
            }
            IDT_Device.stopUSBMonitoring();
         }

        public override void FactoryReset(int majorcfg)
        {
            try
            {
                // TERMINAL DATA
                RETURN_CODE rt = IDT_SpectrumPro.SharedController.emv_removeTerminalData();
                TerminalDataFactory tf = new TerminalDataFactory();
                byte[] term = tf.GetFactoryTerminalData(majorcfg);
                rt = IDT_SpectrumPro.SharedController.emv_setTerminalData(term);
                if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                {
                    Debug.WriteLine("TERMINAL DATA [DEFAULT] ----------------------------------------------------------------------");
                }
                else
                {
                    Debug.WriteLine("TERMINAL DATA [DEFAULT] failed with error code: 0x{0:X}", (ushort) rt);
                }

                // AID
                rt = IDT_SpectrumPro.SharedController.emv_removeAllApplicationData();
                AidFactory factoryAids = new AidFactory();
                Dictionary<byte [], byte []> aid = factoryAids.GetFactoryAids();
                Debug.WriteLine("AID LIST [DEFAULT] ----------------------------------------------------------------------");
                foreach(var item in aid)
                {
                    byte [] name  = item.Key;
                    byte [] value = item.Value;
                    rt = IDT_SpectrumPro.SharedController.emv_setApplicationData(name, value);
                
                    if(rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("AID: {0}", (object) BitConverter.ToString(name).Replace("-", string.Empty));
                    }
                    else
                    {
                        Debug.WriteLine("CAPK: {0} failed Error Code: 0x{1:X}", (ushort) rt);
                    }
                }

                // CAPK
                rt = IDT_SpectrumPro.SharedController.emv_removeAllCAPK();
                CapKFactory factoryCapk = new CapKFactory();
                Dictionary<byte [], byte []> capk = factoryCapk.GetFactoryCapK();
                Debug.WriteLine("CAPK LIST [DEFAULT] ----------------------------------------------------------------------");
                foreach(var item in capk)
                {
                    byte [] name  = item.Key;
                    byte [] value = item.Value;
                    rt = IDT_SpectrumPro.SharedController.emv_setCAPK(value);

                    if (rt == RETURN_CODE.RETURN_CODE_DO_SUCCESS)
                    {
                        Debug.WriteLine("CAPK: {0}", (object) BitConverter.ToString(name).Replace("-", string.Empty).ToUpper());
                    }
                    else
                    {
                        Debug.WriteLine("CAPK: {0} failed Error Code: 0x{1:X}", (ushort) rt);
                    }
                }
                // ICC
                //rt = IDT_SpectrumPro.SharedController.emv_removeAllCRL();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("device: FactoryReset() - exception={0}", (object)ex.Message);
            }
        }

        public override int DataCommand(string command, ref byte [] response, bool calcCRC)
        {
            return (int) IDT_SpectrumPro.SharedController.device_sendDataCommand(command, calcCRC, ref response);
        }

        public override int DataCommandExt(string command, ref byte [] response, bool calcCRC)
        {
            return (int) IDT_SpectrumPro.SharedController.device_sendDataCommand_ext(command, calcCRC, ref response, 60 , false);
        }

        public override int RemoveAllEMV()
        {
            return 0;
        }

        #endregion
    }
}
