using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.ConfigIDTech.Configuration
{
  [Serializable]
  public class ControlConfigItem
  {  
    public string Name { get; set; }  
    public string Value { get; set; }

    public int Id { get; set; }
  }

  // BEEP CONTROL
  public enum BEEP_CONTROL
  {
    HARDWARE = 0x00,
    SOFTWARE
  }

  // LED CONTROL
  public enum LED_CONTROL
  {
    MSR = 0x00,
    ICC
  }

  // ENCRYPTION CONTROL
  public enum ENCRYPTION_CONTROL
  {
    MSR = 0x00,
    ICC
  }
}
