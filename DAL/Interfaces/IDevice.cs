using System;
using IPA.Core.Data.Entity.Other;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Models;
using System.Collections.Generic;
///using IPA.Core.XO.TCCustAttribute;
using System.Threading;
using System.Threading.Tasks;
using IPA.DAL.RBADAL.Services;
using IPA.CommonInterface;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigSphere;

namespace IPA.DAL.RBADAL.Interfaces
{
    public enum IDeviceMessage
    {
        DeviceBusy = 1,
        Offline    = 2
    }

    interface IDevice
    {
        event EventHandler<NotificationEventArgs> OnNotification;
        
        // Readonly Properties
        bool Connected { get; }
        Core.Data.Entity.Device DeviceInfo { get; }
        Core.Data.Entity.Model ModelInfo { get; }
        
        //Public methods
        void Init(string[] accepted, string[] available, int baudRate, int dataBits);
        void Configure(object[] settings);
        DeviceStatus Connect();
        void Disconnect();
        void Abort(DeviceAbortType abortType);
        void Process(DeviceProcess process);
        void ClearBuffer();

        void BadRead();
        ///Signature Signature();
        bool UpdateDevice(DeviceUpdateType updateType);
        string GetSerialNumber();
        string GetFirmwareVersion();
        DeviceInfo GetDeviceInfo();
        bool Reset();
        ///Task CardRead(string paymentAmount, string promptText, string availableReaders, List<TCCustAttributeItem> attributes, EntryModeType entryModeType);
        Task CardRead(string paymentAmount, string promptText);

        bool ShowMessage(IDeviceMessage deviceMessage, string message); //only be used when displaying message OUTSIDE of the transaction workflow (like device update)

        #region -- keyboard mode overrides --
        // keyboard mode overrides
        bool SetQuickChipMode(bool mode);
        bool SetUSBHIDMode();
        bool SetUSBKeyboardMode();

        void SetVP3000DeviceHidMode();
        void VP3000PingReport();
        #endregion

        /********************************************************************************************************/
        // DEVICE CONFIGURATION
        /********************************************************************************************************/
        #region -- device configuration --

        #region --- IDTECH SERIALIZER ---
        void GetTerminalInfo(ref ConfigIDTechSerializer serializer);
        string [] GetTerminalData(ref ConfigIDTechSerializer serializer, ref int exponent);
        string [] GetAidList(ref ConfigIDTechSerializer serializer);
        string [] GetCapKList(ref ConfigIDTechSerializer serializer);
        #endregion

        #region --- SPHERE SERIALIZER ---
        string[] GetTerminalData(int majorcfg);
        void ValidateTerminalData(ref ConfigSphereSerializer serializer);
        string [] GetAidList();
        void ValidateAidList(ref ConfigSphereSerializer serializer);
        string [] GetCapKList();
        void ValidateCapKList(ref ConfigSphereSerializer serializer);
        #endregion

        void GetMSRSettings(ref ConfigIDTechSerializer serializer);
        void GetEncryptionControl(ref ConfigIDTechSerializer serializer);
        string[] GetConfigGroup(int group);
        void ValidateConfigGroup(ConfigSphereSerializer serializer, int group);
        void CloseDevice();
        void FactoryReset(int majorcfg);
        int DataCommand(string command, ref byte [] response, bool calcCRC);
        int DataCommandExt(string command, ref byte [] response, bool calcCRC);
        int RemoveAllEMV();
        #endregion
    }
}
