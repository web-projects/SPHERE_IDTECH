using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.CommonInterface.Helpers
{
    public enum NOTIFICATION_TYPE
    {
        NT_INITIALIZE_DEVICE          = 1,
        // CONFIGURATION EVENTS
        NT_DEVICE_UPDATE_CONFIG,
        NT_UNLOAD_DEVICE_CONFIGDOMAIN,
        NT_GET_DEVICE_CONTROL_CONFIGURATION,
        NT_SET_DEVICE_CONTROL_CONFIGURATION,
        NT_GET_DEVICE_MSR_CONFIGURATION,
        NT_SET_DEVICE_MSR_CONFIGURATION,
        NT_SET_DEVICE_INTERFACE_TYPE,
        NT_SET_EXECUTE_RESULT,
        NT_SHOW_TERMINAL_DATA,
        NT_SET_TERMINAL_DATA_ERROR,
        NT_SHOW_JSON_CONFIG,
        NT_SHOW_AID_LIST,
        NT_SHOW_CAPK_LIST,
        NT_UPDATE_SETUP_MESSAGE,
        // PROCESSING EVENTS
        NT_SHOW_CONFIG_GROUP,
        NT_UI_ENABLE_BUTTONS,
        NT_ENABLE_MODE_BUTTON,
        NT_SET_EMV_MODE_BUTTON,
        // FIRMWARE UPDATE EVENTS
        NT_FIRMWARE_UPDATE_STEP,
        NT_FIRMWARE_UPDATE_STATUS,
        NT_FIRMWARE_UPDATE_FAILED,
        NT_FIRMWARE_UPDATE_COMPLETE,
        // CARD READER EVENTS
        NT_PROCESS_CARDDATA,
        NT_PROCESS_CARDDATA_ERROR
    }

    [Serializable]
    public class DeviceNotificationEventArgs
    {
        public NOTIFICATION_TYPE NotificationType { get; set; }
        public object [] Message { get; set; }
    }
}
