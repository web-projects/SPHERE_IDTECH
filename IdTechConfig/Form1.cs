﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HidLibrary;

// Include DeviceConfiguration.dll in the References list
using IPA.CommonInterface.Interfaces;
using IPA.CommonInterface.Helpers;
using IPA.CommonInterface.ConfigIDTech;
using IPA.CommonInterface.ConfigIDTech.Configuration;
using System.Collections;
using IPA.LoggerManager;
using System.IO;
using System.Runtime.InteropServices;
using IPA.Core.Client.Dal.Models;
using IPA.Core.Shared.Enums;
using IPA.DAL.RBADAL.Services;
using System.Configuration;
using System.Reflection;
using IPA.DAL.RBADAL;
using IPA.CommonInterface.ConfigSphere.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace IPA.MainApp
{
    public enum DEV_USB_MODE
    {
        USB_HID_MODE = 0,
        USB_KYB_MODE = 1
    }

    public partial class Application : Form
    {
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /********************************************************************************************************/
        // ATTRIBUTES SECTION
        /********************************************************************************************************/
        #region -- attributes section --

        const int CONFIG_PANEL_WIDTH = 50;

        public Panel appPnl;

        bool formClosing = false;
        bool loadMSRSettings = false;

        // AppDomain Artifacts
        AppDomainCfg appDomainCfg;
        AppDomain appDomainDevice;
        IDevicePlugIn devicePlugin;
        const string MODULE_NAME = "DeviceConfiguration";
        const string PLUGIN_NAME = "IPA.DAL.RBADAL.DeviceCfg";
        string ASSEMBLY_NAME = typeof(IPA.DAL.RBADAL.DeviceCfg).Assembly.FullName;

        // Application Configuration
        bool tc_show_settings_tab;
        bool tc_show_msrsettings_group;
        bool tc_show_controlsettings_group;
        bool tc_show_raw_mode_tab;
        bool tc_show_terminal_data_tab;
        bool tc_show_json_tab;
        bool tc_show_advanced_tab;
        bool tc_show_advanced_firmware_update;
        bool tc_always_on_top;
        int  tc_transaction_timeout;
        int  tc_configloader_timeout;
        int  tc_transaction_collection_timeout;
        int  tc_minimum_transaction_length;
        int  tc_transaction_start_delay;
        bool isNonAugusta;

        DEV_USB_MODE dev_usb_mode;
        //int loggingLevels;            // 20190508: REVIEW WITH CHANGES TO LOGGER AND DELETE

        // Timers
        Stopwatch stopWatch;

        DateTime configloadStart;
        TimeSpan configloadDuration;

        internal static System.Timers.Timer TransactionTimer { get; set; }
        internal static System.Timers.Timer ConfigLoaderTimer { get; set; }

        Color TEXTBOX_FORE_COLOR;

        System.Timers.Timer FadeTimer;
        List<object> formElements;

        // Always on TOP
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        #endregion

        public Application()
        {
            InitializeComponent();

            this.Text = string.Format("IDTECH Device Discovery Application - Version {0}", Assembly.GetEntryAssembly().GetName().Version);

            // Initial CONFIG Tab Size
            this.tabControlConfiguration.Width += CONFIG_PANEL_WIDTH;

            // Settings Tab
            string show_settings_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_settings_tab"] ?? "false";
            bool.TryParse(show_settings_tab, out tc_show_settings_tab);
            if(!tc_show_settings_tab)
            {
                MaintabControl.TabPages.Remove(SettingstabPage);
            }

            // ControlSettings Group
            string show_controlsettings_group = System.Configuration.ConfigurationManager.AppSettings["tc_show_controlsettings_group"] ?? "false";
            bool.TryParse(show_controlsettings_group, out tc_show_controlsettings_group);
            if(!tc_show_controlsettings_group)
            {
                this.SettingsControlpanel1.Visible = false;
            }

            // MsrSettings Group
            string show_msrsettings_group = System.Configuration.ConfigurationManager.AppSettings["tc_show_msrsettings_group"] ?? "false";
            bool.TryParse(show_msrsettings_group, out tc_show_msrsettings_group);
            if(!tc_show_msrsettings_group)
            {
                this.SettingsMsrpanel1.Visible = false;
            }

            // Raw Mode Tab
            string show_raw_mode_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_raw_mode_tab"] ?? "false";
            bool.TryParse(show_raw_mode_tab, out tc_show_raw_mode_tab);
            if(!tc_show_raw_mode_tab)
            {
                MaintabControl.TabPages.Remove(RawModetabPage);
            }

            // Terminal Data Tab
            string show_terminal_data_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_terminal_data_tab"] ?? "false";
            bool.TryParse(show_terminal_data_tab, out tc_show_terminal_data_tab);
            if(!tc_show_terminal_data_tab)
            {
                MaintabControl.TabPages.Remove(TerminalDatatabPage);
            }

            // Json Tab
            string show_json_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_json_tab"] ?? "false";
            bool.TryParse(show_json_tab, out tc_show_json_tab);
            if(!tc_show_json_tab)
            {
                MaintabControl.TabPages.Remove(JsontabPage);
            }

            // Advanced Tab
            string show_advanced_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_advanced_tab"] ?? "false";
            bool.TryParse(show_advanced_tab, out tc_show_advanced_tab);
            if(!tc_show_advanced_tab)
            {
                MaintabControl.TabPages.Remove(AdvancedtabPage);
            }

            // Advanced Tab: Firmware Update
            string show_advanced_firmware_update = System.Configuration.ConfigurationManager.AppSettings["tc_show_advanced_firmware_update"] ?? "false";
            bool.TryParse(show_advanced_firmware_update, out tc_show_advanced_firmware_update);
            if(!tc_show_advanced_firmware_update)
            {
                this.AdvancedFirmwaregroupBox1.Visible = false;
            }

            // Application Always on Top
            string always_on_top = System.Configuration.ConfigurationManager.AppSettings["tc_always_on_top"] ?? "true";
            bool.TryParse(always_on_top, out tc_always_on_top);

            // Transaction Timer
            tc_transaction_timeout = 2000;
            string transaction_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_transaction_timeout"] ?? "2000";
            int.TryParse(transaction_timeout, out tc_transaction_timeout);

            // Transaction Collection Timer
            tc_transaction_collection_timeout = 5000;
            string transaction_collection_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_transaction_collection_timeout"] ?? "5000";
            int.TryParse(transaction_collection_timeout, out tc_transaction_collection_timeout);

            // Transaction Minimum Length
            tc_minimum_transaction_length = 1000;
            string minimum_transaction_length = System.Configuration.ConfigurationManager.AppSettings["tc_minimum_transaction_length"] ?? "1000";
            int.TryParse(minimum_transaction_length, out tc_minimum_transaction_length);

            // Transaction Delay
            tc_transaction_start_delay = 5000;
            string transaction_delay = System.Configuration.ConfigurationManager.AppSettings["tc_transaction_start_delay"] ?? "5000";
            int.TryParse(transaction_delay, out tc_transaction_start_delay);

            // Configuration Load Timer
            tc_configloader_timeout = 15000;
            string configloader_timeout = System.Configuration.ConfigurationManager.AppSettings["tc_configloader_timeout"] ?? "5000";
            int.TryParse(configloader_timeout, out tc_configloader_timeout);

            // Original Forecolor
            TEXTBOX_FORE_COLOR = ApplicationtxtCardData.ForeColor;

            // Setup Logging
            SetupLogging();
        }

        /********************************************************************************************************/
        // FORM ELEMENTS
        /********************************************************************************************************/
        #region -- form elements --
    
        private void OnFormLoad(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            if(tc_always_on_top)
            {
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }

            this.ConfigurationCollapseButton.BackColor = Color.Transparent;
            this.ConfigurationExpandButton.BackColor = Color.Transparent;

            // Initialize Device
            InitalizeDevice();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            formClosing = true;

            if (devicePlugin != null)
            {
                try
                {
                    devicePlugin.SetFormClosing(formClosing);
                }
                catch(Exception ex)
                {
                    Logger.error("main: Form1_FormClosing() - exception={0}", (object) ex.Message);
                }
            }
        }

        void SetupLogging()
        {
            // 20190508: REVIEW WITH CHANGES TO LOGGER AND DELETE
            /*try
            {
                var logLevels = ConfigurationManager.AppSettings["IPA.DAL.Application.Client.LogLevel"]?.Split('|') ?? new string[0];
                if(logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    string filepath = path + "\\" + logname;

                    foreach(var item in logLevels)
                    {
                        foreach(var level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            loggingLevels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, loggingLevels);
                    Logger.info( "{0} VERSION {1}.", System.IO.Path.GetFileNameWithoutExtension(fullName).ToUpper(), Assembly.GetEntryAssembly().GetName().Version);
                }
            }
            catch(Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", (object) e.Message);
            }*/
            string fullName = Assembly.GetEntryAssembly().Location;
            Logger.info("{0} VERSION {1}.", System.IO.Path.GetFileNameWithoutExtension(fullName).ToUpper(), Assembly.GetEntryAssembly().GetName().Version);
        }

        private void UpdateAppSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }

        private async void SoftBlink(Control ctrl, Color c1, Color c2, short cycleTimeMS, bool backColor)
        {
            Debug.WriteLine("SoftBlink: START");

            short halfCycle = (short)Math.Round(cycleTimeMS * 0.5);

            var sw = new Stopwatch();
            sw.Start();

            while (ctrl.Visible)
            {
                await Task.Delay(1);
                var cycle = sw.ElapsedMilliseconds % cycleTimeMS;
                var per = (double)Math.Abs(cycle - halfCycle) / halfCycle;
                var red = (short)Math.Round((c2.R - c1.R) * per) + c1.R;
                var grn = (short)Math.Round((c2.G - c1.G) * per) + c1.G;
                var blw = (short)Math.Round((c2.B - c1.B) * per) + c1.B;
                var clr = Color.FromArgb(red, grn, blw);
                if (backColor)
                {
                    ctrl.BackColor = clr;
                }
                else
                {
                    ctrl.ForeColor = clr;
                }
            }
            Debug.WriteLine("SoftBlink: STOP");
        }

        private void OnFadeTimerEvent(object sender, ElapsedEventArgs e)
        {
            this.Invoke(new Action(() => FadeOutLabel(sender)));
        }

        private void FadeOutLabel(object sender)
        {
            foreach(var element in formElements)
            {
                System.Windows.Forms.Label label = (System.Windows.Forms.Label) element;
                Debug.WriteLine("main: FadeOutLabel() : brightness={0:0.00}", (object) label.ForeColor.GetBrightness());
                //if (label.ForeColor.GetBrightness() <= 0.01)
                if (label.ForeColor.GetBrightness() >= 1.0)
                {
                    FadeTimer.Enabled = false;
                    label.Visible = false;
                    return;
                }
                //IdTechConfig.HSLColor hsl = new IdTechConfig.HSLColor(label.ForeColor);
                //hsl.SetRGB(label.ForeColor.R, label.ForeColor.G, label.ForeColor.B);
                //hsl.Luminosity -= 0.002;
                //hsl.Luminosity += 0.2;
                //label.ForeColor = (System.Drawing.Color)hsl.ToRgbColor();
                Color fadeColor = label.ForeColor;
                if(fadeColor.R < 250)
                { 
                    fadeColor = Color.FromArgb(fadeColor.R + 50, fadeColor.G + 50, fadeColor.B + 50);
                }
                else
                {
                    fadeColor = Color.FromArgb(255, 255, 255);
                }
                Debug.WriteLine("main: FadeOutLabel() : FADE COLOR={0},{1},{2}", label.ForeColor.R, label.ForeColor.G, label.ForeColor.B);
                this.Invoke((MethodInvoker)delegate()
                {
                    label.ForeColor = fadeColor;
                    //label.BackColor = fadeColor;
                });
            }
        }

        #endregion

        /********************************************************************************************************/
        // DELEGATES SECTION
        /********************************************************************************************************/
        #region -- delegates section --

        protected void OnDeviceNotificationUI(object sender, DeviceNotificationEventArgs args)
        {
            Logger.debug("main: notification type={0}", args.NotificationType);

            switch (args.NotificationType)
            {
                case NOTIFICATION_TYPE.NT_INITIALIZE_DEVICE:
                {
                    break;
                }

                case NOTIFICATION_TYPE.NT_DEVICE_UPDATE_CONFIG:
                {
                    UpdateUI();
                    break;
                }

                case NOTIFICATION_TYPE.NT_UNLOAD_DEVICE_CONFIGDOMAIN:
                {
                    UnloadDeviceConfigurationDomain(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_GET_DEVICE_CONTROL_CONFIGURATION:
                {
                    GetDeviceControlConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_CONTROL_CONFIGURATION:
                {
                    SetDeviceControlConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_GET_DEVICE_MSR_CONFIGURATION:
                {
                    GetDeviceMsrConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_MSR_CONFIGURATION:
                {
                    SetDeviceMsrConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_INTERFACE_TYPE:
                {
                    SetDeviceInterfaceTypeUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_EXECUTE_RESULT:
                {
                    SetExecuteResultUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_TERMINAL_DATA:
                {
                    ShowTerminalDataUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_TERMINAL_DATA_ERROR:
                {
                    ShowTerminalDataErrorUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_JSON_CONFIG:
                {
                    ShowJsonConfigUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_AID_LIST:
                {
                    ShowAidListUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_CAPK_LIST:
                {
                    ShowCapKListUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_UPDATE_SETUP_MESSAGE:
                {
                    UpdateSetupMessageUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SHOW_CONFIG_GROUP:
                {
                    ShowConfigGroupUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_UI_ENABLE_BUTTONS:
                {
                    EnableButtonsUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_ENABLE_MODE_BUTTON:
                {
                    SetModeButtonEnabledUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_EMV_MODE_BUTTON:
                {
                    SetEmvButtonUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_FIRMWARE_UPDATE_STEP:
                {
                    FirmwareUpdateProgressUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_FIRMWARE_UPDATE_STATUS:
                {
                    FirmwareUpdateStatusUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_FIRMWARE_UPDATE_FAILED:
                {
                    FirmwareUpdateFailedUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_FIRMWARE_UPDATE_COMPLETE:
                {
                    EnableMainFormUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_PROCESS_CARDDATA:
                {
                    ProcessCardDataUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_PROCESS_CARDDATA_ERROR:
                {
                    ProcessCardDataErrorUI(sender, args);
                    break;
                }
            }
        }

        #endregion

        /********************************************************************************************************/
        // GUI - DELEGATE SECTION
        /********************************************************************************************************/
        #region -- gui delegate section --

        private void ClearUI()
        {
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(ClearUI);
                Invoke(Callback);
            }
            else
            {
                this.ApplicationlblSerialNumber.Text = "";
                this.ApplicationAdvancedFirmwarelblVersion.Text = "";
                this.ApplicationlblModelName.Text = "";
                this.ApplicationlblModelNumber.Text = "";
                this.ApplicationlblPort.Text = "";
                this.ApplicationtxtCardData.Text = "";
                this.ApplicationtxtCardData.Text = "";
                this.ApplicationtxtCardData.ForeColor = this.ApplicationtxtCardData.BackColor;
                this.ApplicationbtnShowTags.Text = "TAGS";
                this.ApplicationbtnShowTags.Enabled = false;
                this.ApplicationbtnShowTags.Visible = false;
                this.ApplicationlistView1.Visible = false;
            }
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(UpdateUI);
                Invoke(Callback);
            }
            else
            {
                SetConfiguration();
            }
        }

        private void InitalizeDeviceUI(object sender, DeviceNotificationEventArgs e)
        {
            InitalizeDevice(true);
        }

        private void ProcessCardDataUI(object sender, DeviceNotificationEventArgs e)
        {
            ProcessCardData(e.Message);
        }

        private void ProcessCardDataErrorUI(object sender, DeviceNotificationEventArgs e)
        {
            ProcessCardDataError(e.Message[0]);
        }

        private void UnloadDeviceConfigurationDomain(object sender, DeviceNotificationEventArgs e)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // Terminate Transaction Timer if running
                TransactionTimer?.Stop();

                ClearUI();

                bool firmwareIsUpdating = devicePlugin.FirmwareIsUpdating();

                // Unload The Plugin
                appDomainCfg.UnloadPlugin(appDomainDevice);

                // wait for a new device to connect
                WaitForDeviceToConnect(firmwareIsUpdating);

            }).Start();
        }

        private void GetDeviceControlConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            GetDeviceControlConfiguration(e.Message);
        }

        private void SetDeviceControlConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceControlConfiguration(e.Message);
        }

        private void GetDeviceMsrConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            GetDeviceMsrConfiguration(e.Message);
        }

        private void SetDeviceMsrConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceMsrConfiguration(e.Message);
        }

        private void SetDeviceInterfaceTypeUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceInterfaceType(e.Message);
        }

        private void SetExecuteResultUI(object sender, DeviceNotificationEventArgs e)
        {
            SetExecuteResult(e.Message);
        }

        private void ShowJsonConfigUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowJsonConfig(e.Message);
        }

        private void ShowTerminalDataUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowTerminalData(e.Message);
        }

        private void ShowTerminalDataErrorUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowTerminalDataError(e.Message);
        }

        private void ShowAidListUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowAidList(e.Message);
        }

        private void ShowCapKListUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowCapKList(e.Message);
        }

        private void UpdateSetupMessageUI(object sender, DeviceNotificationEventArgs e)
        {
            UpdateSetupMessage(e.Message);
        }

        private void ShowConfigGroupUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowConfigGroup(e.Message);
        }

        private void EnableButtonsUI(object sender, DeviceNotificationEventArgs e)
        {
            EnableButtons();
        }

        private void SetModeButtonEnabledUI(object sender, DeviceNotificationEventArgs e)
        {
            SetModeButtonEnabled(e.Message);
        }

        private void SetEmvButtonUI(object sender, DeviceNotificationEventArgs e)
        {
            SetEmvButton(e.Message);
        }
        private void FirmwareUpdateProgressUI(object sender, DeviceNotificationEventArgs e)
        {
            FirmwareUpdateProgress(e.Message);
        }
        private void FirmwareUpdateStatusUI(object sender, DeviceNotificationEventArgs e)
        {
            FirmwareUpdateStatus(e.Message);
        }
        private void FirmwareUpdateFailedUI(object sender, DeviceNotificationEventArgs e)
        {
            FirmwareUpdateFailed(e.Message);
        }
        private void EnableMainFormUI(object sender, DeviceNotificationEventArgs e)
        {
            EnableMainForm(e.Message);
        }

        #endregion

        /********************************************************************************************************/
        // DEVICE ARTIFACTS
        /********************************************************************************************************/
        #region -- device artifacts --

        private void ClearConfiguration()
        {
            Debug.WriteLine("main: Clear GUI elements =========================================================");
            if (InvokeRequired)
            {
                MethodInvoker Callback = new MethodInvoker(ClearConfiguration);
                Invoke(Callback);
            }
            else
            {
                this.ApplicationlblSerialNumber.Text = "";
                this.ApplicationAdvancedFirmwarelblVersion.Text = "";
                this.ApplicationlblModelName.Text = "";
                this.ApplicationlblModelNumber.Text = "";
                this.ApplicationlblPort.Text = "";
                this.ApplicationtxtCardData.Text = "";
            }
        }

        private void SetConfiguration()
        {
            Debug.WriteLine("main: update GUI elements =========================================================");

            this.ApplicationlblSerialNumber.Text = "";
            this.ApplicationAdvancedFirmwarelblVersion.Text = "";
            this.ApplicationlblModelName.Text = "";
            this.ApplicationlblModelNumber.Text = "";
            this.ApplicationlblPort.Text = "";
            this.ApplicationtxtCardData.Text = "";
            this.ApplicationpictureBoxWait.Enabled = false;
            this.ApplicationpictureBoxWait.Visible = false;

            try
            {
                string[] config = devicePlugin.GetConfig();

                if (config != null)
                {
                    this.ApplicationlblSerialNumber.Text = config[0];
                    this.ApplicationAdvancedFirmwarelblVersion.Text = config[1];
                    this.ApplicationlblModelName.Text = config[2];
                    this.ApplicationlblModelNumber.Text = config[3];

                    this.AdvancedFirmwarelblVersion.Text = config[1];
                    this.AdvancedFirmwarebtnUpdate.Enabled = true;
                    this.AdvancedFirmwarebtnUpdate.Visible = true;
                    this.AdvancedFirmwareprogressBar1.Visible = false;

                    // value expected: either dashed or space separated
                    string [] worker = null;
                    if(config[4] != null)
                    {
                        if(config[4].Trim().Contains(' '))
                        {
                            worker = config[4].Trim().Split(' ');
                        }
                        else
                        {
                            worker = config[4].Trim().Split('-');
                        }
                    }
                    if(worker != null)
                    {
                        string worker1 = worker[0];
                        if(!worker1.Equals("Unknown", StringComparison.CurrentCultureIgnoreCase))
                        {
                            worker1 += "/";
                            if(worker[1].Equals("KB", StringComparison.CurrentCultureIgnoreCase))
                            {
                                worker1 += "KEYBOARD";
                            }
                            else
                            {
                                worker1 += worker[1];
                            }
                        }
                        this.ApplicationlblPort.Text = worker1;
                    }
                    else
                    {
                        this.ApplicationlblPort.Text = "UNKNOWN";
                    }
                }

                // Enable Buttons
                this.ApplicationbtnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                // Enable Tab(s)
                this.ApplicationtabPage.Enabled = true;
                this.ConfigurationtabPage.Enabled = true;
                this.SettingstabPage.Enabled = tc_show_settings_tab;
                this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                this.JsontabPage.Enabled = tc_show_json_tab;
                this.AdvancedtabPage.Enabled = tc_show_advanced_tab;
                this.SettingsMsrpicBoxWait.Enabled = false;
                this.SettingsMsrpicBoxWait.Visible = false;

                // KB Mode
                if (dev_usb_mode == DEV_USB_MODE.USB_KYB_MODE)
                {
                    MaintabControl.SelectedTab = this.ApplicationtabPage;
                    this.ApplicationtxtCardData.ReadOnly = false;
                    this.ApplicationtxtCardData.GotFocus += CardDataTextBoxGotFocus;
                    this.ApplicationtxtCardData.ForeColor = this.ApplicationtxtCardData.BackColor;

                    stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // Transaction Timer
                    SetTransactionTimer();

                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.ApplicationtxtCardData.Focus();
                    }));
                }
                else
                {
                    TransactionTimer?.Stop();
                    this.ApplicationtxtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                    this.ApplicationtxtCardData.ReadOnly = true;
                    this.ApplicationtxtCardData.GotFocus -= CardDataTextBoxGotFocus;
                }
            }
            catch(Exception ex)
            {
                Logger.error("main: SetConfiguration() exception={0}", (object)ex.Message);
            }
        }

        private void InitalizeDevice(bool unload = false)
        {
            // Unload Domain
            if (unload && appDomainCfg != null)
            {
                appDomainCfg.UnloadPlugin(appDomainDevice);

                // Test Unload
                appDomainCfg.TestIfUnloaded(devicePlugin);
            }

            appDomainCfg = new AppDomainCfg();

            // AppDomain Interface
            appDomainDevice = appDomainCfg.CreateAppDomain(MODULE_NAME);

            // Load Interface
            devicePlugin = appDomainCfg.InstantiatePlugin(appDomainDevice, ASSEMBLY_NAME, PLUGIN_NAME);

            // Initialize interface
            if (devicePlugin != null)
            {
                // Disable Tab(s)
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ApplicationtabPage.Enabled = false;
                    this.ConfigurationtabPage.Enabled = false;
                    this.SettingstabPage.Enabled = false;
                    this.RawModetabPage.Enabled = false;
                    this.TerminalDatatabPage.Enabled = false;
                }));

                if(!devicePlugin.FirmwareIsUpdating())
                { 
                    try
                    {
                        if(tc_show_json_tab && dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
                        {
                                this.Invoke(new MethodInvoker(() =>
                                {
                                    if(MaintabControl.Contains(JsontabPage))
                                    {
                                        this.JsonpicBoxWait.Visible = true;
                                        this.MaintabControl.SelectedTab = this.JsontabPage;
                                    }
                                }));
                        }
                        else if(!tc_show_json_tab)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.ApplicationtabPage.Enabled = true;
                                this.MaintabControl.SelectedTab = this.ApplicationtabPage;
                                this.ApplicationpictureBoxWait.Enabled = true;
                                this.ApplicationpictureBoxWait.Visible = true;
                            }));
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.error("main: InitalizeDevice() exception={0}", (object)ex.Message);
                    }
                }

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Debug.WriteLine("\nmain: new device detected! +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");

                    // Setup DeviceCfg Event Handlers
                    devicePlugin.OnDeviceNotification += new EventHandler<DeviceNotificationEventArgs>(this.OnDeviceNotificationUI);

                    System.Windows.Forms.Application.DoEvents();

                    try
                    {
                        // Initialize Device
                        devicePlugin.DeviceInit();
                        Debug.WriteLine("main: loaded plugin={0} ++++++++++++++++++++++++++++++++++++++++++++", (object)devicePlugin.PluginName);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("main: InitalizeDevice() exception={0}", (object)ex.Message);
                        if(ex.Message.Equals("NoDevice"))
                        {
                            WaitForDeviceToConnect(false);
                        }
                        else if (ex.Message.Equals("MultipleDevice"))
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                MessageBoxEx.Show(this, "Multiple Devices Detected\r\nDisconnect One of them !!!", "ERROR: MULTIPLE DEVICES DETECTED", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                WaitForDeviceToConnect(false);
                            }));
                        }
                    }
                }).Start();
            }
        }

        private void WaitForDeviceToConnect(bool firmwareIsUpdating)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                if (!firmwareIsUpdating)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.AdvancedFirmwarebtnUpdate.Enabled = false;
                        this.AdvancedFirmwarelblVersion.Text = "UNKNOWN";

                        this.ApplicationtabPage.Enabled = true;
                        this.MaintabControl.SelectedTab = this.ApplicationtabPage;

                        if(this.MaintabControl.Contains(this.RawModetabPage))
                        {
                            this.MaintabControl.TabPages.Remove(this.RawModetabPage);
                        }
                        if(this.MaintabControl.Contains(this.TerminalDatatabPage))
                        {
                            this.MaintabControl.TabPages.Remove(this.TerminalDatatabPage);
                        }
                        if(this.MaintabControl.Contains(this.JsontabPage))
                        {
                            this.MaintabControl.TabPages.Remove(this.JsontabPage);
                        }
                    }));
                }
            }

            if(!tc_show_json_tab)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ApplicationtabPage.Enabled = true;
                    this.MaintabControl.SelectedTab = this.ApplicationtabPage;
                    this.ApplicationpictureBoxWait.Enabled = true;
                    this.ApplicationpictureBoxWait.Visible = true;
                }));
            }

            // Wait for a new device to connect
            new Thread(() =>
            {
                bool foundit = false;
                Thread.CurrentThread.IsBackground = true;

                Debug.Write("Waiting for new device to connect");

                string description = "";

                // Wait for a device to attach
                while (!formClosing && !foundit)
                {
                    HidDevice device = HidDevices.Enumerate(Device_IDTech.IDTechVendorID).FirstOrDefault();
                    if (device != null)
                    {
                        foundit = true;
                        description = device.Attributes.ProductHexId;
                        device.CloseDevice();
                    }
                    else
                    {
                        Debug.Write(".");
                        Thread.Sleep(1000);
                    }
                }

                // Initialize Device
                if (!formClosing && foundit)
                {
                    Debug.WriteLine("found one with ID={0}", (object) description);

                    Thread.Sleep(3000);

                    // Initialize Device
                    InitalizeDeviceUI(this, new DeviceNotificationEventArgs());
                }

            }).Start();
        }

        private void SetTagData(string tags, bool isHIDMode)
        {
            Debug.WriteLine("main: card data=[{0}]", (object) tags);
            string [] parsed = devicePlugin.ParseCardData(tags);
            if(parsed.Length > 0)
            {
                if(ApplicationlistView1.Items.Count > 0)
                {
                    ApplicationlistView1.Items.Clear();
                }
                StringBuilder sb = new StringBuilder();
                string cardnumber = "";
                foreach(string val in parsed)
                {
                    string [] tlv = val.Split(':');
                    ListViewItem item1 = new ListViewItem(tlv[0], 0);
                    item1.SubItems.Add(tlv[1]);
                    ApplicationlistView1.Items.Add(item1);

                    if(isHIDMode)
                    {
                        if(tlv[0].Equals("5A"))
                        {
                            cardnumber = tlv[1];
                            ApplicationtxtCardData.Text += "\r\nCARD NUMBER: " + cardnumber;
                        }
                    }
                    else
                    {
                        if(tlv[0].Equals("DFEF5B"))
                        {
                            cardnumber = tlv[1];
                            ApplicationtxtCardData.Text += "\r\nCARD NUMBER: " + cardnumber;
                        }
                    }
                    sb.Append(tlv[0]);
                    sb.Append(tlv[1]);
                }

                Logger.debug( "TAG DATA=[{0}]", sb);
                ApplicationlistView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                ApplicationlistView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                this.ApplicationbtnShowTags.Enabled = true;
                this.ApplicationbtnShowTags.Visible = true;
            }
        }

        private void ProcessCardData(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    object [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    // update the view only once
                    bool.TryParse((string) data[4], out bool updateView);
                    if(updateView)
                    {
                        this.ApplicationtxtCardData.Text = (string) data[0];

                        //delay enabling button
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            Thread.Sleep(tc_transaction_start_delay);
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.ApplicationbtnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;
                            }));
                        }).Start();

                        this.ApplicationtxtCardData.ForeColor = TEXTBOX_FORE_COLOR;

                        // Set TAGS to display
                        if (data[0].ToString().Contains("EMV"))
                        {
                            if(bool.TryParse((string) data[3], out bool isHIDMode))
                            {
                                SetTagData((string) data[1], isHIDMode);
                            }
                            else
                            {
                                SetTagData((string) data[1], isHIDMode);
                            }
                        }
                        else
                        {
                            this.ApplicationtxtCardData.Text += "\r\n";
                            this.ApplicationtxtCardData.Text += (string)data[1];
                        }

                        // Enable Tab(s)
                        this.ApplicationtabPage.Enabled = true;
                        this.ConfigurationtabPage.Enabled = true;
                        this.SettingstabPage.Enabled = tc_show_settings_tab;
                        this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                        this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                    }

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        devicePlugin.CardReadNextState(data[2]);
                    }).Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: ProcessCardData() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }
    
        private void ProcessCardDataError(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                //string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                this.ApplicationtxtCardData.Text = payload.ToString();
                this.ApplicationbtnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                // Enable Tab(s)
                this.ApplicationtabPage.Enabled = true;
                this.ConfigurationtabPage.Enabled = true;
                this.SettingstabPage.Enabled = tc_show_settings_tab;
                this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                TransactionTimer?.Stop();
                TransactionTimer?.Dispose();
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void GetDeviceControlConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    //TODO
                    if(data.Length == 5)
                    {
                    }

                    // Enable Tabs
                    this.ApplicationtabPage.Enabled = true;
                    this.ConfigurationtabPage.Enabled = true;
                    this.SettingstabPage.Enabled = tc_show_settings_tab;
                    this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                    this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                    this.JsontabPage.Enabled = tc_show_json_tab;
                    this.AdvancedtabPage.Enabled = tc_show_advanced_tab;
                    this.SettingsMsrpicBoxWait.Enabled = false;
                    this.SettingsMsrpicBoxWait.Visible = false;
                    this.JsonpicBoxWait.Visible = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: GetDeviceControlConfiguration() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetDeviceControlConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    // update settings in panel
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    // Beep Control
                    try
                    { 
                        this.SettingsBeepControlradioButton1.Checked = bool.Parse(data[0]);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("main: SetDeviceControlConfiguration() : Beep Control - exception={0}", (object)ex.Message);
                        this.SettingsBeepControlErrorlabel1.Visible = true;
                        this.SettingsBeepControlErrorlabel1.Text = data[0];
                    }

                    // LED Control
                    string [] ledControl = data[1]?.Split(',') ?? null;
                    if(ledControl != null)
                    { 
                        if(ledControl.Length == 2)
                        { 
                            try
                            { 
                                this.SettingsLEDControlcheckBox1.Checked = bool.Parse(ledControl[0]);
                                this.SettingsLEDControlcheckBox2.Checked = bool.Parse(ledControl[1]);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("main: SetDeviceControlConfiguration() : LED Control - exception={0}", (object)ex.Message);
                                this.SettingsLEDControlErrorlabel1.Visible = true;
                                this.SettingsLEDControlErrorlabel1.Text = data[1];
                            }
                        }
                        else
                        {
                            this.SettingsLEDControlErrorlabel1.Text = data[1];
                            this.SettingsLEDControlErrorlabel1.Visible = true;
                        }
                    }

                    // Encryption Control
                    string [] encryptionControl = data[2]?.Split(',') ?? null;
                    if(encryptionControl != null)
                    { 
                        if(encryptionControl.Length == 2)
                        {
                            try 
                            { 
                                this.SettingsENCControlcheckBox1.Checked = bool.Parse(encryptionControl[0]);
                                this.SettingsENCControlcheckBox2.Checked = bool.Parse(encryptionControl[1]);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("main: SetDeviceControlConfiguration() : Encryption Control - exception={0}", (object)ex.Message);
                                this.SettingsEncryptionControlErrorlabel1.Text = data[2];
                                this.SettingsEncryptionControlErrorlabel1.Visible = true;
                            }
                        }
                        else
                        {
                            this.SettingsEncryptionControlErrorlabel1.Text = data[2];
                            this.SettingsEncryptionControlErrorlabel1.Visible = true;
                        }
                    }

                    // Display Error Message
                    if(this.SettingsBeepControlErrorlabel1.Visible || this.SettingsLEDControlErrorlabel1.Visible || this.SettingsEncryptionControlErrorlabel1.Visible)
                    {
                        formElements = new List<object>();

                        new Thread(() => 
                        {
                            Thread.CurrentThread.IsBackground = true;
                            this.Invoke((MethodInvoker)delegate()
                            {
                                if(this.SettingsBeepControlErrorlabel1.Visible)
                                {
                                    formElements.Add(this.SettingsBeepControlErrorlabel1);
                                    SoftBlink(this.SettingsBeepControlErrorlabel1, Color.FromArgb(30, 30, 30), Color.Red, 1000, true);
                                }
                                if(this.SettingsLEDControlErrorlabel1.Visible)
                                {
                                    formElements.Add(this.SettingsLEDControlErrorlabel1);
                                    SoftBlink(this.SettingsLEDControlErrorlabel1, Color.FromArgb(30, 30, 30), Color.Red, 1000, true);
                                }
                                if(this.SettingsEncryptionControlErrorlabel1.Visible)
                                {
                                    formElements.Add(this.SettingsEncryptionControlErrorlabel1);
                                    SoftBlink(this.SettingsEncryptionControlErrorlabel1, Color.FromArgb(30, 30, 30), Color.Red, 1000, true);
                                }
                            });

                            // Fade Items Away
                            if(formElements.Count > 0)
                            {
                                FadeTimer = new  System.Timers.Timer();
                                FadeTimer.Elapsed+=new ElapsedEventHandler(OnFadeTimerEvent);
                                FadeTimer.Interval = 2500;
                                FadeTimer.Enabled = true;
                            }

                        }).Start();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: SetDeviceControlConfiguration() - exception={0}", (object)ex.Message);
                }
                finally
                {
                    // Enable Tabs
                    this.ApplicationtabPage.Enabled = true;
                    this.ConfigurationtabPage.Enabled = true;
                    this.SettingstabPage.Enabled = tc_show_settings_tab;
                    this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                    this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                    this.JsontabPage.Enabled = tc_show_json_tab;
                    this.AdvancedtabPage.Enabled = tc_show_advanced_tab;

                    this.SettingsMSRgroupBox1.Enabled = true;
                    this.SettingsControlpictureBox1.Visible  = false;
                    this.SettingsControlpictureBox1.Enabled = false;

                    if(!this.SettingsBeepControlErrorlabel1.Visible && !this.SettingsLEDControlErrorlabel1.Visible && !this.SettingsEncryptionControlErrorlabel1.Visible)
                    {
                        this.SettingsControlConfigureBtn.Enabled = true;
                    }

                    // Load MSR Settings Next
                    if(loadMSRSettings)
                    {
                        loadMSRSettings = false;
                        this.SettingsControlgroupBox1.Enabled = false;
                        new Thread(() => 
                        {
                            this.Invoke((MethodInvoker)delegate()
                            {
                                LoadMSRSettings();
                            });
                        }).Start();
                    }
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void GetDeviceMsrConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    //TODO
                    if(data.Length == 5)
                    {
                        //this.lblExpMask.Text    = data[0];
                        //this.lblPanDigits.Text  = data[1];
                        //this.lblSwipeForce.Text = data[2];
                        //this.lblSwipeMask.Text  = data[3];
                        //this.lblMsrSetting.Text = data[4];
                    }

                    // Enable Tabs
                    this.ApplicationtabPage.Enabled = true;
                    this.ConfigurationtabPage.Enabled = true;
                    this.SettingstabPage.Enabled = tc_show_settings_tab;
                    this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                    this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                    this.JsontabPage.Enabled = tc_show_json_tab;
                    this.AdvancedtabPage.Enabled = tc_show_advanced_tab;
                    this.SettingsMsrpicBoxWait.Enabled = false;
                    this.SettingsMsrpicBoxWait.Visible = false;
                    this.JsonpicBoxWait.Visible = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: GetDeviceMsrConfiguration() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetDeviceMsrConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    // update settings in panel
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    // Expiration Mask
                    this.SettingsMsrcBxExpirationMask.Checked = data[0].Equals("Masked", StringComparison.CurrentCultureIgnoreCase) ? true : false;

                    // PAN Clear Digits
                    this.SettingsMsrtxtPAN.Text = data[1].All(c => c >= '0' && c <= '9') ? data[1] : this.SettingsMsrtxtPAN.Text;

                    // Swipe Force Mask
                    string [] values = data[2].Split(',');

                    // Process Individual values
                    if(values.Length == 4)
                    {
                        string [] track1 = values[0].Split(':');
                        string [] track2 = values[1].Split(':');
                        string [] track3 = values[2].Split(':');
                        string [] track3Card0 = values[3].Split(':');
                        string t1Value = track1[1].Trim();
                        string t2Value = track2[1].Trim();
                        string t3Value = track3[1].Trim();
                        string t3Card0Value = track3Card0[1].Trim();
                        bool t1Val = t1Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;
                        bool t2Val = t2Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;
                        bool t3Val = t3Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;
                        bool t3Card0Val = t3Card0Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.SettingsMsrcBxTrack1.Checked != t1Val) {
                            this.SettingsMsrcBxTrack1.Checked = t1Val;
                        }

                        if(this.SettingsMsrcBxTrack2.Checked != t2Val) {
                            this.SettingsMsrcBxTrack2.Checked = t2Val;
                        }

                        if(this.SettingsMsrcBxTrack3.Checked != t3Val) {
                            this.SettingsMsrcBxTrack3.Checked = t3Val;
                        }

                        if(this.SettingsMsrcBxTrack3Card0.Checked != t3Card0Val) {
                            this.SettingsMsrcBxTrack3Card0.Checked = t3Card0Val;
                        }

                        // Swipe Mask
                        values = data[3].Split(',');

                        // Process Individual values
                        track1 = values[0].Split(':');
                        track2 = values[1].Split(':');
                        track3 = values[2].Split(':');

                        t1Value = track1[1].Trim();
                        t2Value = track2[1].Trim();
                        t3Value = track3[1].Trim();

                        t1Val = t1Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;
                        t2Val = t2Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;
                        t3Val = t3Value.Equals("ON", StringComparison.CurrentCultureIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.SettingsMsrcBxSwipeMaskTrack1.Checked != t1Val) 
                        {
                            this.SettingsMsrcBxSwipeMaskTrack1.Checked = t1Val;
                        }

                        if(this.SettingsMsrcBxSwipeMaskTrack2.Checked != t2Val) 
                        {
                            this.SettingsMsrcBxSwipeMaskTrack2.Checked = t2Val;
                        }

                        if(this.SettingsMsrcBxSwipeMaskTrack3.Checked != t3Val) 
                        {
                            this.SettingsMsrcBxSwipeMaskTrack3.Checked = t3Val;
                        }
                    }

                    // Enable Tabs
                    this.ApplicationtabPage.Enabled = true;
                    this.ConfigurationtabPage.Enabled = true;
                    this.SettingstabPage.Enabled = tc_show_settings_tab;
                    this.RawModetabPage.Enabled = tc_show_raw_mode_tab;
                    this.TerminalDatatabPage.Enabled = tc_show_terminal_data_tab;
                    this.JsontabPage.Enabled = tc_show_json_tab;
                    this.AdvancedtabPage.Enabled = tc_show_advanced_tab;

                    this.SettingsControlgroupBox1.Enabled = true;
                    this.SettingsMsrpicBoxWait.Visible  = false;
                    this.SettingsMsrpicBoxWait.Enabled = false;
                    this.JsonpicBoxWait.Visible  = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: SetDeviceMsrConfiguration() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetDeviceInterfaceType(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.ApplicationbtnMode.Text = data[0];
                    this.ApplicationbtnMode.Visible = true;
                    this.ApplicationbtnMode.Enabled = true;

                    this.ConfigurationPanel1btnDeviceMode.Enabled = true;
                    this.ConfigurationPanel1btnDeviceMode.Text = data[0];
                    this.ConfigurationPanel1btnEMVMode.Enabled = (this.ConfigurationPanel1btnDeviceMode.Text.Equals(USK_DEVICE_MODE.USB_HID)) ? false : true;

                    isNonAugusta = false;
                    if (data[0].Contains("OLDIDTECH"))
                    {
                        string [] split = data[0].Split(':');
                        isNonAugusta = true;
                        //this.btnMode.Text = "Set to " + (split[1].Contains("HID") ? "HID" : "KB");
                        this.ApplicationbtnMode.Text = split[1];

                        // Startup Transition to HID mode
                        if (this.JsonpicBoxWait.Visible == true)
                        {
                            this.JsonpicBoxWait.Visible = false;
                            MaintabControl.SelectedTab = this.ApplicationtabPage;
                        }

                        // Only have btnCardRead for HID devices.  KB devices always read cards.
                        this.ApplicationbtnCardRead.Enabled = (split[1].Contains("HID")); 
                        dev_usb_mode = (data[0].Contains("HID")) ? DEV_USB_MODE.USB_KYB_MODE : DEV_USB_MODE.USB_HID_MODE;

                        if (MaintabControl.Contains(RawModetabPage))
                        {
                            MaintabControl.TabPages.Remove(RawModetabPage);
                        }
                        if (MaintabControl.Contains(TerminalDatatabPage))
                        {
                            MaintabControl.TabPages.Remove(TerminalDatatabPage);
                        }
                        if (MaintabControl.Contains(JsontabPage))
                        {
                            MaintabControl.TabPages.Remove(JsontabPage);
                        }
                        MaintabControl.SelectedTab = this.ApplicationtabPage;
                    }
                    else if (data[0].Contains("HID") || data[0].Contains("UNKNOWN"))
                    {
                        if(data[0].Contains("UNKNOWN"))
                        {
                            this.ApplicationbtnMode.Visible = false;
                        }
                        else
                        {
                            dev_usb_mode = DEV_USB_MODE.USB_KYB_MODE;
                        }

                        // Startup Transition to HID mode
                        if(this.JsonpicBoxWait.Visible == true)
                        {
                            this.JsonpicBoxWait.Visible = false;
                            MaintabControl.SelectedTab = this.ApplicationtabPage;
                        }

                        this.ApplicationbtnCardRead.Enabled = false;

                        if(MaintabControl.Contains(RawModetabPage))
                        {
                            MaintabControl.TabPages.Remove(RawModetabPage);
                        }
                        if(MaintabControl.Contains(TerminalDatatabPage))
                        {
                            MaintabControl.TabPages.Remove(TerminalDatatabPage);
                        }
                        if(MaintabControl.Contains(JsontabPage))
                        {
                            MaintabControl.TabPages.Remove(JsontabPage);
                        }
                        if(MaintabControl.Contains(ConfigurationtabPage))
                        {
                            MaintabControl.TabPages.Remove(ConfigurationtabPage);
                        }
                        if(MaintabControl.Contains(SettingstabPage))
                        {
                            MaintabControl.TabPages.Remove(SettingstabPage);
                        }
                        if(MaintabControl.Contains(AdvancedtabPage))
                        {
                            MaintabControl.TabPages.Remove(AdvancedtabPage);
                        }
                        MaintabControl.SelectedTab = this.ApplicationtabPage;
                    }
                    else
                    {
                        dev_usb_mode = DEV_USB_MODE.USB_HID_MODE;

                        this.ApplicationbtnCardRead.Enabled = true;

                        if(!MaintabControl.Contains(ConfigurationtabPage))
                        {
                            MaintabControl.TabPages.Add(ConfigurationtabPage);
                        }
                        this.ConfigurationIDgrpBox.Visible = false;
                        this.ConfigurationCollapseButton.Visible = false;
                        this.ConfigurationPanel2.Visible = false;
                        ConfigurationResetLoadFromButtons();
                        if(!MaintabControl.Contains(SettingstabPage) && tc_show_settings_tab)
                        {
                            MaintabControl.TabPages.Add(SettingstabPage);
                        }
                        if(!MaintabControl.Contains(RawModetabPage) && tc_show_raw_mode_tab)
                        {
                            MaintabControl.TabPages.Add(RawModetabPage);
                        }
                        if(!MaintabControl.Contains(TerminalDatatabPage) && tc_show_terminal_data_tab)
                        {
                            MaintabControl.TabPages.Add(TerminalDatatabPage);
                        }
                        if(!MaintabControl.Contains(AdvancedtabPage) && tc_show_advanced_tab)
                        {
                            MaintabControl.TabPages.Add(AdvancedtabPage);
                        }
                        if(!MaintabControl.Contains(JsontabPage) && tc_show_json_tab)
                        {
                            MaintabControl.TabPages.Add(JsontabPage);
                            MaintabControl.SelectedTab = this.JsontabPage;
                            this.JsontabPage.Enabled = true;
                            this.JsonpicBoxWait.Visible = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: SetDeviceInterfaceType() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetExecuteResult(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.RawModetxtCommandResult.Text = "RESPONSE: [" + data[0] + "]";
                    this.RawModebtnExecute.Enabled = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: SetExecuteResult() - exception={0}", (object)ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowTerminalData(object payload)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                // Invoker with Parameter(s)
                MethodInvoker mi = () =>
                {
                    this.ConfigurationIDgrpBox.Visible = false;

                    try
                    {
                        string [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray() ?? null;

                        // Remove previous entries
                        if(ConfigurationTerminalDatalistView.Items.Count > 0)
                        {
                            ConfigurationTerminalDatalistView.Items.Clear();
                        }

                        if(data != null && data.Length > 0)
                        { 
                            // Check for ERRORS
                            if(!data[0].Equals("NO FIRMWARE VERSION MATCH"))
                            {
                                foreach(string val in data)
                                {
                                    string [] tlv = val.Split(':');
                                    ListViewItem item1 = new ListViewItem(tlv[0], 0);
                                    item1.SubItems.Add(tlv[1]);
                                    ConfigurationTerminalDatalistView.Items.Add(item1);
                                }

                                // TAB 0
                                if(!tabControlConfiguration.Contains(ConfigurationTerminalDatatabPage))
                                {
                                    tabControlConfiguration.TabPages.Add(ConfigurationTerminalDatatabPage);
                                }
                                this.ConfigurationTerminalDatatabPage.Enabled = true;
                                tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                                // TAB 1
                                if(!tabControlConfiguration.Contains(ConfigurationAIDStabPage))
                                {
                                    tabControlConfiguration.TabPages.Add(ConfigurationAIDStabPage);
                                }
                                this.ConfigurationAIDStabPage.Enabled = true;
                                // TAB 2
                                if(!tabControlConfiguration.Contains(ConfigurationCAPKStabPage))
                                {
                                    tabControlConfiguration.TabPages.Add(ConfigurationCAPKStabPage);
                                }
                                this.ConfigurationCAPKStabPage.Enabled = true;
                                // TAB 3
                                if(this.ApplicationlblModelName.Text.Contains("VP5300"))
                                { 
                                    if(!tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                                    {
                                        tabControlConfiguration.TabPages.Add(ConfigurationGROUPStabPage);
                                    }
                                    this.ConfigurationGROUPStabPage.Enabled = true;
                                }
                                else
                                {
                                    if(tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                                    {
                                        tabControlConfiguration.TabPages.Remove(ConfigurationGROUPStabPage);
                                    }
                                    this.ConfigurationGROUPStabPage.Enabled = false;
                                }

                                // Set ConfigurationID
                                if(this.radioLoadFromFile.Checked)
                                {
                                    if(configloadDuration.ToString().Equals("00:00:00"))
                                    { 
                                        configloadDuration = DateTime.Now - configloadStart;
                                        string configloadDurationlbl = String.Format("{0}:{1:D2}:{2:D2}", configloadDuration.Hours, configloadDuration.Minutes, configloadDuration.Seconds);
                                        ToolTip tpDuration = new ToolTip()
                                        { 
                                            IsBalloon = true,
                                            ToolTipTitle = "Configuration Load Time",
                                        };
                                        tpDuration.SetToolTip(this.ConfigurationVersionlbl, configloadDurationlbl);
                                    }

                                    ConfigurationID configurationID = devicePlugin?.GetConfigSphereSerializer().GetConfigurationID();

                                    // Update Configuration File
                                    if(configurationID != null)
                                    {
                                        this.ConfigurationPlatform.Text = configurationID.Platform;
                                        this.ConfigurationEnvironment.Text = configurationID.CardEnvironment;
                                        this.ConfigurationVersion.Text = configurationID.Version;
                                        ToolTip tp = new ToolTip()
                                        { 
                                            IsBalloon = true,
                                            ToolTipTitle = "Configuration Version",
                                        };
                                        tp.SetToolTip(this.ConfigurationVersion, configurationID.Version);
                                        this.ConfigurationIDgrpBox.Visible = true;
                                    }

                                    this.ConfigurationModel.Text = this.ApplicationlblModelNumber.Text;
                                    this.ConfigurationFirmwareVersion.Text = this.ApplicationAdvancedFirmwarelblVersion.Text;
                                }
                            }
                            else
                            {
                                ListViewItem item1 = new ListViewItem("ERROR", 0);
                                item1.SubItems.Add(data[0]);
                                ConfigurationTerminalDatalistView.Items.Add(item1);
                            }
                        }
                        else
                        {
                            ListViewItem item1 = new ListViewItem("ERROR", 0);
                            item1.SubItems.Add("*** NO DATA ***");
                            ConfigurationTerminalDatalistView.Items.Add(item1);
                        }

                        ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                        ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    }
                    catch (Exception ex)
                    {
                        Logger.error("main: ShowTerminalData() - exception={0}", (object) ex.Message);
                    }
                    finally
                    {
                        StopConfigLoaderTimer();
                        this.ConfigurationTerminalDatapicBoxWait.Enabled = false;
                        this.ConfigurationTerminalDatapicBoxWait.Visible  = false;
                        this.ConfigurationPanel1pictureBox1.Enabled = false;
                        this.ConfigurationPanel1pictureBox1.Visible = false;
                        this.ConfigurationTerminalDataLoading.Visible = false;
                        this.ConfigurationPanel1.Enabled = true;
                    }
                };

                if (InvokeRequired)
                {
                    BeginInvoke(mi);
                }
                else
                {
                    Invoke(mi);
                }
            }
        }

        private void ShowTerminalDataError(object payload)
        {
            MethodInvoker mi = () =>
            {
                this.ConfigurationIDgrpBox.Visible = false;

                try
                {
                    string [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray() ?? null;

                    if(data != null)
                    {
                        StopConfigLoaderTimer();
                        
                        this.ConfigurationTerminalDatapicBoxWait.Enabled = false;
                        this.ConfigurationTerminalDatapicBoxWait.Visible  = false;
                        this.ConfigurationPanel1pictureBox1.Enabled = false;
                        this.ConfigurationPanel1pictureBox1.Visible = false;
                        this.ConfigurationPanel1.Visible = false;
                        this.ConfigurationPanel2.Visible = false;

                        this.ConfigurationErrorgroupBox1.Visible = true;
                        this.ConfigurationlblError2.Text = data[0];

                        new Thread(() => 
                        {
                            SoftBlink(this.ConfigurationlblWarning1, Color.FromArgb(30, 30, 30), Color.Red, 5000, true);
                            SoftBlink(this.ConfigurationlblError1, Color.FromArgb(30, 30, 30), Color.Green, 5000, true);
                            SoftBlink(this.ConfigurationlblError2, Color.FromArgb(30, 30, 30), Color.Green, 5000, true);
                            SoftBlink(this.ConfigurationlblError3, Color.FromArgb(30, 30, 30), Color.Green, 5000, true);
                            Thread.Sleep(15000);
                            // Exit the App
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }).Start();
                    }

                }
                catch (Exception ex)
                {
                    Logger.error("main: ShowTerminalDataError() - exception={0}", (object) ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowTerminalDataInfo(object payload)
        {
            MethodInvoker mi = () =>
            {
                this.ConfigurationIDgrpBox.Visible = false;

                try
                {
                    string [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray() ?? null;

                    if(data != null)
                    {
                        StopConfigLoaderTimer();
                        
                        this.ConfigurationTerminalDatapicBoxWait.Enabled = false;
                        this.ConfigurationTerminalDatapicBoxWait.Visible  = false;
                        this.ConfigurationPanel1pictureBox1.Enabled = false;
                        this.ConfigurationPanel1pictureBox1.Visible = false;
                        this.ConfigurationTerminalDataLoading.Visible = false;
                        this.ConfigurationPanel1.Visible = false;
                        this.ConfigurationPanel2.Visible = false;

                        this.ConfigurationInfogroupBox1.Visible = true;
                        this.ConfigurationlblInfo3.Text = data[0];

                        new Thread(() => 
                        {
                            SoftBlink(this.ConfigurationlblWarning1, Color.FromArgb(30, 30, 30), Color.Red, 5000, true);
                            SoftBlink(this.ConfigurationlblError1, Color.FromArgb(30, 0, 30), Color.Green, 5000, true);
                            SoftBlink(this.ConfigurationlblError2, Color.FromArgb(30, 0, 30), Color.Green, 5000, true);
                            SoftBlink(this.ConfigurationlblError3, Color.FromArgb(30, 0, 30), Color.Green, 5000, true);
                            Thread.Sleep(5000);

                            this.Invoke((MethodInvoker)delegate()
                            {
                                this.ConfigurationInfogroupBox1.Visible = false;
                                this.ConfigurationPanel1.Visible = true;
                                this.ConfigurationPanel2.Visible = true;

                                if(data[1]?.Equals("DEVICE", StringComparison.CurrentCultureIgnoreCase) ?? false)
                                { 
                                    this.radioLoadFromDevice.Checked = true;
                                }
                            });

                        }).Start();
                    }

                }
                catch (Exception ex)
                {
                    Logger.error("main: ShowTerminalDataError() - exception={0}", (object) ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowAidList(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray() ?? null;

                    // Remove previous entries
                    if(ConfigurationAIDSlistView.Items.Count > 0)
                    {
                        ConfigurationAIDSlistView.Items.Clear();
                    }

                    if(data != null && data.Length > 0)
                    { 
                        foreach(string item in data)
                        {
                            string [] components = item.Split('#');
                            if(components.Length == 2)
                            {
                                ListViewItem item1 = new ListViewItem(components[0], 0);
                                item1.SubItems.Add(components[1]);
                                ConfigurationAIDSlistView.Items.Add(item1);
                            }
                        }
                    }
                    else
                    {
                        ListViewItem item1 = new ListViewItem("ERROR", 0);
                        item1.SubItems.Add("*** NO DATA ***");
                        ConfigurationAIDSlistView.Items.Add(item1);
                    }

                    ConfigurationAIDSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    ConfigurationAIDSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                    if(!tabControlConfiguration.Contains(ConfigurationAIDStabPage))
                    {
                        tabControlConfiguration.TabPages.Add(ConfigurationAIDStabPage);
                    }
                    this.ConfigurationAIDStabPage.Enabled = true;
                    tabControlConfiguration.SelectedTab = this.ConfigurationAIDStabPage;
                }
                catch (Exception ex)
                {
                    Logger.error("main: ShowAIDData() - exception={0}", (object) ex.Message);
                }
                finally
                {
                    this.ConfigurationAIDSpicBoxWait.Enabled = false;
                    this.ConfigurationAIDSpicBoxWait.Visible  = false;
                    this.ConfigurationPanel1pictureBox1.Enabled = false;
                    this.ConfigurationPanel1pictureBox1.Visible = false;
                    this.ConfigurationAIDLoading.Visible = false;
                    this.ConfigurationPanel1.Enabled = true;
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowCapKList(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray() ?? null;

                    // Remove previous entries
                    if(ConfigurationCAPKSlistView.Items.Count > 0)
                    {
                        ConfigurationCAPKSlistView.Items.Clear();
                    }

                    if(data != null && data.Length > 0)
                    { 
                        foreach(string item in data)
                        {
                            string [] components = item.Split('#');
                            if(components.Length == 2)
                            {
                                ListViewItem item1 = new ListViewItem(components[0], 0);
                                string [] keyvalue = components[1].Split(' ');
                                if(keyvalue.Length > 2)
                                {
                                    // RID
                                    //string [] ridvalue = keyvalue[0].Split(':');
                                    //if(ridvalue.Length == 2)
                                    //{
                                    //    item1.SubItems.Add(ridvalue[1]);
                                    //}
                                    // INDEX
                                    //string [] indexvalue = keyvalue[1].Split(':');
                                    //if(indexvalue.Length == 2)
                                    //{
                                    //    item1.SubItems.Add(indexvalue[1]);
                                    //}
                                    // MODULUS
                                    string [] modvalues = keyvalue[2].Split(':');
                                    if(modvalues.Length == 2)
                                    {
                                        item1.SubItems.Add(modvalues[1]);
                                    }
                                    // EXPONENT: FILE ONLY
                                    //string [] expvalues = keyvalue[3].Split(':');
                                    //if(expvalues.Length == 2)
                                    //{
                                    //    item1.SubItems.Add(expvalues[1]);
                                    //}
                                    // CHECKSUM: FILE ONLY
                                    //string [] checksumvalues = keyvalue[4].Split(':');
                                    //if(checksumvalues.Length == 2)
                                    //{
                                    //    item1.SubItems.Add(checksumvalues[1]);
                                    //}
                                    ConfigurationCAPKSlistView.Items.Add(item1);
                                }
                            }
                        }
                    }
                    else
                    {
                        ListViewItem item1 = new ListViewItem("N/A", 0);
                        item1.SubItems.Add("*** NO DATA ****");
                        ConfigurationCAPKSlistView.Items.Add(item1);
                    }

                    ConfigurationCAPKSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    ConfigurationCAPKSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                    if(!tabControlConfiguration.Contains(ConfigurationCAPKStabPage))
                    {
                        tabControlConfiguration.TabPages.Add(ConfigurationCAPKStabPage);
                    }
                    this.ConfigurationCAPKStabPage.Enabled = true;
                    tabControlConfiguration.SelectedTab = this.ConfigurationCAPKStabPage;
                }
                catch (Exception ex)
                {
                    Logger.error("main: ShowCapKList() - exception={0}", (object) ex.Message);
                }
                finally
                {
                    this.ConfigurationCAPKSpicBoxWait.Enabled = false;
                    this.ConfigurationCAPKSpicBoxWait.Visible  = false;
                    this.ConfigurationPanel1pictureBox1.Enabled = false;
                    this.ConfigurationPanel1pictureBox1.Visible = false;
                    this.ConfigurationCAPKLoading.Visible = false;
                    this.ConfigurationPanel1.Enabled = true;
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void UpdateSetupMessage(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                // Configuration Loading
                StopConfigLoaderTimer();

                try
                {
                    object [] data = ((IEnumerable) payload)?.Cast<object>().Select(x => x ?? "").ToArray() ?? null;

                    if(data != null && data.Length > 0)
                    {
                        this.ConfigurationTerminalDataLoading.Text = (string) data[0];
                        if(data.Length > 1)
                        { 
                            Color color = (Color) data[1];
                            new Thread(() => 
                            {
                                Thread.CurrentThread.IsBackground = true;
                                Thread.Sleep(100);
                                SoftBlink(this.ConfigurationTerminalDataLoading, Color.FromArgb(30, 30, 30), color, 2000, true);
                                // Set Configuration Load Timer
                                SetConfigLoaderTimer();
                            }).Start();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.error("main: UpdateSetupMessage() - exception={0}", (object) ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowConfigGroup(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    // Remove previous entries
                    if(ConfigurationGROUPSlistView.Items.Count > 0)
                    {
                        ConfigurationGROUPSlistView.Items.Clear();
                    }

                    if(payload != null)
                    {
                        string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                        foreach(string item in data)
                        {
                            string [] components = item.Split(':');
                            if(components.Length == 3)
                            {
                                ListViewItem item1 = new ListViewItem(components[0], 0);
                                string keytag = components[1];
                                if(keytag.Length > 0)
                                {
                                    // TAG
                                    item1.SubItems.Add(keytag);

                                    // VALUE
                                    string keyvalue = components[2];
                                    if(keyvalue.Length > 0)
                                    {
                                        // TAG
                                        item1.SubItems.Add(keyvalue);
                                        ConfigurationGROUPSlistView.Items.Add(item1);
                                    }
                                }
                            }
                        }

                        if(!tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                        {
                            tabControlConfiguration.TabPages.Add(ConfigurationGROUPStabPage);
                        }
                        this.ConfigurationGROUPStabPage.Enabled = true;
                        tabControlConfiguration.SelectedTab = this.ConfigurationGROUPStabPage;
                    }
                    else
                    {
                        ListViewItem item1 = new ListViewItem("INFO", 0);
                        item1.SubItems.Add("UNDEFINED");
                        item1.SubItems.Add(String.Format("NO DEFINITIONS FOR GROUP {0} IN CONFIG", ConfigurationGROUPStabPagecomboBox1.SelectedItem));
                        ConfigurationGROUPSlistView.Items.Add(item1);
                    }
                    ConfigurationGROUPSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    ConfigurationGROUPSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                }
                catch (Exception ex)
                {
                    Logger.error("main: ShowConfigGroup() - exception={0}", (object) ex.Message);
                }
                finally
                {
                    this.ConfigurationGROUPSpicBoxWait.Enabled = false;
                    this.ConfigurationGROUPSpicBoxWait.Visible  = false;
                    this.ConfigurationPanel1pictureBox1.Enabled = false;
                    this.ConfigurationPanel1pictureBox1.Visible = false;
                    this.ConfigurationPanel1.Enabled = true;
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void EnableButtons()
        {
            MethodInvoker mi = () =>
            {
                this.ConfigurationPanel1btnDeviceMode.Enabled = true;
                this.ConfigurationPanel1btnEMVMode.Enabled = true;
                this.ConfigurationPanel1pictureBox1.Enabled = false;
                this.ConfigurationPanel1pictureBox1.Visible = false;
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void ShowJsonConfig(object payload)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_HID_MODE)
            {
                // Invoker with Parameter(s)
                MethodInvoker mi = () =>
                {
                    try
                    {
                        if(tc_show_json_tab)
                        {
                            string [] filename = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                            this.JsontextBox1.Text = File.ReadAllText(filename[0]);
                            MaintabControl.SelectedTab = this.JsontabPage;
                            this.JsonpicBoxWait.Visible = false;
                        }
                        else
                        {
                            UpdateUI();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("main: ShowJsonConfig() - exception={0}", (object) ex.Message);
                    }
                };

                if (InvokeRequired)
                {
                    BeginInvoke(mi);
                }
                else
                {
                    Invoke(mi);
                }
            }
            else
            {
                if(this.JsonpicBoxWait.Visible == true)
                { 
                    this.JsonpicBoxWait.Visible = false;
                    MaintabControl.SelectedTab = this.ApplicationtabPage;
                }
            }
        }

        private void SetModeButtonEnabled(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                    this.ApplicationbtnMode.Enabled = data[0].Equals("Enable") ? true : false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("main: SetModeButtonEnabled() - exception={0}", (object) ex.Message);
                }
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void SetEmvButton(object payload)
        {
            MethodInvoker mi = () =>
            {
                string[] data = ((IEnumerable)payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                this.ConfigurationPanel1btnDeviceMode.Text = data[0];
                this.ConfigurationPanel1btnDeviceMode.Enabled = true;
                this.ConfigurationPanel1pictureBox1.Enabled = false;
                this.ConfigurationPanel1pictureBox1.Visible = false;
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void FirmwareUpdateProgress(object payload)
        {
            MethodInvoker mi = () =>
            {
                string[] data = ((IEnumerable)payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                this.AdvancedFirmwareprogressBar1.PerformStep();
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void FirmwareUpdateStatus(object payload)
        {
            MethodInvoker mi = () =>
            {
                string[] data = ((IEnumerable)payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                this.AdvancedFirmwarelblVersion.Text = data[0];
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void FirmwareUpdateFailed(object payload)
        {
            MethodInvoker mi = () =>
            {
                string[] data = ((IEnumerable)payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();
                this.AdvancedFirmwarelblVersion.Text = data[0];
                Thread.Sleep(3000);
                this.AdvancedFirmwarebtnUpdate.Visible = true;
                this.AdvancedFirmwarebtnUpdate.Enabled = true;
                this.AdvancedFirmwarepicBoxWait.Enabled = false;
                this.AdvancedFirmwarepicBoxWait.Visible = false;
                this.AdvancedFirmwareprogressBar1.Visible = false;
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        private void EnableMainForm(object payload)
        {
            MethodInvoker mi = () =>
            {
                string[] data = ((IEnumerable)payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                this.AdvancedFirmwarelblVersion.Text = data[0];
                this.AdvancedFirmwarebtnUpdate.Visible = true;
                this.AdvancedFirmwarebtnUpdate.Enabled = false;
                this.AdvancedFirmwarepicBoxWait.Enabled = false;
                this.AdvancedFirmwarepicBoxWait.Visible = false;
                this.AdvancedFirmwareprogressBar1.Visible = false;
            };

            if (InvokeRequired)
            {
                BeginInvoke(mi);
            }
            else
            {
                Invoke(mi);
            }
        }

        #endregion

        /********************************************************************************************************/
        // TIMERS
        /********************************************************************************************************/
        #region -- device timers --
        private void SetConfigLoaderTimer()
        {
            ConfigLoaderTimer = new System.Timers.Timer(tc_configloader_timeout)
            {
                AutoReset = false
            };
            ConfigLoaderTimer.Elapsed += (sender, e) => ConfigLoaderRaiseTimerExpired(new TimerEventArgs { Timer = TimerType.Transaction });
            ConfigLoaderTimer.Start();
        }
        private void StopConfigLoaderTimer()
        {
            ConfigLoaderTimer?.Stop();
        }

        private void SetTransactionTimer()
        {
            TransactionTimer = new System.Timers.Timer(tc_transaction_timeout)
            {
                AutoReset = false
            };
            TransactionTimer.Elapsed += (sender, e) => TransactionRaiseTimerExpired(new TimerEventArgs { Timer = TimerType.Transaction });
            TransactionTimer.Start();
        }

        private void TransactionRaiseTimerExpired(TimerEventArgs e)
        {
            TransactionTimer?.Stop();

            // Check for valid collection and completion of collection
            if(stopWatch.ElapsedMilliseconds > tc_transaction_collection_timeout)
            {
                int minTransactionLen = tc_minimum_transaction_length;
                if (isNonAugusta)
                { 
                    minTransactionLen = 120;
                }
                if(this.ApplicationtxtCardData.Text.Length > minTransactionLen)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string data = ApplicationtxtCardData.Text;
                        if (isNonAugusta)
                        {
                            ApplicationtxtCardData.Text = "*** TRANSACTION DATA CAPTURED : MSR ***";
                        }
                        else
                        {
                            ApplicationtxtCardData.Text = "*** TRANSACTION DATA CAPTURED : EMV ***";
                        }
                        this.ApplicationtxtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                        // Set TAGS to display
                        SetTagData(data, false);
                    }));
                }
                else
                {
                    // This could be an MSR fallback transaction
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string data = ApplicationtxtCardData.Text;
                        if(data.Length > 0)
                        {
                            string message = devicePlugin.GetErrorMessage(data);
                            ApplicationtxtCardData.Text = message;
                            this.ApplicationtxtCardData.ForeColor = TEXTBOX_FORE_COLOR;
                            Debug.WriteLine("main: card data=[{0}]", (object) data);
                        }
                        else
                        {
                            SetTransactionTimer();
                        }
                    }));
                }
            }
            else
            {
                SetTransactionTimer();
            }

            Debug.WriteLine("main: transaction timer raised ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            this.Invoke(new MethodInvoker(() =>
            {
                if(!string.IsNullOrEmpty(this.ApplicationtxtCardData.Text))
                {
                    Debug.WriteLine("main: card data length=[0}", this.ApplicationtxtCardData.Text.Length);
                }
            }));
        }

        private void ConfigLoaderRaiseTimerExpired(TimerEventArgs e)
        {
            ConfigLoaderTimer?.Stop();

            this.Invoke(new MethodInvoker(() =>
            {
                ShowTerminalData(null);
            }));
        }

        #endregion

        /**************************************************************************/
        // CONFIGURATION TAB
        /**************************************************************************/
        #region -- configuration tab --

        private void OnCollapseConfigurationView(object sender, EventArgs e)
        {
            this.ConfigurationPanel1.Visible = false;
            this.ConfigurationExpandButton.Visible = true;
            this.tabControlConfiguration.Width -= CONFIG_PANEL_WIDTH;
            this.tabControlConfiguration.Width *= 2;

            if(this.ConfigurationTerminalDatatabPage.Visible)
            { 
                ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            else if(this.ConfigurationAIDSlistView.Visible)
            {
                ConfigurationAIDSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                ConfigurationAIDSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            else if(this.ConfigurationCAPKSlistView.Visible)
            {
                ConfigurationCAPKSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                ConfigurationCAPKSlistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private void OnExpandConfigurationView(object sender, EventArgs e)
        {
            this.ConfigurationPanel1.Visible = true;
            this.ConfigurationExpandButton.Visible = false;
            this.tabControlConfiguration.Width /= 2;
            this.tabControlConfiguration.Width += CONFIG_PANEL_WIDTH;

            if(this.ConfigurationTerminalDatatabPage.Visible)
            { 
                ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                ConfigurationTerminalDatalistView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private void OnConfigurationPanel2VisibilityChanged(object sender, EventArgs e)
        {
            if(((System.Windows.Forms.Panel)sender).Visible)
            {
                if(this.ApplicationlblModelName.Text.Contains("VP5300"))
                {
                    if(!tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                    {
                        tabControlConfiguration.TabPages.Add(ConfigurationGROUPStabPage);
                    }
                    this.ConfigurationGROUPStabPage.Enabled = true;
                }
                else if(tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                {
                    tabControlConfiguration.TabPages.Remove(ConfigurationGROUPStabPage);
                    this.ConfigurationGROUPStabPage.Enabled = false;
                }
            }
        }

        private void OnConfigurationListItemSelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlConfiguration.SelectedTab?.Name.Equals("ConfigurationTerminalDatatabPage") ?? false)
            {
                // Configuration Mode
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ConfigurationTerminalDatapicBoxWait.Visible = true;
                    this.ConfigurationTerminalDatapicBoxWait.Enabled = true;
                    this.ConfigurationTerminalDataLoading.Visible = true;
                    System.Windows.Forms.Application.DoEvents();

                    // load starting time
                    configloadStart = DateTime.Now;

                    if(!devicePlugin.ConfigFileLoaded() && this.radioLoadFromFile.Checked)
                    {
                        this.ConfigurationTerminalDataLoading.Text = "SETTING UP TERMINAL DATA...";
                        this.ConfigurationTerminalDataLoading.ForeColor = Color.Black;

                        // Add Pages sequentially after load is complete
                        if(tabControlConfiguration.Contains(ConfigurationAIDStabPage))
                        {
                            tabControlConfiguration.TabPages.Remove(ConfigurationAIDStabPage);
                        }
                        if(tabControlConfiguration.Contains(ConfigurationCAPKStabPage))
                        {
                            tabControlConfiguration.TabPages.Remove(ConfigurationCAPKStabPage);
                        }
                        if(tabControlConfiguration.Contains(ConfigurationGROUPStabPage))
                        {
                            tabControlConfiguration.TabPages.Remove(ConfigurationGROUPStabPage);
                        }
                    }
                    else
                    {
                        this.ConfigurationTerminalDataLoading.Text = "RETRIEVING TERMINAL DATA...";
                        this.ConfigurationTerminalDataLoading.ForeColor = Color.White;
                    }

                    new Thread(() => 
                    {
                        Thread.CurrentThread.IsBackground = true;
                        try 
                        {
                            this.Invoke((MethodInvoker)delegate()
                            {
                                SoftBlink(this.ConfigurationTerminalDataLoading, Color.FromArgb(30, 30, 30), Color.Red, 2000, true);
                                System.Windows.Forms.Application.DoEvents();
                            });

                            int majorcfgint = devicePlugin?.GetConfigSphereSerializer()?.GetTerminalMajorConfiguration() ?? 2;
                            devicePlugin. GetSphereTerminalData(majorcfgint); 
                        }
                        catch(Exception)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.ConfigurationPanel1.Enabled = true;
                                this.ConfigurationTerminalDatapicBoxWait.Visible = false;
                                this.ConfigurationTerminalDatapicBoxWait.Enabled = false;
                                System.Windows.Forms.Application.DoEvents();
                            }));
                        }
                    }).Start();
                }));

                ConfigurationGROUPStabPagecomboBox1.SelectedIndex = -1;
            }
            else if (tabControlConfiguration.SelectedTab?.Name.Equals("ConfigurationAIDStabPage") ?? false)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ConfigurationPanel1.Enabled = false;
                    this.ConfigurationAIDSpicBoxWait.Visible = true;
                    this.ConfigurationAIDSpicBoxWait.Enabled = true;
                    this.ConfigurationAIDLoading.Visible = true;
                    this.ConfigurationAIDLoading.ForeColor = Color.White;
                    SoftBlink(this.ConfigurationAIDLoading, Color.FromArgb(30, 30, 30), Color.Blue, 2000, true);
                    System.Windows.Forms.Application.DoEvents();

                    new Thread(() => 
                    { 
                        try
                        {
                            Thread.CurrentThread.IsBackground = true;
                            devicePlugin.GetAIDList(); 
                        }
                        catch(Exception)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.ConfigurationPanel1.Enabled = true;
                                this.ConfigurationAIDSpicBoxWait.Visible = false;
                                this.ConfigurationAIDSpicBoxWait.Enabled = false;
                                System.Windows.Forms.Application.DoEvents();
                            }));
                        }
                    }).Start();
                }));

                ConfigurationGROUPStabPagecomboBox1.SelectedIndex = -1;
            }
            else if (tabControlConfiguration.SelectedTab?.Name.Equals("ConfigurationCAPKStabPage") ?? false)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ConfigurationPanel1.Enabled = false;
                    this.ConfigurationCAPKSpicBoxWait.Visible = true;
                    this.ConfigurationCAPKSpicBoxWait.Enabled = true;
                    this.ConfigurationCAPKLoading.Visible = true;
                    this.ConfigurationCAPKLoading.ForeColor = Color.White;
                    SoftBlink(this.ConfigurationCAPKLoading, Color.FromArgb(30, 30, 30), Color.Green, 2000, true);
                    System.Windows.Forms.Application.DoEvents();

                    new Thread(() => 
                    { 
                        try
                        { 
                            Thread.CurrentThread.IsBackground = true; devicePlugin.GetCapKList(); 
                        }
                        catch(Exception)
                        {
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.ConfigurationPanel1.Enabled = true;
                                this.ConfigurationCAPKSpicBoxWait.Visible = false;
                                this.ConfigurationCAPKSpicBoxWait.Enabled = false;
                            }));
                        }
                    }).Start();
                }));

                ConfigurationGROUPStabPagecomboBox1.SelectedIndex = -1;
            }
            else if (tabControlConfiguration.SelectedTab?.Name.Equals("ConfigurationGROUPStabPage") ?? false)
            {
                ConfigurationGROUPStabPagecomboBox1.SelectedIndex = 0;
            }
        }

        private void OnConfigurationTabControlVisibilityChanged(object sender, EventArgs e)
        {
            // Load Terminal Configuration
            //if (this.ConfigurationTerminalDatatabPage.Visible == true)
            //{
            //    this.tabControlConfiguration.SelectedIndex = -1;
            //    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
            //    this.tabControlConfiguration.SelectedIndex = 0;
            //}
        }

        private void OnConfigGroupSelectionChanged(object sender, EventArgs e)
        {
            if (ConfigurationGROUPStabPagecomboBox1.SelectedItem != null)
            {
                this.ConfigurationPanel1pictureBox1.Enabled = true;
                this.ConfigurationPanel1pictureBox1.Visible = true;
                this.ConfigurationGROUPSpicBoxWait.Visible = true;
                this.ConfigurationGROUPSpicBoxWait.Enabled = true;
                int group = Convert.ToInt16(ConfigurationGROUPStabPagecomboBox1.SelectedItem.ToString());
                new Thread(() =>
                {
                    try
                    {
                        Thread.CurrentThread.IsBackground = true;
                        devicePlugin.GetConfigGroup(group);
                    }
                    catch (Exception ex)
                    {
                        Logger.error("main: OnConfigGroupSelectionChanged() - exception={0}", (object)ex.Message);
                    }

                }).Start();
            }
        }

        private void ConfigurationResetLoadFromButtons()
        {
            foreach (RadioButton radio in ConfigurationGroupBox1.Controls.OfType<RadioButton>().ToList())
            {
                if (radio.Checked == true)
                {
                    radio.Checked = false;
                    break;
                }
            }
        }

        private void OnLoadFactory(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                this.ConfigurationIDgrpBox.Visible = false;
                this.ConfigurationPanel1btnDeviceMode.Enabled = false;
                this.ConfigurationPanel1btnEMVMode.Enabled = false;
                if (this.ConfigurationCollapseButton.Visible)
                {
                    this.ConfigurationCollapseButton.Visible = false;
                    this.ConfigurationPanel2.Visible = false;
                }
                ConfigurationResetLoadFromButtons();
                this.ConfigurationPanel1pictureBox1.Enabled = true;
                this.ConfigurationPanel1pictureBox1.Visible = true;
                System.Windows.Forms.Application.DoEvents();
            }));

            // Reset Configuration to Factory defaults AND load from device
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                int majorcfgint = devicePlugin?.GetConfigSphereSerializer()?.GetTerminalMajorConfiguration() ?? 2;
                devicePlugin.FactoryReset(majorcfgint);

                // Load Configuration from DEVICE
                devicePlugin.SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes.FROM_DEVICE);
                this.Invoke(new MethodInvoker(() =>
                {
                    radioLoadFromDevice.Checked = true;
                }));
            }).Start();
        }

        private void OnSetDeviceInterfaceType(object sender, EventArgs e)
        {
            string mode = this.ConfigurationPanel1btnDeviceMode.Text;

            // Disable Buttons
            this.ConfigurationPanel1btnEMVMode.Enabled = false;
            this.AdvancedFirmwarebtnUpdate.Enabled = false;

            // Switch over to Application TAB
            MaintabControl.SelectedTab = this.ApplicationtabPage;

            this.ApplicationpictureBoxWait.Enabled = true;
            this.ApplicationpictureBoxWait.Visible = true;

            new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;
                    devicePlugin.SetDeviceInterfaceType(mode);
                }
                catch (Exception ex)
                {
                    Logger.error("main: OnSetInterfaceType() - exception={0}", (object)ex.Message);
                }

            }).Start();
        }

        private void OnEMVModeDisable(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;
                    devicePlugin.DisableQCEmvMode();
                }
                catch (Exception ex)
                {
                    Logger.error("main: OnEMVModeDisable() - exception={0}", (object)ex.Message);
                }

            }).Start();

            // Disable Button
            this.ConfigurationPanel1btnEMVMode.Enabled = false;
        }

        private void OnLoadFromFile(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                this.ConfigurationPanel1pictureBox1.Enabled = true;
                this.ConfigurationPanel1pictureBox1.Visible = true;

                if(devicePlugin.ConfigFileLoaded())
                {
                    this.radioLoadFromFile.Checked = false;

                    string [] message = { "VERSION: ", "DEVICE" };
                    ConfigurationID configurationID = devicePlugin?.GetConfigSphereSerializer().GetConfigurationID();
                    if(configurationID != null)
                    {
                        message[0] += configurationID.Version;
                    }

                    ShowTerminalDataInfo(message);
                }
                else
                { 
                    // Load Configuration from FILE
                    new Thread(() => devicePlugin.SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes.FROM_CONFIG)).Start();

                    if (this.ConfigurationCollapseButton.Visible == false)
                    {
                        this.ConfigurationCollapseButton.Visible = true;
                        this.ConfigurationPanel2.Visible = true;
                    }

                    // Allows set back to TerminalData
                    this.tabControlConfiguration.SelectedIndex = -1;
                    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                    this.tabControlConfiguration.SelectedIndex = 0;

                    SetConfigLoaderTimer();
                }
            }
        }

        private void OnLoadFromDevice(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                this.ConfigurationPanel1.Enabled = false;

                // Load Configuration from DEVICE
                new Thread(() => devicePlugin.SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes.FROM_DEVICE)).Start();

                if (this.ConfigurationCollapseButton.Visible == false)
                {
                    this.ConfigurationCollapseButton.Visible = true;
                    this.ConfigurationPanel2.Visible = true;
                }

                // Allows set back to TerminalData
                this.tabControlConfiguration.SelectedIndex = -1;
                this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                this.tabControlConfiguration.SelectedIndex = 0;

                SetConfigLoaderTimer();
            }
        }

        #endregion

        /**************************************************************************/
        // SETTINGS TAB
        /**************************************************************************/
        #region -- settings tab --

        // SETTINGS: CONTROL
        public List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem> configBeepControl;
        public List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem> configLEDControl;
        public List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem> configEncryptionControl;

        public static void SetDeviceControlConfig(IDevicePlugIn devicePlugin, object payload)
        {
            try
            {
                // Make call to DeviceCfg
                devicePlugin.SetDeviceControlConfiguration(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("main: SetDeviceControlConfig() - exception={0}", (object)ex.Message);
            }
        }

        // SETTINGS: MSR
        public List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem> configExpirationMask;
        public List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem> configPanDigits;
        public List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem> configSwipeForceEncryption;
        public List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem> configSwipeMask;

        public static void SetDeviceMsrConfig(IDevicePlugIn devicePlugin, object payload)
        {
            try
            {
                // Make call to DeviceCfg
                devicePlugin.SetDeviceMsrConfiguration(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("main: SetDeviceMsrConfig() - exception={0}", (object)ex.Message);
            }
        }

        private void LoadControlSettings()
        {
            ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

            // Update settings
            if(serializer != null)
            {
                // Beep Control
                bool firmware_beep_control = serializer?.terminalCfg?.user_configuration?.firmware_beep_control ?? false;
                if(firmware_beep_control)
                { 
                    this.SettingsBeepControlradioButton1.Checked = true;
                }
                else
                { 
                    this.SettingsBeepControlradioButton2.Checked = true;
                }
                // LED Control
                this.SettingsLEDControlcheckBox1.Checked = serializer?.terminalCfg?.user_configuration?.firmware_LED_control_msr ?? false;
                this.SettingsLEDControlcheckBox2.Checked = serializer?.terminalCfg?.user_configuration?.firmware_LED_control_icc ?? false;
                // Encryption
                this.SettingsENCControlcheckBox1.Checked = serializer?.terminalCfg?.user_configuration?.encryption_msr ?? false;
                this.SettingsENCControlcheckBox2.Checked = serializer?.terminalCfg?.user_configuration?.encryption_icc ?? false;

                // Invoker without Parameter(s)
                this.Invoke((MethodInvoker)delegate()
                {
                    this.OnSettingsControlConfigureClick(this, null);
                });
            }

            loadMSRSettings = tc_show_msrsettings_group;
        }

        private void LoadMSRSettings()
        {
            ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

            // Update settings
            if(serializer != null)
            {
                // EXPIRATION MASK
                this.SettingsMsrcBxExpirationMask.Checked = serializer?.terminalCfg?.user_configuration?.expiration_masking ?? false;

                // PAN DIGITS
                this.SettingsMsrtxtPAN.Text = serializer?.terminalCfg?.user_configuration?.pan_clear_digits.ToString();

                // SWIPE FORCE
                this.SettingsMsrcBxTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track1 ?? false;
                this.SettingsMsrcBxTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track2 ?? false;
                this.SettingsMsrcBxTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3 ?? false;
                this.SettingsMsrcBxTrack3Card0.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3card0 ?? false;

                // SWIPE MASK
                this.SettingsMsrcBxSwipeMaskTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track1 ?? false;
                this.SettingsMsrcBxSwipeMaskTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track2 ?? false;
                this.SettingsMsrcBxSwipeMaskTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track3 ?? false;

                // Invoker without Parameter(s)
                this.Invoke((MethodInvoker)delegate()
                {
                    this.OnSettingsMsrConfigureClick(this, null);
                });
            }
        }

        private void OnSettingsControlActive(object sender, EventArgs e)
        {
            if(tc_show_controlsettings_group)
            {
                LoadControlSettings();
            }
            else if(tc_show_msrsettings_group)
            {
                LoadMSRSettings();
            }
        }

        private void SaveConfigurationControl()
        {
            try
            {
                ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

                // Update Configuration File
                if(serializer != null)
                {
                    // Update Data: Beep Control
                    serializer.terminalCfg.user_configuration.firmware_beep_control = this.SettingsBeepControlradioButton1.Checked;
                    // LED Control
                    serializer.terminalCfg.user_configuration.firmware_LED_control_msr = this.SettingsLEDControlcheckBox1.Checked;
                    serializer.terminalCfg.user_configuration.firmware_LED_control_icc = this.SettingsLEDControlcheckBox2.Checked;
                    // Encryption Control
                    serializer.terminalCfg.user_configuration.encryption_msr = this.SettingsENCControlcheckBox1.Checked;
                    serializer.terminalCfg.user_configuration.encryption_icc = this.SettingsENCControlcheckBox2.Checked;

                    // WRITE to Config
                    serializer.WriteConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("main: SaveConfiguration() - exception={0}", (object)ex.Message);
            }
        }

        private void SaveConfigurationMsr()
        {
            try
            {
                ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

                // Update Configuration File
                if(serializer != null)
                {
                    // Update Data: EXPIRATION MASKING
                    serializer.terminalCfg.user_configuration.expiration_masking = this.SettingsMsrcBxExpirationMask.Checked;
                    // PAN Clear Digits
                    serializer.terminalCfg.user_configuration.pan_clear_digits = Convert.ToInt32(this.SettingsMsrtxtPAN.Text);
                    // Swipe Force Mask
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track1 = this.SettingsMsrcBxTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track2 = this.SettingsMsrcBxTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3 = this.SettingsMsrcBxTrack3.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3card0 = this.SettingsMsrcBxTrack3Card0.Checked;
                    // Swipe Mask
                    serializer.terminalCfg.user_configuration.swipe_mask.track1 = this.SettingsMsrcBxSwipeMaskTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track2 = this.SettingsMsrcBxSwipeMaskTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track3 = this.SettingsMsrcBxSwipeMaskTrack3.Checked;

                    // WRITE to Config
                    serializer.WriteConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("main: SaveConfiguration() - exception={0}", (object)ex.Message);
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            if(this.RawModetxtCommand.Text.Length > 5)
            {
                this.RawModebtnExecute.Visible = true;
            }
            else
            {
                this.RawModebtnExecute.Visible = false;
            }
        }

        private void OnCardDataKeyEvent(object sender, KeyEventArgs e)
        {
            if(dev_usb_mode == DEV_USB_MODE.USB_KYB_MODE)
            {
                //Debug.WriteLine("main: key down event => key={0}", e.KeyData);
                // Start a new collection
                if(stopWatch.ElapsedMilliseconds > tc_transaction_collection_timeout)
                {
                    this.ApplicationtxtCardData.Text = "";
                    this.ApplicationtxtCardData.ForeColor = this.ApplicationtxtCardData.BackColor;
                    this.ApplicationbtnShowTags.Enabled = false;
                    this.ApplicationbtnShowTags.Visible = false;
                    this.ApplicationlistView1.Visible = false;
                    SetTransactionTimer();
                    Debug.WriteLine("main: new scan detected ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                }
                stopWatch.Restart();
            }
        }

        private void CardDataTextBoxGotFocus(object sender, EventArgs args)
        {
            HideCaret(this.ApplicationtxtCardData.Handle);
        }

        private void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            //ConfigurationResetLoadFromButtons();

            if (MaintabControl.SelectedTab?.Name.Equals("ConfigurationtabPage") ?? false)
            {
                //if(this.ConfigurationCollapseButton.Visible)
                //{
                //    this.ConfigurationCollapseButton.Visible = false;
                //    this.ConfigurationPanel2.Visible = false;
                //}
            }
            else if (MaintabControl.SelectedTab.Name.Equals("SettingstabPage"))
            {
                if(this.SettingstabPage.Enabled)
                { 
                    // Configuration Mode
                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.SettingstabPage.Enabled = true;

                        if(tc_show_controlsettings_group)
                        {
                            this.SettingsControlpictureBox1.Enabled = true;
                            this.SettingsControlpictureBox1.Visible = true;
                        }
                        if(tc_show_msrsettings_group)
                        {
                            this.SettingsMsrpicBoxWait.Enabled = true;
                            this.SettingsMsrpicBoxWait.Visible = true;
                        }
                    }));
                }
            }
            else if (MaintabControl.SelectedTab.Name.Equals("RawModetabPage"))
            {
                // Raw Mode TabPage: set focus to command field
                this.Invoke(new MethodInvoker(() =>
                {
                    this.RawModetxtCommand.Focus();
                }));
            }
            else if (MaintabControl.SelectedTab?.Name.Equals("AdvancedtabPage") ?? false)
            {
                int logLevel = Logger.GetFileLoggerLevel();
                if(logLevel == (int)LOGLEVELS.ALL)
                {
                    logLevel = (int)LOGLEVELS.DEBUG;
                }
                else if(logLevel > (int)LOGLEVELS.INFO)
                {
                    logLevel = (int)LOGLEVELS.INFO;
                }
                // Configuration Mode
                this.Invoke(new MethodInvoker(() =>
                {
                    ConfigurationGROUPStabPagecomboBox1.SelectedIndex = -1;
                    switch(logLevel)
                    {
                        case (int)LOGLEVELS.NONE:
                        {
                            this.AdvancedLoggingradioButtonNone.Checked = true;
                            break;
                        }
                        case (int)LOGLEVELS.INFO:
                        {
                            this.AdvancedLoggingradioButtonInfo.Checked = true;
                            break;
                        }
                        case (int)LOGLEVELS.DEBUG:
                        {
                            this.AdvancedLoggingradioButtonDebug.Checked = true;
                            break;
                        }
                    }

                }));
            }
        }

        private void OnDeselectingMainTabPage(object sender, TabControlCancelEventArgs e)
        {
            if(this.ApplicationpictureBoxWait.Visible   || this.JsonpicBoxWait.Visible                 ||
               this. SettingsControlpictureBox1.Visible || this. SettingsMsrpicBoxWait.Visible         ||
               this.AdvancedFirmwarepicBoxWait.Visible  || this.ConfigurationPanel1pictureBox1.Visible ||
               !this.ConfigurationPanel1.Enabled)
            {
                e.Cancel = true;
            }
        }

        private void OnDeselectingConfigurationTabPage(object sender, TabControlCancelEventArgs e)
        {
            if(this.ConfigurationTerminalDatapicBoxWait.Visible ||
               this.ConfigurationAIDSpicBoxWait.Visible         ||
               this.ConfigurationCAPKSpicBoxWait.Visible        ||
               this.ConfigurationGROUPSpicBoxWait.Visible)
            {
                e.Cancel = true;
            }
        }

        #endregion

        /**************************************************************************/
        // ADVANCED TAB
        /**************************************************************************/
        #region -- advanced tab --

        private void OnSetLoggerLevel(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                switch(((RadioButton)sender).Text)
                {
                    case "NONE":
                    { 
                        Logger.SetFileLoggerLevel((int)LOGLEVELS.NONE);
                        UpdateAppSetting("IPA.DAL.Application.Client.LogLevel", "NONE");
                        break;
                    }

                    case "INFO":
                    { 
                        Logger.SetFileLoggerLevel((int)LOGLEVELS.INFO);
                        Logger.info("LOGGING LEVEL SET TO INFO.");
                        UpdateAppSetting("IPA.DAL.Application.Client.LogLevel", "FATAL|ERROR|WARNING|INFO");
                        break;
                    }

                    case "DEBUG":
                    { 
                        Logger.SetFileLoggerLevel((int)LOGLEVELS.DEBUG);
                        Logger.info("LOGGING LEVEL SET TO DEBUG.");
                        UpdateAppSetting("IPA.DAL.Application.Client.LogLevel", "FATAL|ERROR|WARNING|INFO|DEBUG");
                        break;
                    }
                }
            }
        }

        #endregion

        /**************************************************************************/
        // ACTIONS TAB
        /**************************************************************************/
        #region -- actions tab --
        private void OnCardReadClick(object sender, EventArgs e)
        {
            // Disable Tab(s)
            this.ApplicationtabPage.Enabled = false;
            this.ConfigurationtabPage.Enabled = false;
            this.SettingstabPage.Enabled = false;
            this.RawModetabPage.Enabled = false;
            this.TerminalDatatabPage.Enabled = false;
            this.JsontabPage.Enabled = false;

            this.ApplicationbtnCardRead.Enabled = false;

            this.ApplicationbtnShowTags.Text = "TAGS";
            this.ApplicationbtnShowTags.Enabled = false;
            this.ApplicationbtnShowTags.Visible = false;
            this.ApplicationlistView1.Visible = false;

            // Clear field
            this.ApplicationtxtCardData.Text = "";
            this.ApplicationtxtCardData.ForeColor = this.ApplicationtxtCardData.BackColor;

            // Set Focus to reader input
            this.ApplicationtxtCardData.Focus();

            // MSR Read
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                devicePlugin.GetCardData();
            }).Start();
        }

        private void OnSetDeviceModeClick(object sender, EventArgs e)
        {
            TransactionTimer?.Stop();

            // Disable MODE Button
            this.ApplicationbtnMode.Enabled = false;
            this.ApplicationtxtCardData.Text = "";

            this.ApplicationpictureBoxWait.Enabled = true;
            this.ApplicationpictureBoxWait.Visible = true;

            string mode = this.ApplicationbtnMode.Text;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    devicePlugin.SetDeviceInterfaceType(mode);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("main: OnModeClick() exception={0}", (object)ex.Message);
                }

            }).Start();
        }

        private void OnSettingsControlConfigureClick(object sender, EventArgs e)
        {
            // Disable Tabs
            this.ApplicationtabPage.Enabled = false;
            this.RawModetabPage.Enabled = false;
            this.TerminalDatatabPage.Enabled = false;
            this.JsontabPage.Enabled = false;

            this.SettingsControlConfigureBtn.Enabled = false;
            this.SettingsBeepControlErrorlabel1.Visible = false;
            this.SettingsLEDControlErrorlabel1.Visible = false;
            this.SettingsEncryptionControlErrorlabel1.Visible = false;

            this.Invoke(new MethodInvoker(() =>
            {
                this.SettingsControlpictureBox1.Enabled = true;
                this.SettingsControlpictureBox1.Visible = true;
                this.SettingsControlpictureBox1.Refresh();
                System.Windows.Forms.Application.DoEvents();
            }));

            // BEEP CONTROL
            configBeepControl = new List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.ControlConfigItem() { Name="beepControl", Id=(int)CommonInterface.ConfigIDTech.Configuration.BEEP_CONTROL.HARDWARE, Value=string.Format("{0}", this.SettingsBeepControlradioButton1.Checked.ToString()) }},
            };

            // LED CONTROL
            configLEDControl = new List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.ControlConfigItem() { Name="ledControl", Id=(int)CommonInterface.ConfigIDTech.Configuration.LED_CONTROL.MSR, Value=string.Format("{0}", this.SettingsLEDControlcheckBox1.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.ControlConfigItem() { Name="ledControl", Id=(int)CommonInterface.ConfigIDTech.Configuration.LED_CONTROL.ICC, Value=string.Format("{0}", this.SettingsLEDControlcheckBox2.Checked.ToString()) }},
            };

            // ENCRYPTON CONTROL
            configEncryptionControl = new List<CommonInterface.ConfigIDTech.Configuration.ControlConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.ControlConfigItem() { Name="encryptionMSR", Id=(int)CommonInterface.ConfigIDTech.Configuration.ENCRYPTION_CONTROL.MSR, Value=string.Format("{0}", this.SettingsENCControlcheckBox1.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.ControlConfigItem() { Name="encryptionICC", Id=(int)CommonInterface.ConfigIDTech.Configuration.ENCRYPTION_CONTROL.ICC, Value=string.Format("{0}", this.SettingsENCControlcheckBox2.Checked.ToString()) }},
            };

            // Build Payload Package
            object payload = new object[] { configBeepControl, configLEDControl, configEncryptionControl };

            // Save to Configuration File
            if(e != null)
            {
                this.SettingsMSRgroupBox1.Enabled = false;
                SaveConfigurationControl();
            }

            // Settings Read
            new Thread(() => SetDeviceControlConfig(devicePlugin, payload)).Start();
        }

        private void OnSettingsMsrConfigureClick(object sender, EventArgs e)
        {
            // Disable Tabs
            this.ApplicationtabPage.Enabled = false;
            this.RawModetabPage.Enabled = false;
            this.TerminalDatatabPage.Enabled = false;
            this.JsontabPage.Enabled = false;

            this.Invoke(new MethodInvoker(() =>
            {
                this.SettingsMsrpicBoxWait.Enabled = true;
                this.SettingsMsrpicBoxWait.Visible = true;
                this.SettingsMsrpicBoxWait.Refresh();
                System.Windows.Forms.Application.DoEvents();
            }));

            // EXPIRATION MASK
            configExpirationMask = new List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="expirationmask", Id=(int)CommonInterface.ConfigIDTech.Configuration.EXPIRATION_MASK.MASK, Value=string.Format("{0}", this.SettingsMsrcBxExpirationMask.Checked.ToString()) }},
            };

            // PAN DIGITS
            configPanDigits = new List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="digits", Id=(int)CommonInterface.ConfigIDTech.Configuration.PAN_DIGITS.DIGITS, Value=string.Format("{0}", this.SettingsMsrtxtPAN.Text) }},
            };

            // SWIPE FORCE
            configSwipeForceEncryption = new List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track1",      Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_FORCE_ENCRYPTION.TRACK1, Value=string.Format("{0}",      this.SettingsMsrcBxTrack1.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track2",      Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_FORCE_ENCRYPTION.TRACK2, Value=string.Format("{0}",      this.SettingsMsrcBxTrack2.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track3",      Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_FORCE_ENCRYPTION.TRACK3, Value=string.Format("{0}",      this.SettingsMsrcBxTrack3.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track3Card0", Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_FORCE_ENCRYPTION.TRACK3CARD0, Value=string.Format("{0}", this.SettingsMsrcBxTrack3Card0.Checked.ToString()) }}
            };

            // SWIPE MASK
            configSwipeMask = new List<CommonInterface.ConfigIDTech.Configuration.MsrConfigItem>
            {
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track1", Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_MASK.TRACK1, Value=string.Format("{0}", this.SettingsMsrcBxSwipeMaskTrack1.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track2", Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_MASK.TRACK2, Value=string.Format("{0}", this.SettingsMsrcBxSwipeMaskTrack2.Checked.ToString()) }},
                { new CommonInterface.ConfigIDTech.Configuration.MsrConfigItem() { Name="track3", Id=(int)CommonInterface.ConfigIDTech.Configuration.SWIPE_MASK.TRACK3, Value=string.Format("{0}", this.SettingsMsrcBxSwipeMaskTrack3.Checked.ToString()) }}
            };

            // Build Payload Package
            object payload = new object[4] { configExpirationMask, configPanDigits, configSwipeForceEncryption, configSwipeMask };

            // Save to Configuration File
            if(e != null)
            {
                this.SettingsControlgroupBox1.Enabled = false;
                SaveConfigurationMsr();
            }

            // Settings Read
            new Thread(() => SetDeviceMsrConfig(devicePlugin, payload)).Start();
        }

        private void OnExecuteCommandClick(object sender, EventArgs e)
        {
            string command = this.RawModetxtCommand.Text;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                devicePlugin.DeviceCommand(command, true);
            }).Start();

            this.RawModebtnExecute.Enabled = false;
            this.RawModetxtCommandResult.Text = "";
        }

        private void OnCloseJsonClick(object sender, EventArgs e)
        {
            if(tc_show_json_tab && MaintabControl.Contains(JsontabPage))
            {
                MaintabControl.TabPages.Remove(JsontabPage);
            }
        }

        private void OnShowTagsClick(object sender, EventArgs e)
        {
            if(this.ApplicationbtnShowTags.Text.Equals("CLOSE"))
            {
                this.ApplicationbtnShowTags.Text = "TAGS";
                this.ApplicationlistView1.Visible = false;
            }
            else
            {
                this.ApplicationbtnShowTags.Text = "CLOSE";
                this.ApplicationlistView1.Visible = true;
            }
        }

        private void OnFirmwareUpdate(object sender, EventArgs e)
        {
            AdvancedFirmwareopenFileDialog1.Title = "FIRMWARE UPDATE";
            AdvancedFirmwareopenFileDialog1.Filter = "NGA FW Files|*.fm";
            AdvancedFirmwareopenFileDialog1.InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\Assets";

            if (AdvancedFirmwareopenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                byte[] bytes = System.IO.File.ReadAllBytes(AdvancedFirmwareopenFileDialog1.FileName);
                if (bytes.Length > 0)
                {
                    // Set the initial value of the ProgressBar.
                    this.AdvancedFirmwareprogressBar1.Value = 0;
                    this.AdvancedFirmwareprogressBar1.Maximum = bytes.Length / 1024;
                    this.AdvancedFirmwareprogressBar1.Step = 1;

                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.AdvancedtabPage.Enabled = true;
                        this.AdvancedFirmwarepicBoxWait.Enabled = true;
                        this.AdvancedFirmwarepicBoxWait.Visible = true;
                        this.AdvancedFirmwarelblVersion.Text = "UPDATING FIRMWARE (PLEASE DON'T INTERRUPT)...";
                        this.AdvancedFirmwarebtnUpdate.Visible = false;
                        this.AdvancedFirmwareprogressBar1.Visible = true;
                        System.Windows.Forms.Application.DoEvents();
                    }));

                    // Firmware Update
                    new Thread(() =>
                    {
                        try
                        {
                            Thread.CurrentThread.IsBackground = true;
                            devicePlugin.FirmwareUpdate(AdvancedFirmwareopenFileDialog1.FileName, bytes);
                        }
                        catch (Exception ex)
                        {
                            Logger.error("main: OnFirmwareUpdate() - exception={0}", (object)ex.Message);
                        }

                    }).Start();
                }
            }
        }

        #endregion
    }
}
