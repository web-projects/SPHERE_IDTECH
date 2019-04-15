using System;
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

        // AppDomain Artifacts
        AppDomainCfg appDomainCfg;
        AppDomain appDomainDevice;
        IDevicePlugIn devicePlugin;
        const string MODULE_NAME = "DeviceConfiguration";
        const string PLUGIN_NAME = "IPA.DAL.RBADAL.DeviceCfg";
        string ASSEMBLY_NAME = typeof(IPA.DAL.RBADAL.DeviceCfg).Assembly.FullName;

        // Application Configuration
        bool tc_show_settings_tab;
        bool tc_show_raw_mode_tab;
        bool tc_show_terminal_data_tab;
        bool tc_show_json_tab;
        bool tc_show_advanced_tab;
        bool tc_always_on_top;
        int  tc_transaction_timeout;
        int  tc_configloader_timeout;
        int  tc_transaction_collection_timeout;
        int  tc_minimum_transaction_length;
        bool isNonAugusta;

        DEV_USB_MODE dev_usb_mode;

        // Timers
        Stopwatch stopWatch;

        internal static System.Timers.Timer TransactionTimer { get; set; }
        internal static System.Timers.Timer ConfigLoaderTimer { get; set; }

        Color TEXTBOX_FORE_COLOR;

        // Always on TOP
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        #endregion

        public Application()
        {
            InitializeComponent();

            this.Text = "IDTECH Device Discovery Application";

            // Initial CONFIG Tab Size
            this.tabControlConfiguration.Width += CONFIG_PANEL_WIDTH;

            // Settings Tab
            string show_settings_tab = System.Configuration.ConfigurationManager.AppSettings["tc_show_settings_tab"] ?? "false";
            bool.TryParse(show_settings_tab, out tc_show_settings_tab);
            if(!tc_show_settings_tab)
            {
                MaintabControl.TabPages.Remove(SettingstabPage);
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

            // Initialize Device
            InitalizeDevice();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
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
            try
            {
                var logLevels = ConfigurationManager.AppSettings["IPA.DAL.Application.Client.LogLevel"]?.Split('|') ?? new string[0];
                if(logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = System.IO.Directory.GetCurrentDirectory(); 
                    string filepath = path + "\\" + logname;

                    int levels = 0;
                    foreach(var item in logLevels)
                    {
                        foreach(var level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);
                    Logger.info( "{0} VERSION {1}.", System.IO.Path.GetFileNameWithoutExtension(fullName).ToUpper(), Assembly.GetEntryAssembly().GetName().Version);
                }
            }
            catch(Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", (object) e.Message);
            }
        }

        private void UpdateAppSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
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

                case NOTIFICATION_TYPE.NT_GET_DEVICE_CONFIGURATION:
                {
                    GetDeviceConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_CONFIGURATION:
                {
                    SetDeviceConfigurationUI(sender, args);
                    break;
                }

                case NOTIFICATION_TYPE.NT_SET_DEVICE_MODE:
                {
                    SetDeviceModeUI(sender, args);
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
                this.ApplicationlblFirmwareVersion.Text = "";
                this.ApplicationlblModelName.Text = "";
                this.ApplicationlblModelNumber.Text = "";
                this.ApplicationlblPort.Text = "";
                this.ApplicationtxtCardData.Text = "";

                // Disable Tab(s)
                this.ApplicationtabPage.Enabled = false;
                this.ConfigurationtabPage.Enabled = false;
                this.SettingstabPage.Enabled = false;
                this.RawModetabPage.Enabled = false;
                this.TerminalDatatabPage.Enabled = false;
                this.JsontabPage.Enabled = false;

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

        private void GetDeviceConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            GetDeviceConfiguration(e.Message);
        }

        private void SetDeviceConfigurationUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceConfiguration(e.Message);
        }

        private void SetDeviceModeUI(object sender, DeviceNotificationEventArgs e)
        {
            SetDeviceMode(e.Message);
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

        private void ShowAidListUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowAidList(e.Message);
        }

        private void ShowCapKListUI(object sender, DeviceNotificationEventArgs e)
        {
            ShowCapKList(e.Message);
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
                this.ApplicationlblFirmwareVersion.Text = "";
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
            this.ApplicationlblFirmwareVersion.Text = "";
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
                    this.ApplicationlblFirmwareVersion.Text = config[1];
                    this.ApplicationlblModelName.Text = config[2];
                    this.ApplicationlblModelNumber.Text = config[3];

                    this.lblFirmwareVersion.Text = config[1];
                    this.btnFirmwareUpdate.Enabled = true;
                    this.btnFirmwareUpdate.Visible = true;
                    this.FirmwareprogressBar1.Visible = false;

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
                        if(!worker1.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                        {
                            worker1 += "/";
                            if(worker[1].Equals("KB"))
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
                this.SettingspicBoxWait.Enabled = false;
                this.SettingspicBoxWait.Visible = false;

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
                                this.ApplicationpictureBoxWait.Enabled = true;
                                this.ApplicationpictureBoxWait.Visible = true;
                                this.MaintabControl.SelectedTab = this.ApplicationtabPage;
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
                        this.btnFirmwareUpdate.Enabled = false;
                        this.lblFirmwareVersion.Text = "UNKNOWN";

                        MaintabControl.SelectedTab = this.ApplicationtabPage;

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
                    }));
                }
            }

            if(!tc_show_json_tab)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    this.ApplicationtabPage.Enabled = true;
                    this.ApplicationpictureBoxWait.Enabled = true;
                    this.ApplicationpictureBoxWait.Visible = true;
                    this.MaintabControl.SelectedTab = this.ApplicationtabPage;
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
                    bool updateView = true;
                    bool.TryParse((string) data[4], out updateView);
                    if(updateView)
                    {
                        this.ApplicationtxtCardData.Text = (string) data[0];
                        this.ApplicationbtnCardRead.Enabled = (dev_usb_mode == DEV_USB_MODE.USB_HID_MODE) ? true : false;

                        this.ApplicationtxtCardData.ForeColor = TEXTBOX_FORE_COLOR;

                        // Set TAGS to display
                        bool isHIDMode = true;
                        if(bool.TryParse((string) data[3], out isHIDMode))
                        {
                            SetTagData((string) data[1], isHIDMode);
                        }
                        else
                        {
                            SetTagData((string) data[1], isHIDMode);
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
                catch (Exception exp)
                {
                    Debug.WriteLine("main: ProcessCardData() - exception={0}", (object)exp.Message);
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

        private void GetDeviceConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

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
                    this.SettingspicBoxWait.Enabled = false;
                    this.SettingspicBoxWait.Visible = false;
                    this.JsonpicBoxWait.Visible = false;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: GetDeviceConfiguration() - exception={0}", (object)exp.Message);
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

        private void SetDeviceConfiguration(object payload)
        {
            // Invoker with Parameter(s)
            MethodInvoker mi = () =>
            {
                try
                {
                    // update settings in panel
                    string [] data = ((IEnumerable) payload).Cast<object>().Select(x => x == null ? "" : x.ToString()).ToArray();

                    // Expiration Mask
                    this.SettingscBxExpirationMask.Checked = data[0].Equals("Masked", StringComparison.OrdinalIgnoreCase) ? true : false;

                    // PAN Clear Digits
                    this.SettingstxtPAN.Text = data[1];

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
                        bool t1Val = t1Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t2Val = t2Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t3Val = t3Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        bool t3Card0Val = t3Card0Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.SettingscBxTrack1.Checked != t1Val) {
                            this.SettingscBxTrack1.Checked = t1Val;
                        }

                        if(this.SettingscBxTrack2.Checked != t2Val) {
                            this.SettingscBxTrack2.Checked = t2Val;
                        }

                        if(this.SettingscBxTrack3.Checked != t3Val) {
                            this.SettingscBxTrack3.Checked = t3Val;
                        }

                        if(this.SettingscBxTrack3Card0.Checked != t3Card0Val) {
                            this.SettingscBxTrack3Card0.Checked = t3Card0Val;
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

                        t1Val = t1Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        t2Val = t2Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;
                        t3Val = t3Value.Equals("ON", StringComparison.OrdinalIgnoreCase) ? true : false;

                        // Compare to existing values
                        if(this.SettingscBxSwipeMaskTrack1.Checked != t1Val) 
                        {
                            this.SettingscBxSwipeMaskTrack1.Checked = t1Val;
                        }

                        if(this.SettingscBxSwipeMaskTrack2.Checked != t2Val) 
                        {
                            this.SettingscBxSwipeMaskTrack2.Checked = t2Val;
                        }

                        if(this.SettingscBxSwipeMaskTrack3.Checked != t3Val) 
                        {
                            this.SettingscBxSwipeMaskTrack3.Checked = t3Val;
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

                    this.SettingspicBoxWait.Visible  = false;
                    this.SettingspicBoxWait.Enabled = false;
                    this.JsonpicBoxWait.Visible  = false;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
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

        private void SetDeviceMode(object payload)
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
                        MaintabControl.SelectedTab = this.ApplicationtabPage;
                    }
                    else
                    {
                        dev_usb_mode = DEV_USB_MODE.USB_HID_MODE;

                        this.ApplicationbtnCardRead.Enabled = true;

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
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
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
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
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
                    catch (Exception exp)
                    {
                        Logger.error("main: ShowTerminalData() - exception={0}", (object) exp.Message);
                    }
                    finally
                    {
                        StopConfigLoaderTimer();
                        this.ConfigurationTerminalDatapicBoxWait.Enabled = false;
                        this.ConfigurationTerminalDatapicBoxWait.Visible  = false;
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
                catch (Exception exp)
                {
                    Logger.error("main: ShowAIDData() - exception={0}", (object) exp.Message);
                }
                finally
                {
                    this.ConfigurationAIDSpicBoxWait.Enabled = false;
                    this.ConfigurationAIDSpicBoxWait.Visible  = false;
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
                catch (Exception exp)
                {
                    Logger.error("main: ShowCapKList() - exception={0}", (object) exp.Message);
                }
                finally
                {
                    this.ConfigurationCAPKSpicBoxWait.Enabled = false;
                    this.ConfigurationCAPKSpicBoxWait.Visible  = false;
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
                catch (Exception exp)
                {
                    Logger.error("main: ShowConfigGroup() - exception={0}", (object) exp.Message);
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
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine("main: ShowJsonConfig() - exception={0}", (object) exp.Message);
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
            else if(this.JsonpicBoxWait.Visible == true)
            {
                this.JsonpicBoxWait.Visible = false;
                MaintabControl.SelectedTab = this.ApplicationtabPage;
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
                catch (Exception exp)
                {
                    Debug.WriteLine("main: SetModeButtonEnabled() - exception={0}", (object) exp.Message);
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
                this.FirmwareprogressBar1.PerformStep();
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
                this.lblFirmwareVersion.Text = data[0];
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
                this.lblFirmwareVersion.Text = data[0];
                Thread.Sleep(3000);
                this.btnFirmwareUpdate.Visible = true;
                this.btnFirmwareUpdate.Enabled = true;
                this.FirmwarepicBoxWait.Enabled = false;
                this.FirmwarepicBoxWait.Visible = false;
                this.FirmwareprogressBar1.Visible = false;
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

                this.lblFirmwareVersion.Text = data[0];
                this.btnFirmwareUpdate.Visible = true;
                this.btnFirmwareUpdate.Enabled = false;
                this.FirmwarepicBoxWait.Enabled = false;
                this.FirmwarepicBoxWait.Visible = false;
                this.FirmwareprogressBar1.Visible = false;
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
            ConfigLoaderTimer = new System.Timers.Timer(tc_configloader_timeout);
            ConfigLoaderTimer.AutoReset = false;
            ConfigLoaderTimer.Elapsed += (sender, e) => ConfigLoaderRaiseTimerExpired(new TimerEventArgs { Timer = TimerType.Transaction });
            ConfigLoaderTimer.Start();
        }
        private void StopConfigLoaderTimer()
        {
            ConfigLoaderTimer?.Stop();
        }

        private void SetTransactionTimer()
        {
            TransactionTimer = new System.Timers.Timer(tc_transaction_timeout);
            TransactionTimer.AutoReset = false;
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
        }

        private void OnExpandConfigurationView(object sender, EventArgs e)
        {
            this.ConfigurationPanel1.Visible = true;
            this.ConfigurationExpandButton.Visible = false;
            this.tabControlConfiguration.Width /= 2;
            this.tabControlConfiguration.Width += CONFIG_PANEL_WIDTH;
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
                    this.ConfigurationPanel1.Enabled = false;
                    this.ConfigurationTerminalDatapicBoxWait.Visible = true;
                    this.ConfigurationTerminalDatapicBoxWait.Enabled = true;
                    System.Windows.Forms.Application.DoEvents();
                    new Thread(() => { Thread.CurrentThread.IsBackground = true; devicePlugin. GetSphereTerminalData(); }).Start();
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
                    System.Windows.Forms.Application.DoEvents();
                    new Thread(() => { Thread.CurrentThread.IsBackground = true; devicePlugin.GetAIDList(); }).Start();
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
                    System.Windows.Forms.Application.DoEvents();
                    new Thread(() => { Thread.CurrentThread.IsBackground = true; devicePlugin.GetCapKList(); }).Start();
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
                        Logger.error("main: exception={0}", (object)ex.Message);
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
                devicePlugin.FactoryReset();
                // Load Configuration from DEVICE
                devicePlugin.SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes.FROM_DEVICE);
                this.Invoke(new MethodInvoker(() =>
                {
                    radioLoadFromDevice.Checked = true;
                }));
            }).Start();
        }

        private void OnSetDeviceMode(object sender, EventArgs e)
        {
            string mode = this.ConfigurationPanel1btnDeviceMode.Text;
            new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;
                    devicePlugin.SetDeviceMode(mode);
                }
                catch (Exception ex)
                {
                    Logger.error("main: exception={0}", (object)ex.Message);
                }

            }).Start();

            // Disable Buttons
            this.ConfigurationPanel1btnEMVMode.Enabled = false;
            this.btnFirmwareUpdate.Enabled = false;
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
                    Logger.error("main: exception={0}", (object)ex.Message);
                }

            }).Start();

            // Disable Button
            this.ConfigurationPanel1btnEMVMode.Enabled = false;
        }

        private void OnLoadFromFile(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                this.ConfigurationPanel1.Enabled = false;

                // Load Configuration from FILE
                new Thread(() => devicePlugin.SetConfigurationMode(IPA.Core.Shared.Enums.ConfigurationModes.FROM_CONFIG)).Start();

                SetConfigLoaderTimer();

                if (this.ConfigurationCollapseButton.Visible == false)
                {
                    this.ConfigurationCollapseButton.Visible = true;
                    this.ConfigurationPanel2.Visible = true;
                    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                }

                if (this.tabControlConfiguration.SelectedIndex == 0)
                {
                    this.tabControlConfiguration.SelectedIndex = -1;
                    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                    this.tabControlConfiguration.SelectedIndex = 0;
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

                SetConfigLoaderTimer();

                if (this.ConfigurationCollapseButton.Visible == false)
                {
                    this.ConfigurationCollapseButton.Visible = true;
                    this.ConfigurationPanel2.Visible = true;
                    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                }

                if (this.tabControlConfiguration.SelectedIndex == 0)
                {
                    this.tabControlConfiguration.SelectedIndex = -1;
                    this.tabControlConfiguration.SelectedTab = this.ConfigurationTerminalDatatabPage;
                    this.tabControlConfiguration.SelectedIndex = 0;
                }
            }
        }

        #endregion

        /**************************************************************************/
        // SETTINGS TAB
        /**************************************************************************/
        #region -- settings tab --

        public List<MsrConfigItem> configExpirationMask;
        public List<MsrConfigItem> configPanDigits;
        public List<MsrConfigItem> configSwipeForceEncryption;
        public List<MsrConfigItem> configSwipeMask;

        public static void SetDeviceConfig(IDevicePlugIn devicePlugin, object payload)
        {
            try
            {
                // Make call to DeviceCfg
                devicePlugin.SetDeviceConfiguration(payload);
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: SetDeviceConfiguration() - exception={0}", (object)exp.Message);
            }
        }

        private void OnSettingsControlActive(object sender, EventArgs e)
        {
            ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

            // Update settings
            if(serializer != null)
            {
                // EXPIRATION MASK
                this.SettingscBxExpirationMask.Checked = serializer?.terminalCfg?.user_configuration?.expiration_masking?? false;

                // PAN DIGITS
                this.SettingstxtPAN.Text = serializer?.terminalCfg?.user_configuration?.pan_clear_digits.ToString();

                // SWIPE FORCE
                this.SettingscBxTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track1?? false;
                this.SettingscBxTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track2?? false;
                this.SettingscBxTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3?? false;
                this.SettingscBxTrack3Card0.Checked = serializer?.terminalCfg?.user_configuration?.swipe_force_mask.track3card0?? false;

                // SWIPE MASK
                this.SettingscBxSwipeMaskTrack1.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track1?? false;
                this.SettingscBxSwipeMaskTrack2.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track2?? false;
                this.SettingscBxSwipeMaskTrack3.Checked = serializer?.terminalCfg?.user_configuration?.swipe_mask.track3?? false;

                // Invoker without Parameter(s)
                this.Invoke((MethodInvoker)delegate()
                {
                    this.OnConfigureClick(this, null);
                });
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                ConfigIDTechSerializer serializer = devicePlugin.GetConfigIDTechSerializer();

                // Update Configuration File
                if(serializer != null)
                {
                    // Update Data: EXPIRATION MASKING
                    serializer.terminalCfg.user_configuration.expiration_masking = this.SettingscBxExpirationMask.Checked;
                    // PAN Clear Digits
                    serializer.terminalCfg.user_configuration.pan_clear_digits = Convert.ToInt32(this.SettingstxtPAN.Text);
                    // Swipe Force Mask
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track1 = this.SettingscBxTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track2 = this.SettingscBxTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3 = this.SettingscBxTrack3.Checked;
                    serializer.terminalCfg.user_configuration.swipe_force_mask.track3card0 = this.SettingscBxTrack3Card0.Checked;
                    // Swipe Mask
                    serializer.terminalCfg.user_configuration.swipe_mask.track1 = this.SettingscBxSwipeMaskTrack1.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track2 = this.SettingscBxSwipeMaskTrack2.Checked;
                    serializer.terminalCfg.user_configuration.swipe_mask.track3 = this.SettingscBxSwipeMaskTrack3.Checked;

                    // WRITE to Config
                    serializer.WriteConfig();
                }
            }
            catch (Exception exp)
            {
                Debug.WriteLine("main: SaveConfiguration() - exception={0}", (object)exp.Message);
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
                        this.SettingspicBoxWait.Enabled = true;
                        this.SettingspicBoxWait.Visible = true;
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
            else if (MaintabControl.SelectedTab?.Name.Equals("FirmwaretabPage") ?? false)
            {
                // Configuration Mode
                this.Invoke(new MethodInvoker(() =>
                {
                    ConfigurationGROUPStabPagecomboBox1.SelectedIndex = -1;
                }));
            }
        }

        private void OnDeselectingMainTabPage(object sender, TabControlCancelEventArgs e)
        {
            if(this.ApplicationpictureBoxWait.Visible || this.JsonpicBoxWait.Visible      ||
               this. SettingspicBoxWait.Visible       || this.FirmwarepicBoxWait.Visible  ||
               this.ConfigurationPanel1pictureBox1.Visible || !this.ConfigurationPanel1.Enabled)
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
                        UpdateAppSetting("IPA.DAL.Application.Client.LogLevel", "INFO|WARNING|ERROR|FATAL");
                        break;
                    }

                    case "DEBUG":
                    { 
                        Logger.SetFileLoggerLevel((int)LOGLEVELS.DEBUG);
                        Logger.info("LOGGING LEVEL SET TO DEBUG.");
                        UpdateAppSetting("IPA.DAL.Application.Client.LogLevel", "DEBUG|INFO|WARNING|ERROR|FATAL");
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

        private void OnModeClick(object sender, EventArgs e)
        {
            string mode = this.ApplicationbtnMode.Text;
            TransactionTimer?.Stop();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    devicePlugin.SetDeviceMode(mode);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("main: OnModeClick() exception={0}", (object)ex.Message);
                }

            }).Start();

            // Disable MODE Button
            this.ApplicationbtnMode.Enabled = false;
            // Clear Card Data
            this.ApplicationtxtCardData.Text = "";
        }

        private void OnConfigureClick(object sender, EventArgs e)
        {
            // Disable Tabs
            this.ApplicationtabPage.Enabled = false;
            this.RawModetabPage.Enabled = false;
            this.TerminalDatatabPage.Enabled = false;
            this.JsontabPage.Enabled = false;

            this.Invoke(new MethodInvoker(() =>
            {
                this.SettingstabPage.Enabled = true;
                this.SettingspicBoxWait.Enabled = true;
                this.SettingspicBoxWait.Visible = true;
                this.SettingspicBoxWait.Refresh();
                System.Windows.Forms.Application.DoEvents();
            }));

            // EXPIRATION MASK
            configExpirationMask = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="expirationmask", Id=(int)EXPIRATION_MASK.MASK, Value=string.Format("{0}", this.SettingscBxExpirationMask.Checked.ToString()) }},
            };

            // PAN DIGITS
            configPanDigits = new List<MsrConfigItem>
            {
            { new MsrConfigItem() { Name="digits", Id=(int)PAN_DIGITS.DIGITS, Value=string.Format("{0}", this.SettingstxtPAN.Text) }},
            };

            // SWIPE FORCE
            configSwipeForceEncryption = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="track1",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK1, Value=string.Format("{0}",      this.SettingscBxTrack1.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track2",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK2, Value=string.Format("{0}",      this.SettingscBxTrack2.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3",      Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK3, Value=string.Format("{0}",      this.SettingscBxTrack3.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3Card0", Id=(int)SWIPE_FORCE_ENCRYPTION.TRACK3CARD0, Value=string.Format("{0}", this.SettingscBxTrack3Card0.Checked.ToString()) }}
            };

            // SWIPE MASK
            configSwipeMask = new List<MsrConfigItem>
            {
                { new MsrConfigItem() { Name="track1", Id=(int)SWIPE_MASK.TRACK1, Value=string.Format("{0}", this.SettingscBxSwipeMaskTrack1.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track2", Id=(int)SWIPE_MASK.TRACK2, Value=string.Format("{0}", this.SettingscBxSwipeMaskTrack2.Checked.ToString()) }},
                { new MsrConfigItem() { Name="track3", Id=(int)SWIPE_MASK.TRACK3, Value=string.Format("{0}", this.SettingscBxSwipeMaskTrack3.Checked.ToString()) }}
            };

            // Build Payload Package
            object payload = new object[4] { configExpirationMask, configPanDigits, configSwipeForceEncryption, configSwipeMask };

            // Save to Configuration File
            if(e != null)
            {
                SaveConfiguration();
            }

            // Settings Read
            new Thread(() => SetDeviceConfig(devicePlugin, payload)).Start();
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
            FirmwareopenFileDialog1.Title = "FIRMWARE UPDATE";
            FirmwareopenFileDialog1.Filter = "NGA FW Files|*.fm";
            FirmwareopenFileDialog1.InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\Assets";

            if (FirmwareopenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                byte[] bytes = System.IO.File.ReadAllBytes(FirmwareopenFileDialog1.FileName);
                if (bytes.Length > 0)
                {
                    // Set the initial value of the ProgressBar.
                    this.FirmwareprogressBar1.Value = 0;
                    this.FirmwareprogressBar1.Maximum = bytes.Length / 1024;
                    this.FirmwareprogressBar1.Step = 1;

                    this.Invoke(new MethodInvoker(() =>
                    {
                        this.AdvancedtabPage.Enabled = true;
                        this.FirmwarepicBoxWait.Enabled = true;
                        this.FirmwarepicBoxWait.Visible = true;
                        this.lblFirmwareVersion.Text = "UPDATING FIRMWARE (PLEASE DON'T INTERRUPT)...";
                        this.btnFirmwareUpdate.Visible = false;
                        this.FirmwareprogressBar1.Visible = true;
                        System.Windows.Forms.Application.DoEvents();
                    }));

                    // Firmware Update
                    new Thread(() =>
                    {
                        try
                        {
                            Thread.CurrentThread.IsBackground = true;
                            devicePlugin.FirmwareUpdate(FirmwareopenFileDialog1.FileName, bytes);
                        }
                        catch (Exception ex)
                        {
                            Logger.error("main: exception={0}", (object)ex.Message);
                        }

                    }).Start();
                }
            }
        }

        #endregion
    }
}
