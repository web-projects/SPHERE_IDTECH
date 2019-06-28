using System;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigSphere;
using System.Threading.Tasks;

namespace IPA.CommonInterface.Interfaces
{
  public interface IDevicePlugIn
  {
    // Device Events back to Main Form
    event EventHandler<DeviceNotificationEventArgs> OnDeviceNotification;

    // INITIALIZATION
    string PluginName { get; }
    void DeviceInit();
    ConfigIDTechSerializer GetConfigIDTechSerializer();
    ConfigSphereSerializer GetConfigSphereSerializer();
    // GUI UPDATE
    string [] GetConfig();
    // NOTIFICATION
    void SetFormClosing(bool state);
    // DATA READER
    void GetCardData();
    void CardReadNextState(object state);
    // Parse Card Data
    string [] ParseCardData(string data);
    // Settings
    void SetDeviceControlConfiguration(object data);
    void GetDeviceMsrConfiguration();
    void SetDeviceMsrConfiguration(object data);
    void SetDeviceInterfaceType(string mode);
    string DeviceCommand(string command, bool notify);
    // Messaging
    string GetErrorMessage(string data);
    // QC EMV Mode
    void DisableQCEmvMode();
    // Configuration Mode
    void SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes mode);
    // TERMINAL DATA
    Task GetSphereTerminalData(int majorcfg);
    bool ConfigFileMatches(int majorcfg);
    bool ConfigFileLoaded();
    // AID
    Task GetAIDList();
    // CAPK
    Task GetCapKList();
    void GetConfigGroup(int group);
    // Firmware Update
    void FirmwareUpdate(string filename, byte[] bytes);
    // Firmware is Updating
    bool FirmwareIsUpdating();
    // Factory Reset
    void FactoryReset(int majorcfg);
  }
}
