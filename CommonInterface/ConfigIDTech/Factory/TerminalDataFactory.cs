﻿using IDTechSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Factory
{
    [Serializable]
    public class TerminalDataFactory
    {
        private byte [] terminalData2C = Common.getByteArray("5F3601029F1A0208409F3501219F33036028C89F4005F000F0A0019F1E085465726D696E616C9F150212349F160F3030303030303030303030303030309F1C0838373635343332319F4E2231303732312057616C6B65722053742E20437970726573732C204341202C5553412EDF260101DF1008656E667265737A68DF110100DF270100DFEE150101DFEE160100DFEE170105DFEE180180DFEE1E08D0DC20D0C41E1400DFEE1F0180DFEE1B083030303130353030DFEE20013CDFEE21010ADFEE2203323C3C");
        private byte [] terminalData4C = Common.getByteArray("9F33036008C89F3501259F40056000F05001DF110101DF260101DF270100DFEE1E08D09C20F0C20E16005F3601029F1A0208409F1E085465726D696E616C9F150212349F160F3030303030303030303030303030309F1C0838373635343332319F4E2231303732312057616C6B65722053742E20437970726573732C204341202C5553412EDFEE150101DFEE160100DFEE170105DFEE180180DFEE1F0180DFEE1B083030303135313030DFEE2203323C3CDF1008656E667265737A68");
        private byte [] terminalData5C = Common.getByteArray("5F3601029F1A0208409F3501219F33036028C89F4005F000F0A0019F1E085465726D696E616C9F150212349F160F3030303030303030303030303030309F1C0838373635343332319F4E2231303732312057616C6B65722053742E20437970726573732C204341202C5553412EDF260101DF1008656E667265737A68DF110100DF270100DFEE150101DFEE160100DFEE170105DFEE180180DFEE1E08D09C20D0C41E1400DFEE1F0180DFEE1B083030303130353030DFEE20013CDFEE21010ADFEE2203323C3C");
        public byte[] GetFactoryTerminalData(int major)
        {
            switch(major)
            {
                case 2:
                {
                    return terminalData2C;
                }
                case 4:
                {
                    return terminalData4C;
                }
                case 5:
                {
                    return terminalData5C;
                }
            }

            return null;
        }
    }
}
