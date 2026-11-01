using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Security.Principal;
using System.Windows.Forms;

namespace CustomISLC
{
    public class MainForm : Form
    {
        private AppConfig _config;
        private Timer _monitorTimer;
        private bool _isMonitoring = false;
        private int _purgeCount = 0;
        private DateTime? _lastPurgeTime = null;
        private const int HOTKEY_ID = 9001;
        private const string TASK_NAME = "CustomISLC_AutoStart";

        // Painel 1: Estatísticas de Memória
        private Panel _cardMemory;
        private Label _lblMemTitle;
        private Label _lblTotalMemCap;
        private Label _lblTotalMemValue;
        private Label _lblStandbyMemCap;
        private Label _lblStandbyMemValue;
        private Label _lblStandbySubDetail;
        private Label _lblFreeMemCap;
        private Label _lblFreeMemValue;
        private Label _lblModifiedMemCap;
        private Label _lblModifiedMemValue;
        private Label _lblPurgeCountCap;
        private Label _lblPurgeCountValue;
        private Label _lblStatusBadge;
        private MemoryBarControl _memoryBar;

        // Painel 2: Regras de Limpeza Automática
        private Panel _cardConfig;
        private Label _lblRulesTitle;
        private Label _lblStandbyLimit;
        private Label _lblCondition;
        private Label _lblFreeMemLimit;
        private Label _lblPollingRate;
        private NumericUpDown _numStandbyLimit;
        private NumericUpDown _numFreeMemLimit;
        private ComboBox _cmbCondition;
        private ComboBox _cmbPollingRate;

        // Painel 3: Resolução do Timer do Windows
        private Panel _cardTimer;
        private Label _lblTimerTitle;
        private Label _lblCurrentTimerCap;
        private Label _lblCurrentTimerVal;
        private Label _lblMinMaxTimerCap;
        private Label _lblMinMaxTimerVal;
        private CheckBox _chkCustomTimer;
        private Label _lblWantedTimerCap;
        private NumericUpDown _numWantedTimer;

        // Painel 4: Otimização de Entrega de Imagem & Frametime
        private Panel _cardFrameOpt;
        private Label _lblFrameOptTitle;
        private CheckBox _chkSmartLowPrioPurge;
        private CheckBox _chkFlushModified;
        private CheckBox _chkAutoBoostFg;
        private Label _lblBoostedProcessVal;
        private CheckBox _chkMmcssGpuTweaks;
        private CheckBox _chkUltimatePowerPlan;
        private Label _lblActivePowerPlanVal;

        // Painel 5: Opções Gerais & Atalho Global
        private Panel _cardOptions;
        private Label _lblOptionsTitle;
        private ComboBox _cmbLanguage;
        private CheckBox _chkStartMinimized;
        private CheckBox _chkAutoStartMonitoring;
        private CheckBox _chkMinimizeOnClose;
        private CheckBox _chkAlwaysOnTop;
        private CheckBox _chkStartWithWindows;
        private CheckBox _chkDarkMode;
        private CheckBox _chkHotkeyEnabled;
        private ComboBox _cmbHotkeyMod;
        private ComboBox _cmbHotkeyKey;

        // Painel 6: Working Set & Lista de Exclusão de Processos
        private Panel _cardExclusions;
        private Label _lblExclusionsTitle;
        private CheckBox _chkPurgeWorkingSetAuto;
        private ListBox _lstExclusions;
        private TextBox _txtExclusionProc;
        private Button _btnAddExclusion;
        private Button _btnRemoveExclusion;

        // Botões Principais de Ação
        private Button _btnToggleMonitor;
        private Button _btnPurgeStandbyNow;
        private Button _btnPurgeWorkingSetNow;

        // System Tray
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayItemOpen;
        private ToolStripMenuItem _trayItemToggle;
        private ToolStripMenuItem _trayItemPurgeStandby;
        private ToolStripMenuItem _trayItemPurgeWS;
        private ToolStripMenuItem _trayItemExit;
        private Icon _appIcon;
        private bool _suppressEvents = false;

        public MainForm(string[] args)
        {
            _config = AppConfig.Load();
            ApplyCommandLineOverrides(args);
            Localization.SetLanguage(_config.Language);

            InitializeComponent();
            LoadConfigToUI();
            ApplyLocalization();
            ApplyTheme(_config.DarkMode);
            RegisterConfiguredHotkey();

            if (_config.CustomTimer)
            {
                NativeMethods.SetTimerResolution(_config.WantedResolution, true);
            }

            if (_config.MmcssGpuOptimization)
            {
                NativeMethods.ApplyFrameDeliverySystemTweaks(true);
            }

            if (_config.UltimatePowerPlan)
            {
                NativeMethods.SetHighPerformancePowerPlan(true);
            }

            _lblActivePowerPlanVal.Text = Localization.Get("ActivePlanPrefix") + NativeMethods.GetActivePowerPlanName();

            UpdateMemoryStats(false);

            if (_config.AutoStartMonitoring)
            {
                StartMonitoring();
            }

            if (_config.StartMinimized)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        private void ApplyCommandLineOverrides(string[] args)
        {
            if (args == null) return;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "-minimized")
                {
                    _config.StartMinimized = true;
                    _config.AutoStartMonitoring = true;
                }
                else if (arg == "-polling" && i + 1 < args.Length)
                {
                    _config.PollingRateSTR = args[++i];
                }
                else if (arg == "-listsize" && i + 1 < args.Length)
                {
                    _config.StandbyListValue = args[++i];
                }
                else if (arg == "-freememory" && i + 1 < args.Length)
                {
                    _config.FreeMemoryValue = args[++i];
                }
                else if (arg == "-lang" && i + 1 < args.Length)
                {
                    _config.Language = args[++i];
                }
            }
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private Icon CreateDynamicIcon(Color accentColor)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(24, 26, 32)))
                using (Pen border = new Pen(accentColor, 2.5f))
                using (SolidBrush barBrush = new SolidBrush(accentColor))
                {
                    g.FillEllipse(bg, 1, 1, 29, 29);
                    g.DrawEllipse(border, 2, 2, 27, 27);
                    g.FillRectangle(barBrush, 8, 16, 4, 8);
                    g.FillRectangle(barBrush, 14, 11, 4, 13);
                    g.FillRectangle(barBrush, 20, 8, 4, 16);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void InitializeComponent()
        {
            bool isAdmin = IsRunningAsAdmin();
            Text = Localization.Get("AppTitle") + (isAdmin ? Localization.Get("AdminSuffix") : Localization.Get("NoAdminSuffix"));
            Size = new Size(830, 745);
            MinimumSize = new Size(830, 745);
            MaximumSize = new Size(830, 745);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            DoubleBuffered = true;

            _appIcon = CreateDynamicIcon(Color.FromArgb(56, 189, 248));
            Icon = _appIcon;

            // =====================================================================
            // PAINEL 1: MONITORAMENTO DE MEMÓRIA
            // =====================================================================
            _cardMemory = CreateCard(16, 12, 782, 165);
            _lblMemTitle = CreateSectionTitle(Localization.Get("SectionMemoryTitle"), 14, 10);
            _lblStatusBadge = new Label
            {
                Text = Localization.Get("StatusStopped"),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 113, 113),
                AutoSize = true,
                Location = new Point(620, 10)
            };

            _memoryBar = new MemoryBarControl
            {
                Location = new Point(14, 36),
                Size = new Size(754, 26)
            };

            _lblTotalMemValue = CreateMetricBox(_cardMemory, Localization.Get("MetricTotalMemory"), "0 MB", 14, 72, Color.FromArgb(148, 163, 184), out _lblTotalMemCap);
            _lblStandbyMemValue = CreateMetricBox(_cardMemory, Localization.Get("MetricStandbyList"), "0 MB", 168, 72, Color.FromArgb(56, 189, 248), out _lblStandbyMemCap, out _lblStandbySubDetail);
            _lblFreeMemValue = CreateMetricBox(_cardMemory, Localization.Get("MetricFreeMemory"), "0 MB", 322, 72, Color.FromArgb(74, 222, 128), out _lblFreeMemCap);
            _lblModifiedMemValue = CreateMetricBox(_cardMemory, Localization.Get("MetricModifiedPages"), "0 MB", 476, 72, Color.FromArgb(251, 191, 36), out _lblModifiedMemCap);
            _lblPurgeCountValue = CreateMetricBox(_cardMemory, Localization.Get("MetricPurgesCount"), "0", 630, 72, Color.FromArgb(192, 132, 252), out _lblPurgeCountCap);

            _cardMemory.Controls.Add(_lblMemTitle);
            _cardMemory.Controls.Add(_lblStatusBadge);
            _cardMemory.Controls.Add(_memoryBar);

            // =====================================================================
            // PAINEL 2: REGRAS DE LIMPEZA AUTOMÁTICA
            // =====================================================================
            _cardConfig = CreateCard(16, 186, 385, 165);
            _lblRulesTitle = CreateSectionTitle(Localization.Get("SectionRulesTitle"), 14, 10);
            _cardConfig.Controls.Add(_lblRulesTitle);

            _lblStandbyLimit = CreateLabel(Localization.Get("StandbyLimitLabel"), 14, 40);
            _cardConfig.Controls.Add(_lblStandbyLimit);
            _numStandbyLimit = CreateNumeric(235, 37, 95, 64, 131072, 1024);
            _cardConfig.Controls.Add(_numStandbyLimit);
            _cardConfig.Controls.Add(CreateLabel("MB", 335, 40));

            _lblCondition = CreateLabel(Localization.Get("TriggerConditionLabel"), 14, 70);
            _cardConfig.Controls.Add(_lblCondition);
            _cmbCondition = new ComboBox
            {
                Location = new Point(235, 67),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCondition.Items.AddRange(new object[] { Localization.Get("ConditionAnd"), Localization.Get("ConditionOr") });
            _cmbCondition.SelectedIndex = 0;
            _cardConfig.Controls.Add(_cmbCondition);

            _lblFreeMemLimit = CreateLabel(Localization.Get("FreeMemoryLimitLabel"), 14, 100);
            _cardConfig.Controls.Add(_lblFreeMemLimit);
            _numFreeMemLimit = CreateNumeric(235, 97, 95, 64, 131072, 8000);
            _cardConfig.Controls.Add(_numFreeMemLimit);
            _cardConfig.Controls.Add(CreateLabel("MB", 335, 100));

            _lblPollingRate = CreateLabel(Localization.Get("PollingRateLabel"), 14, 130);
            _cardConfig.Controls.Add(_lblPollingRate);
            _cmbPollingRate = new ComboBox
            {
                Location = new Point(235, 127),
                Size = new Size(95, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPollingRate.Items.AddRange(new object[] { "250", "500", "1000", "2000", "5000", "10000" });
            _cmbPollingRate.SelectedIndex = 2;
            _cmbPollingRate.SelectedIndexChanged += (s, e) =>
            {
                if (_monitorTimer != null)
                {
                    int rate;
                    if (int.TryParse(_cmbPollingRate.Text, out rate) && rate >= 100)
                        _monitorTimer.Interval = rate;
                }
                SaveUIConfig();
            };
            _cardConfig.Controls.Add(_cmbPollingRate);
            _cardConfig.Controls.Add(CreateLabel("ms", 335, 130));

            // =====================================================================
            // PAINEL 3: RESOLUÇÃO DO TIMER DO WINDOWS
            // =====================================================================
            _cardTimer = CreateCard(413, 186, 385, 165);
            _lblTimerTitle = CreateSectionTitle(Localization.Get("SectionTimerTitle"), 14, 10);
            _cardTimer.Controls.Add(_lblTimerTitle);

            _lblCurrentTimerCap = CreateLabel(Localization.Get("CurrentTimerLabel"), 14, 40);
            _cardTimer.Controls.Add(_lblCurrentTimerCap);
            _lblCurrentTimerVal = new Label
            {
                Text = "1.00 ms",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Location = new Point(235, 39)
            };
            _cardTimer.Controls.Add(_lblCurrentTimerVal);

            _lblMinMaxTimerCap = CreateLabel(Localization.Get("SystemTimerLimitsLabel"), 14, 68);
            _cardTimer.Controls.Add(_lblMinMaxTimerCap);
            _lblMinMaxTimerVal = CreateLabel("15.62 ms / 0.50 ms", 235, 68);
            _cardTimer.Controls.Add(_lblMinMaxTimerVal);

            _chkCustomTimer = new CheckBox
            {
                Text = Localization.Get("EnableCustomTimer"),
                AutoSize = true,
                Location = new Point(14, 98)
            };
            _chkCustomTimer.CheckedChanged += OnCustomTimerChanged;
            _cardTimer.Controls.Add(_chkCustomTimer);

            _lblWantedTimerCap = CreateLabel(Localization.Get("DesiredResolutionLabel"), 14, 128);
            _cardTimer.Controls.Add(_lblWantedTimerCap);
            _numWantedTimer = new NumericUpDown
            {
                Location = new Point(235, 126),
                Size = new Size(95, 24),
                DecimalPlaces = 2,
                Increment = 0.10M,
                Minimum = 0.50M,
                Maximum = 15.60M,
                Value = 0.50M
            };
            _numWantedTimer.ValueChanged += OnCustomTimerChanged;
            _cardTimer.Controls.Add(_numWantedTimer);

            // =====================================================================
            // PAINEL 4: OTIMIZAÇÃO DE ENTREGA DE IMAGEM & FRAMETIME
            // =====================================================================
            _cardFrameOpt = CreateCard(16, 360, 782, 122);
            _lblFrameOptTitle = CreateSectionTitle(Localization.Get("SectionFrameOptTitle"), 14, 10);
            _cardFrameOpt.Controls.Add(_lblFrameOptTitle);

            _chkSmartLowPrioPurge = CreateCheckBox(Localization.Get("SmartLowPrioPurge"), 14, 34);
            _chkFlushModified = CreateCheckBox(Localization.Get("FlushModified"), 14, 60);

            _chkAutoBoostFg = CreateCheckBox(Localization.Get("AutoBoostForeground"), 14, 86);
            _lblBoostedProcessVal = new Label
            {
                Text = Localization.Get("BoostWaiting"),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                AutoSize = true,
                Location = new Point(422, 87)
            };

            _chkMmcssGpuTweaks = new CheckBox
            {
                Text = Localization.Get("MmcssGpuTweaks"),
                AutoSize = true,
                Location = new Point(415, 34)
            };
            _chkMmcssGpuTweaks.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                NativeMethods.ApplyFrameDeliverySystemTweaks(_chkMmcssGpuTweaks.Checked);
                SaveUIConfig();
            };

            _chkUltimatePowerPlan = new CheckBox
            {
                Text = Localization.Get("UltimatePowerPlan"),
                AutoSize = true,
                Location = new Point(415, 60)
            };
            _chkUltimatePowerPlan.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                NativeMethods.SetHighPerformancePowerPlan(_chkUltimatePowerPlan.Checked);
                _lblActivePowerPlanVal.Text = Localization.Get("ActivePlanPrefix") + NativeMethods.GetActivePowerPlanName();
                SaveUIConfig();
            };

            _lblActivePowerPlanVal = new Label
            {
                Text = Localization.Get("ActivePlanChecking"),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Location = new Point(605, 88)
            };

            _cardFrameOpt.Controls.AddRange(new Control[]
            {
                _chkSmartLowPrioPurge, _chkFlushModified, _chkAutoBoostFg,
                _lblBoostedProcessVal, _chkMmcssGpuTweaks, _chkUltimatePowerPlan,
                _lblActivePowerPlanVal
            });

            // =====================================================================
            // PAINEL 5: OPÇÕES GERAIS & ATALHO GLOBAL
            // =====================================================================
            _cardOptions = CreateCard(16, 492, 385, 145);
            _lblOptionsTitle = CreateSectionTitle(Localization.Get("SectionOptionsTitle"), 14, 10);
            _cardOptions.Controls.Add(_lblOptionsTitle);

            _cmbLanguage = new ComboBox
            {
                Location = new Point(235, 6),
                Size = new Size(135, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular)
            };
            _cmbLanguage.Items.AddRange(new object[]
            {
                "English",
                "Português (Brasil)",
                "Русский"
            });
            _cmbLanguage.SelectedIndexChanged += OnLanguageSelectedIndexChanged;
            _cardOptions.Controls.Add(_cmbLanguage);

            _chkStartMinimized = CreateCheckBox(Localization.Get("StartMinimized"), 14, 36);
            _chkAutoStartMonitoring = CreateCheckBox(Localization.Get("AutoStartMonitoring"), 205, 36);
            _chkMinimizeOnClose = CreateCheckBox(Localization.Get("MinimizeOnClose"), 14, 62);
            _chkAlwaysOnTop = CreateCheckBox(Localization.Get("AlwaysOnTop"), 205, 62);
            _chkStartWithWindows = CreateCheckBox(Localization.Get("StartWithWindows"), 14, 88);
            _chkDarkMode = CreateCheckBox(Localization.Get("DarkMode"), 205, 88);

            _chkAlwaysOnTop.CheckedChanged += (s, e) => { if (!_suppressEvents) { TopMost = _chkAlwaysOnTop.Checked; SaveUIConfig(); } };
            _chkDarkMode.CheckedChanged += (s, e) => { if (!_suppressEvents) { ApplyTheme(_chkDarkMode.Checked); SaveUIConfig(); } };
            _chkStartWithWindows.CheckedChanged += OnStartWithWindowsChanged;

            _chkHotkeyEnabled = new CheckBox
            {
                Text = Localization.Get("HotkeyLabel"),
                AutoSize = true,
                Location = new Point(14, 114)
            };
            _chkHotkeyEnabled.CheckedChanged += (s, e) => { if (!_suppressEvents) { RegisterConfiguredHotkey(); SaveUIConfig(); } };

            _cmbHotkeyMod = new ComboBox
            {
                Location = new Point(130, 111),
                Size = new Size(120, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbHotkeyMod.Items.AddRange(new object[]
            {
                "Ctrl + Alt",     // 3
                "Ctrl + Shift",   // 6
                "Alt + Shift",    // 5
                "Ctrl"            // 2
            });
            _cmbHotkeyMod.SelectedIndex = 0;
            _cmbHotkeyMod.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) { RegisterConfiguredHotkey(); SaveUIConfig(); } };

            _cmbHotkeyKey = new ComboBox
            {
                Location = new Point(258, 111),
                Size = new Size(108, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string keyName in new[] { "P", "M", "L", "F9", "F10", "F11", "F12", "Insert", "Delete" })
            {
                _cmbHotkeyKey.Items.Add(keyName);
            }
            _cmbHotkeyKey.SelectedIndex = 0;
            _cmbHotkeyKey.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) { RegisterConfiguredHotkey(); SaveUIConfig(); } };

            _cardOptions.Controls.AddRange(new Control[]
            {
                _chkStartMinimized, _chkAutoStartMonitoring, _chkMinimizeOnClose,
                _chkAlwaysOnTop, _chkStartWithWindows, _chkDarkMode,
                _chkHotkeyEnabled, _cmbHotkeyMod, _cmbHotkeyKey
            });

            // =====================================================================
            // PAINEL 6: WORKING SET & LISTA DE EXCLUSÃO DE PROCESSOS
            // =====================================================================
            _cardExclusions = CreateCard(413, 492, 385, 145);
            _lblExclusionsTitle = CreateSectionTitle(Localization.Get("SectionWorkingSetTitle"), 14, 10);
            _cardExclusions.Controls.Add(_lblExclusionsTitle);

            _chkPurgeWorkingSetAuto = CreateCheckBox(Localization.Get("PurgeWorkingSetAuto"), 14, 34);
            _cardExclusions.Controls.Add(_chkPurgeWorkingSetAuto);

            _lstExclusions = new ListBox
            {
                Location = new Point(14, 60),
                Size = new Size(210, 74),
                BorderStyle = BorderStyle.FixedSingle
            };
            _cardExclusions.Controls.Add(_lstExclusions);

            _txtExclusionProc = new TextBox
            {
                Location = new Point(232, 60),
                Size = new Size(138, 24),
                Text = Localization.Get("ExclusionPlaceholder")
            };
            _cardExclusions.Controls.Add(_txtExclusionProc);

            _btnAddExclusion = new Button
            {
                Text = Localization.Get("AddExclusion"),
                Location = new Point(232, 86),
                Size = new Size(138, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAddExclusion.Click += OnAddExclusionClick;
            _cardExclusions.Controls.Add(_btnAddExclusion);

            _btnRemoveExclusion = new Button
            {
                Text = Localization.Get("RemoveExclusion"),
                Location = new Point(232, 112),
                Size = new Size(138, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnRemoveExclusion.Click += OnRemoveExclusionClick;
            _cardExclusions.Controls.Add(_btnRemoveExclusion);

            // =====================================================================
            // BARRA INFERIOR DE BOTÕES DE AÇÃO
            // =====================================================================
            _btnToggleMonitor = new Button
            {
                Text = Localization.Get("BtnStartMonitoring"),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(16, 648),
                Size = new Size(270, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnToggleMonitor.Click += (s, e) =>
            {
                if (_isMonitoring) StopMonitoring();
                else StartMonitoring();
            };

            _btnPurgeStandbyNow = new Button
            {
                Text = Localization.Get("BtnPurgeStandbyNow"),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(298, 648),
                Size = new Size(255, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPurgeStandbyNow.Click += (s, e) => ExecutePurge(true, false);

            _btnPurgeWorkingSetNow = new Button
            {
                Text = Localization.Get("BtnPurgeWorkingSetNow"),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(565, 648),
                Size = new Size(233, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPurgeWorkingSetNow.Click += (s, e) => ExecutePurge(false, true);

            Controls.AddRange(new Control[]
            {
                _cardMemory, _cardConfig, _cardTimer, _cardFrameOpt, _cardOptions, _cardExclusions,
                _btnToggleMonitor, _btnPurgeStandbyNow, _btnPurgeWorkingSetNow
            });

            // Configurar Tray Icon
            _trayMenu = new ContextMenuStrip();
            _trayItemOpen = new ToolStripMenuItem(Localization.Get("TrayOpen"), null, (s, e) => RestoreFromTray());
            _trayItemOpen.Font = new Font(_trayItemOpen.Font, FontStyle.Bold);
            _trayItemToggle = new ToolStripMenuItem(Localization.Get("TrayStartMonitoring"), null, (s, e) =>
            {
                if (_isMonitoring) StopMonitoring();
                else StartMonitoring();
            });
            _trayItemPurgeStandby = new ToolStripMenuItem(Localization.Get("TrayPurgeStandbyNow"), null, (s, e) => ExecutePurge(true, false));
            _trayItemPurgeWS = new ToolStripMenuItem(Localization.Get("TrayPurgeWorkingSetNow"), null, (s, e) => ExecutePurge(false, true));
            _trayItemExit = new ToolStripMenuItem(Localization.Get("TrayExit"), null, (s, e) => ExitApplication());

            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                _trayItemOpen,
                new ToolStripSeparator(),
                _trayItemToggle,
                _trayItemPurgeStandby,
                _trayItemPurgeWS,
                new ToolStripSeparator(),
                _trayItemExit
            });

            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Text = "Custom ISLC Pro",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Timer principal
            _monitorTimer = new Timer();
            _monitorTimer.Interval = 1000;
            _monitorTimer.Tick += (s, e) => UpdateMemoryStats(_isMonitoring);

            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    ShowInTaskbar = false;
                }
            };

            FormClosing += (s, e) =>
            {
                SaveUIConfig();
                if (e.CloseReason == CloseReason.UserClosing && _chkMinimizeOnClose.Checked)
                {
                    e.Cancel = true;
                    WindowState = FormWindowState.Minimized;
                    ShowInTaskbar = false;
                }
                else
                {
                    CleanupResources();
                }
            };
        }

        private void ApplyLocalization()
        {
            bool isAdmin = IsRunningAsAdmin();
            Text = Localization.Get("AppTitle") + (isAdmin ? Localization.Get("AdminSuffix") : Localization.Get("NoAdminSuffix"));

            // Panel 1
            if (_lblMemTitle != null) _lblMemTitle.Text = Localization.Get("SectionMemoryTitle");
            if (_lblStatusBadge != null)
            {
                _lblStatusBadge.Text = _isMonitoring ? Localization.Get("StatusRunning") : Localization.Get("StatusStopped");
            }
            if (_lblTotalMemCap != null) _lblTotalMemCap.Text = Localization.Get("MetricTotalMemory");
            if (_lblStandbyMemCap != null) _lblStandbyMemCap.Text = Localization.Get("MetricStandbyList");
            if (_lblFreeMemCap != null) _lblFreeMemCap.Text = Localization.Get("MetricFreeMemory");
            if (_lblModifiedMemCap != null) _lblModifiedMemCap.Text = Localization.Get("MetricModifiedPages");
            if (_lblPurgeCountCap != null) _lblPurgeCountCap.Text = Localization.Get("MetricPurgesCount");

            // Panel 2
            if (_lblRulesTitle != null) _lblRulesTitle.Text = Localization.Get("SectionRulesTitle");
            if (_lblStandbyLimit != null) _lblStandbyLimit.Text = Localization.Get("StandbyLimitLabel");
            if (_lblCondition != null) _lblCondition.Text = Localization.Get("TriggerConditionLabel");
            if (_lblFreeMemLimit != null) _lblFreeMemLimit.Text = Localization.Get("FreeMemoryLimitLabel");
            if (_lblPollingRate != null) _lblPollingRate.Text = Localization.Get("PollingRateLabel");
            if (_cmbCondition != null)
            {
                int sel = _cmbCondition.SelectedIndex;
                _cmbCondition.Items.Clear();
                _cmbCondition.Items.AddRange(new object[] { Localization.Get("ConditionAnd"), Localization.Get("ConditionOr") });
                _cmbCondition.SelectedIndex = (sel >= 0 && sel < 2) ? sel : 0;
            }

            // Panel 3
            if (_lblTimerTitle != null) _lblTimerTitle.Text = Localization.Get("SectionTimerTitle");
            if (_lblCurrentTimerCap != null) _lblCurrentTimerCap.Text = Localization.Get("CurrentTimerLabel");
            if (_lblMinMaxTimerCap != null) _lblMinMaxTimerCap.Text = Localization.Get("SystemTimerLimitsLabel");
            if (_chkCustomTimer != null) _chkCustomTimer.Text = Localization.Get("EnableCustomTimer");
            if (_lblWantedTimerCap != null) _lblWantedTimerCap.Text = Localization.Get("DesiredResolutionLabel");

            // Panel 4
            if (_lblFrameOptTitle != null) _lblFrameOptTitle.Text = Localization.Get("SectionFrameOptTitle");
            if (_chkSmartLowPrioPurge != null) _chkSmartLowPrioPurge.Text = Localization.Get("SmartLowPrioPurge");
            if (_chkFlushModified != null) _chkFlushModified.Text = Localization.Get("FlushModified");
            if (_chkAutoBoostFg != null) _chkAutoBoostFg.Text = Localization.Get("AutoBoostForeground");
            if (_chkMmcssGpuTweaks != null) _chkMmcssGpuTweaks.Text = Localization.Get("MmcssGpuTweaks");
            if (_chkUltimatePowerPlan != null) _chkUltimatePowerPlan.Text = Localization.Get("UltimatePowerPlan");
            if (_lblActivePowerPlanVal != null)
            {
                _lblActivePowerPlanVal.Text = Localization.Get("ActivePlanPrefix") + NativeMethods.GetActivePowerPlanName();
            }

            // Panel 5
            if (_lblOptionsTitle != null) _lblOptionsTitle.Text = Localization.Get("SectionOptionsTitle");
            if (_chkStartMinimized != null) _chkStartMinimized.Text = Localization.Get("StartMinimized");
            if (_chkAutoStartMonitoring != null) _chkAutoStartMonitoring.Text = Localization.Get("AutoStartMonitoring");
            if (_chkMinimizeOnClose != null) _chkMinimizeOnClose.Text = Localization.Get("MinimizeOnClose");
            if (_chkAlwaysOnTop != null) _chkAlwaysOnTop.Text = Localization.Get("AlwaysOnTop");
            if (_chkStartWithWindows != null) _chkStartWithWindows.Text = Localization.Get("StartWithWindows");
            if (_chkDarkMode != null) _chkDarkMode.Text = Localization.Get("DarkMode");
            if (_chkHotkeyEnabled != null) _chkHotkeyEnabled.Text = Localization.Get("HotkeyLabel");

            // Panel 6
            if (_lblExclusionsTitle != null) _lblExclusionsTitle.Text = Localization.Get("SectionWorkingSetTitle");
            if (_chkPurgeWorkingSetAuto != null) _chkPurgeWorkingSetAuto.Text = Localization.Get("PurgeWorkingSetAuto");
            if (_btnAddExclusion != null) _btnAddExclusion.Text = Localization.Get("AddExclusion");
            if (_btnRemoveExclusion != null) _btnRemoveExclusion.Text = Localization.Get("RemoveExclusion");

            // Action buttons
            if (_btnToggleMonitor != null)
            {
                _btnToggleMonitor.Text = _isMonitoring ? Localization.Get("BtnStopMonitoring") : Localization.Get("BtnStartMonitoring");
            }
            if (_btnPurgeStandbyNow != null) _btnPurgeStandbyNow.Text = Localization.Get("BtnPurgeStandbyNow");
            if (_btnPurgeWorkingSetNow != null) _btnPurgeWorkingSetNow.Text = Localization.Get("BtnPurgeWorkingSetNow");

            // Tray Menu
            if (_trayItemOpen != null) _trayItemOpen.Text = Localization.Get("TrayOpen");
            if (_trayItemToggle != null)
            {
                _trayItemToggle.Text = _isMonitoring ? Localization.Get("TrayStopMonitoring") : Localization.Get("TrayStartMonitoring");
            }
            if (_trayItemPurgeStandby != null) _trayItemPurgeStandby.Text = Localization.Get("TrayPurgeStandbyNow");
            if (_trayItemPurgeWS != null) _trayItemPurgeWS.Text = Localization.Get("TrayPurgeWorkingSetNow");
            if (_trayItemExit != null) _trayItemExit.Text = Localization.Get("TrayExit");

            if (_memoryBar != null)
            {
                _memoryBar.LegendTemplate = Localization.Get("MemoryBarLegend");
                _memoryBar.Invalidate();
            }
        }

        private void OnLanguageSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            string selectedLang = Localization.LANG_EN;
            if (_cmbLanguage.SelectedIndex == 1)
                selectedLang = Localization.LANG_PT_BR;
            else if (_cmbLanguage.SelectedIndex == 2)
                selectedLang = Localization.LANG_RU;

            _config.Language = selectedLang;
            Localization.SetLanguage(selectedLang);
            ApplyLocalization();
            UpdateMemoryStats(false);
            SaveUIConfig();
        }

        private Panel CreateCard(int x, int y, int w, int h)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private Label CreateSectionTitle(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Location = new Point(x, y)
            };
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y)
            };
        }

        private CheckBox CreateCheckBox(string text, int x, int y)
        {
            CheckBox chk = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y)
            };
            chk.CheckedChanged += (s, e) => { if (!_suppressEvents) SaveUIConfig(); };
            return chk;
        }

        private NumericUpDown CreateNumeric(int x, int y, int width, decimal min, decimal max, decimal val)
        {
            NumericUpDown num = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 24),
                Minimum = min,
                Maximum = max,
                Value = val,
                Increment = 256
            };
            num.ValueChanged += (s, e) => { if (!_suppressEvents) SaveUIConfig(); };
            return num;
        }

        private Label CreateMetricBox(Panel parent, string caption, string initialValue, int x, int y, Color valueColor, out Label capLabel, out Label subDetailLabel)
        {
            Panel box = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(140, 80),
                Tag = "metricBox"
            };

            capLabel = new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Location = new Point(8, 6),
                AutoSize = true,
                Tag = "metricCaption"
            };

            Label lblVal = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = valueColor,
                Location = new Point(8, 25),
                AutoSize = true,
                Tag = "metricValue"
            };

            subDetailLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 7.25f, FontStyle.Regular),
                Location = new Point(8, 56),
                AutoSize = true,
                Tag = "metricCaption"
            };

            box.Controls.Add(capLabel);
            box.Controls.Add(lblVal);
            box.Controls.Add(subDetailLabel);
            parent.Controls.Add(box);
            return lblVal;
        }

        private Label CreateMetricBox(Panel parent, string caption, string initialValue, int x, int y, Color valueColor, out Label capLabel)
        {
            Label dummy;
            return CreateMetricBox(parent, caption, initialValue, x, y, valueColor, out capLabel, out dummy);
        }

        private void LoadConfigToUI()
        {
            _suppressEvents = true;
            try
            {
                if (string.Equals(_config.Language, Localization.LANG_PT_BR, StringComparison.OrdinalIgnoreCase))
                    _cmbLanguage.SelectedIndex = 1;
                else if (string.Equals(_config.Language, Localization.LANG_RU, StringComparison.OrdinalIgnoreCase))
                    _cmbLanguage.SelectedIndex = 2;
                else
                    _cmbLanguage.SelectedIndex = 0;

                int standbyVal;
                if (int.TryParse(_config.StandbyListValue, out standbyVal))
                    _numStandbyLimit.Value = Math.Max(_numStandbyLimit.Minimum, Math.Min(_numStandbyLimit.Maximum, standbyVal));

                int freeVal;
                if (int.TryParse(_config.FreeMemoryValue, out freeVal))
                    _numFreeMemLimit.Value = Math.Max(_numFreeMemLimit.Minimum, Math.Min(_numFreeMemLimit.Maximum, freeVal));

                _cmbCondition.SelectedIndex = _config.TriggerConditionOr ? 1 : 0;
                _cmbCondition.SelectedIndexChanged += (s, e) => { if (!_suppressEvents) SaveUIConfig(); };

                int idxPolling = _cmbPollingRate.Items.IndexOf(_config.PollingRateSTR);
                if (idxPolling >= 0) _cmbPollingRate.SelectedIndex = idxPolling;

                int pollMs;
                if (int.TryParse(_config.PollingRateSTR, out pollMs) && pollMs >= 100)
                    _monitorTimer.Interval = pollMs;

                _chkCustomTimer.Checked = _config.CustomTimer;
                decimal wantedDec = (decimal)_config.WantedResolution;
                if (wantedDec >= _numWantedTimer.Minimum && wantedDec <= _numWantedTimer.Maximum)
                    _numWantedTimer.Value = wantedDec;

                _chkSmartLowPrioPurge.Checked = _config.SmartLowPriorityPurge;
                _chkFlushModified.Checked = _config.FlushModifiedBeforePurge;
                _chkAutoBoostFg.Checked = _config.AutoBoostForeground;
                _chkMmcssGpuTweaks.Checked = _config.MmcssGpuOptimization;
                _chkUltimatePowerPlan.Checked = _config.UltimatePowerPlan;

                _chkStartMinimized.Checked = _config.StartMinimized;
                _chkAutoStartMonitoring.Checked = _config.AutoStartMonitoring;
                _chkMinimizeOnClose.Checked = _config.MinimizeOnClose;
                _chkAlwaysOnTop.Checked = _config.AlwaysOnTop;
                TopMost = _config.AlwaysOnTop;
                _chkDarkMode.Checked = _config.DarkMode;
                _chkPurgeWorkingSetAuto.Checked = _config.PurgeWorkingSetOnAuto;

                _chkHotkeyEnabled.Checked = _config.HotkeyEnabled;
                _cmbHotkeyMod.SelectedIndex = ModifiersToComboIndex(_config.HotkeyModifiers);
                _cmbHotkeyKey.SelectedIndex = VKeyToComboIndex(_config.HotkeyVKey);

                _lstExclusions.Items.Clear();
                if (_config.ExclusionList != null)
                {
                    foreach (string item in _config.ExclusionList)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            _lstExclusions.Items.Add(item);
                    }
                }

                _chkStartWithWindows.Checked = IsStartupTaskRegistered();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void SaveUIConfig()
        {
            if (_suppressEvents || _numStandbyLimit == null || _numFreeMemLimit == null) return;

            if (_cmbLanguage != null)
            {
                if (_cmbLanguage.SelectedIndex == 1)
                    _config.Language = Localization.LANG_PT_BR;
                else if (_cmbLanguage.SelectedIndex == 2)
                    _config.Language = Localization.LANG_RU;
                else
                    _config.Language = Localization.LANG_EN;
            }

            _config.StandbyListValue = ((int)_numStandbyLimit.Value).ToString();
            _config.FreeMemoryValue = ((int)_numFreeMemLimit.Value).ToString();
            _config.TriggerConditionOr = (_cmbCondition.SelectedIndex == 1);
            _config.PollingRateSTR = _cmbPollingRate.Text;
            _config.CustomTimer = _chkCustomTimer.Checked;
            _config.WantedResolution = (double)_numWantedTimer.Value;

            _config.SmartLowPriorityPurge = _chkSmartLowPrioPurge.Checked;
            _config.FlushModifiedBeforePurge = _chkFlushModified.Checked;
            _config.AutoBoostForeground = _chkAutoBoostFg.Checked;
            _config.MmcssGpuOptimization = _chkMmcssGpuTweaks.Checked;
            _config.UltimatePowerPlan = _chkUltimatePowerPlan.Checked;

            _config.StartMinimized = _chkStartMinimized.Checked;
            _config.AutoStartMonitoring = _chkAutoStartMonitoring.Checked;
            _config.MinimizeOnClose = _chkMinimizeOnClose.Checked;
            _config.AlwaysOnTop = _chkAlwaysOnTop.Checked;
            _config.DarkMode = _chkDarkMode.Checked;
            _config.PurgeWorkingSetOnAuto = _chkPurgeWorkingSetAuto.Checked;
            _config.HotkeyEnabled = _chkHotkeyEnabled.Checked;
            _config.HotkeyModifiers = ComboIndexToModifiers(_cmbHotkeyMod.SelectedIndex);
            _config.HotkeyVKey = ComboIndexToVKey(_cmbHotkeyKey.SelectedIndex);

            _config.ExclusionList.Clear();
            foreach (object item in _lstExclusions.Items)
            {
                _config.ExclusionList.Add(item.ToString());
            }

            _config.Save();
        }

        private void OnCustomTimerChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            double wanted = (double)_numWantedTimer.Value;
            NativeMethods.SetTimerResolution(wanted, _chkCustomTimer.Checked);
            UpdateTimerDisplay();
            SaveUIConfig();
        }

        private void UpdateTimerDisplay()
        {
            double min, max, cur;
            NativeMethods.GetTimerResolution(out min, out max, out cur);
            _lblCurrentTimerVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00} ms", cur);
            _lblMinMaxTimerVal.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00} ms / {1:0.00} ms", min, max);
        }

        private void StartMonitoring()
        {
            _isMonitoring = true;
            _numStandbyLimit.Enabled = false;
            _numFreeMemLimit.Enabled = false;
            _cmbCondition.Enabled = false;
            _lblStatusBadge.Text = Localization.Get("StatusRunning");
            _lblStatusBadge.ForeColor = Color.FromArgb(74, 222, 128);
            _btnToggleMonitor.Text = Localization.Get("BtnStopMonitoring");
            _btnToggleMonitor.BackColor = Color.FromArgb(220, 38, 38);
            _btnToggleMonitor.ForeColor = Color.White;
            _trayItemToggle.Text = Localization.Get("TrayStopMonitoring");
            _monitorTimer.Start();
            UpdateMemoryStats(true);
        }

        private void StopMonitoring()
        {
            _isMonitoring = false;
            _numStandbyLimit.Enabled = true;
            _numFreeMemLimit.Enabled = true;
            _cmbCondition.Enabled = true;
            _lblStatusBadge.Text = Localization.Get("StatusStopped");
            _lblStatusBadge.ForeColor = Color.FromArgb(248, 113, 113);
            _btnToggleMonitor.Text = Localization.Get("BtnStartMonitoring");
            _btnToggleMonitor.BackColor = Color.FromArgb(16, 185, 129);
            _btnToggleMonitor.ForeColor = Color.White;
            _trayItemToggle.Text = Localization.Get("TrayStartMonitoring");
            NativeMethods.RestoreLastBoostedProcess();
            _lblBoostedProcessVal.Text = Localization.Get("BoostStopped");
            _monitorTimer.Start();
        }

        private void UpdateMemoryStats(bool checkAutoPurge)
        {
            NativeMethods.MemorySnapshot snap = NativeMethods.GetMemorySnapshot();

            _lblTotalMemValue.Text = string.Format("{0:N0} MB", snap.TotalPhysicalMB);
            _lblStandbyMemValue.Text = string.Format("{0:N0} MB", snap.StandbyListMB);
            if (_lblStandbySubDetail != null)
            {
                _lblStandbySubDetail.Text = string.Format(Localization.Get("StandbySubDetail"), snap.LowPriorityStandbyMB, snap.HighPriorityStandbyMB);
            }
            _lblFreeMemValue.Text = string.Format("{0:N0} MB", snap.FreeMemoryMB);
            _lblModifiedMemValue.Text = string.Format("{0:N0} MB", snap.ModifiedMemoryMB);

            ulong usedMB = 0;
            if (snap.TotalPhysicalMB > snap.StandbyListMB + snap.FreeMemoryMB)
                usedMB = snap.TotalPhysicalMB - snap.StandbyListMB - snap.FreeMemoryMB;

            _memoryBar.UpdateValues(snap.TotalPhysicalMB, usedMB, snap.StandbyListMB, snap.FreeMemoryMB);
            UpdateTimerDisplay();

            if (_isMonitoring)
            {
                string boosted = NativeMethods.UpdateForegroundProcessBoost(_chkAutoBoostFg.Checked);
                if (boosted == "Nenhum" || boosted == "Waiting for focus..." || string.IsNullOrEmpty(boosted))
                    _lblBoostedProcessVal.Text = Localization.Get("BoostWaiting");
                else if (boosted == "Desativado" || boosted == "Disabled")
                    _lblBoostedProcessVal.Text = Localization.Get("BoostDisabled");
                else
                    _lblBoostedProcessVal.Text = boosted;
            }

            string stateStr = _isMonitoring ? Localization.Get("TrayStateActive") : Localization.Get("TrayStateStopped");
            string trayTooltip = string.Format(Localization.Get("TrayTooltip"), stateStr, snap.StandbyListMB, snap.FreeMemoryMB, _purgeCount);
            if (trayTooltip.Length > 63)
                trayTooltip = trayTooltip.Substring(0, 63);
            _trayIcon.Text = trayTooltip;

            if (checkAutoPurge && _isMonitoring)
            {
                ulong standbyLimit = (ulong)_numStandbyLimit.Value;
                ulong freeLimit = (ulong)_numFreeMemLimit.Value;
                bool useOr = (_cmbCondition.SelectedIndex == 1);

                ulong effectiveStandby = _chkSmartLowPrioPurge.Checked ? snap.LowPriorityStandbyMB : snap.StandbyListMB;

                bool shouldPurge = useOr
                    ? (snap.StandbyListMB >= standbyLimit || snap.FreeMemoryMB < freeLimit)
                    : (snap.StandbyListMB >= standbyLimit && snap.FreeMemoryMB < freeLimit);

                if (shouldPurge && effectiveStandby >= 32)
                {
                    ExecutePurge(true, _chkPurgeWorkingSetAuto.Checked);
                }
            }
        }

        public void ExecutePurge(bool purgeStandby, bool purgeWorkingSet)
        {
            bool ok = false;
            if (purgeStandby)
            {
                if (_chkFlushModified != null && _chkFlushModified.Checked)
                {
                    NativeMethods.FlushModifiedList();
                }

                bool lowPrioOnly = (_chkSmartLowPrioPurge != null && _chkSmartLowPrioPurge.Checked);
                ok = NativeMethods.PurgeStandbyList(lowPrioOnly);
            }
            if (purgeWorkingSet)
            {
                int count = NativeMethods.EmptyProcessWorkingSets(_config.ExclusionList);
                if (count > 0) ok = true;
            }

            if (ok)
            {
                _purgeCount++;
                _lastPurgeTime = DateTime.Now;
                _lblPurgeCountValue.Text = string.Format("{0} ({1:HH:mm:ss})", _purgeCount, _lastPurgeTime.Value);
            }

            UpdateMemoryStats(false);
        }

        private void OnAddExclusionClick(object sender, EventArgs e)
        {
            string proc = _txtExclusionProc.Text.Trim();
            if (!string.IsNullOrEmpty(proc) && !_lstExclusions.Items.Contains(proc))
            {
                _lstExclusions.Items.Add(proc);
                _txtExclusionProc.Clear();
                SaveUIConfig();
            }
        }

        private void OnRemoveExclusionClick(object sender, EventArgs e)
        {
            if (_lstExclusions.SelectedIndex >= 0)
            {
                _lstExclusions.Items.RemoveAt(_lstExclusions.SelectedIndex);
                SaveUIConfig();
            }
        }

        private void ApplyTheme(bool dark)
        {
            Color bgMain = dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(241, 245, 249);
            Color bgCard = dark ? Color.FromArgb(30, 41, 59) : Color.White;
            Color bgBox = dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
            Color fgPrimary = dark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(30, 41, 59);
            Color fgSecondary = dark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
            Color inputBg = dark ? Color.FromArgb(15, 23, 42) : Color.White;

            BackColor = bgMain;
            ForeColor = fgPrimary;

            foreach (Panel card in new[] { _cardMemory, _cardConfig, _cardTimer, _cardFrameOpt, _cardOptions, _cardExclusions })
            {
                card.BackColor = bgCard;
                card.ForeColor = fgPrimary;
                foreach (Control c in card.Controls)
                {
                    if (c is Panel && (string)c.Tag == "metricBox")
                    {
                        c.BackColor = bgBox;
                        foreach (Control sub in c.Controls)
                        {
                            if ((string)sub.Tag == "metricCaption")
                                sub.ForeColor = fgSecondary;
                        }
                    }
                    else if (c is NumericUpDown || c is ComboBox || c is TextBox || c is ListBox)
                    {
                        c.BackColor = inputBg;
                        c.ForeColor = fgPrimary;
                    }
                    else if (c is Button)
                    {
                        c.BackColor = dark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240);
                        c.ForeColor = fgPrimary;
                    }
                }
            }

            _btnToggleMonitor.BackColor = _isMonitoring ? Color.FromArgb(220, 38, 38) : Color.FromArgb(16, 185, 129);
            _btnToggleMonitor.ForeColor = Color.White;
            _btnToggleMonitor.FlatAppearance.BorderSize = 0;

            _btnPurgeStandbyNow.BackColor = Color.FromArgb(2, 132, 199);
            _btnPurgeStandbyNow.ForeColor = Color.White;
            _btnPurgeStandbyNow.FlatAppearance.BorderSize = 0;

            _btnPurgeWorkingSetNow.BackColor = Color.FromArgb(124, 58, 237);
            _btnPurgeWorkingSetNow.ForeColor = Color.White;
            _btnPurgeWorkingSetNow.FlatAppearance.BorderSize = 0;

            _memoryBar.SetDarkMode(dark);
        }

        private void RegisterConfiguredHotkey()
        {
            NativeMethods.UnregisterHotKey(Handle, HOTKEY_ID);
            if (_chkHotkeyEnabled != null && _chkHotkeyEnabled.Checked)
            {
                uint mods = (uint)ComboIndexToModifiers(_cmbHotkeyMod.SelectedIndex);
                uint vk = (uint)ComboIndexToVKey(_cmbHotkeyKey.SelectedIndex);
                NativeMethods.RegisterHotKey(Handle, HOTKEY_ID, mods, vk);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ExecutePurge(true, _chkPurgeWorkingSetAuto.Checked);
            }
            base.WndProc(ref m);
        }

        private static int ModifiersToComboIndex(int mod)
        {
            switch (mod)
            {
                case 3: return 0; // Ctrl + Alt
                case 6: return 1; // Ctrl + Shift
                case 5: return 2; // Alt + Shift
                case 2: return 3; // Ctrl
                default: return 0;
            }
        }

        private static int ComboIndexToModifiers(int idx)
        {
            switch (idx)
            {
                case 0: return 3;
                case 1: return 6;
                case 2: return 5;
                case 3: return 2;
                default: return 3;
            }
        }

        private static int VKeyToComboIndex(int vk)
        {
            switch (vk)
            {
                case 80: return 0;  // P
                case 77: return 1;  // M
                case 76: return 2;  // L
                case 120: return 3; // F9
                case 121: return 4; // F10
                case 122: return 5; // F11
                case 123: return 6; // F12
                case 45: return 7;  // Insert
                case 46: return 8;  // Delete
                default: return 0;
            }
        }

        private static int ComboIndexToVKey(int idx)
        {
            switch (idx)
            {
                case 0: return 80;
                case 1: return 77;
                case 2: return 76;
                case 3: return 120;
                case 4: return 121;
                case 5: return 122;
                case 6: return 123;
                case 7: return 45;
                case 8: return 46;
                default: return 80;
            }
        }

        private bool IsStartupTaskRegistered()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", "/Query /TN \"" + TASK_NAME + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(2000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void OnStartWithWindowsChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            try
            {
                string exePath = Application.ExecutablePath;
                if (_chkStartWithWindows.Checked)
                {
                    string args = string.Format("/Create /F /RL HIGHEST /SC ONLOGON /TN \"{0}\" /TR \"\\\"{1}\\\" -minimized\"", TASK_NAME, exePath);
                    ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", args)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(3000);
                    }
                }
                else
                {
                    ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", "/Delete /F /TN \"" + TASK_NAME + "\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(3000);
                    }
                }
            }
            catch
            {
            }
        }

        private void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void CleanupResources()
        {
            NativeMethods.UnregisterHotKey(Handle, HOTKEY_ID);
            NativeMethods.RestoreLastBoostedProcess();
            if (_chkCustomTimer != null && _chkCustomTimer.Checked)
            {
                NativeMethods.SetTimerResolution((double)_numWantedTimer.Value, false);
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }

        private void ExitApplication()
        {
            SaveUIConfig();
            CleanupResources();
            Application.Exit();
        }
    }

    public class MemoryBarControl : Control
    {
        private ulong _total = 16384;
        private ulong _used = 4096;
        private ulong _standby = 1024;
        private ulong _free = 11264;
        private bool _dark = true;
        public string LegendTemplate { get; set; }

        public MemoryBarControl()
        {
            DoubleBuffered = true;
            LegendTemplate = Localization.Get("MemoryBarLegend");
        }

        public void SetDarkMode(bool dark)
        {
            _dark = dark;
            Invalidate();
        }

        public void UpdateValues(ulong total, ulong used, ulong standby, ulong free)
        {
            _total = total > 0 ? total : 1;
            _used = used;
            _standby = standby;
            _free = free;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            using (SolidBrush bgBrush = new SolidBrush(_dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(226, 232, 240)))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            int usedW = (int)Math.Round((double)_used / _total * w);
            int standbyW = (int)Math.Round((double)_standby / _total * w);
            int freeW = Math.Max(0, w - usedW - standbyW);

            using (SolidBrush usedBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
            using (SolidBrush standbyBrush = new SolidBrush(Color.FromArgb(56, 189, 248)))
            using (SolidBrush freeBrush = new SolidBrush(Color.FromArgb(34, 197, 94)))
            {
                if (usedW > 0)
                    g.FillRectangle(usedBrush, 0, 0, usedW, h);
                if (standbyW > 0)
                    g.FillRectangle(standbyBrush, usedW, 0, standbyW, h);
                if (freeW > 0)
                    g.FillRectangle(freeBrush, usedW + standbyW, 0, freeW, h);
            }

            string fmt = string.IsNullOrEmpty(LegendTemplate) ? Localization.Get("MemoryBarLegend") : LegendTemplate;
            string legend = string.Format(fmt, _used, _standby, _free);
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush textShadow = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                SizeF sz = g.MeasureString(legend, f);
                float tx = (w - sz.Width) / 2f;
                float ty = (h - sz.Height) / 2f;
                g.DrawString(legend, f, textShadow, tx + 1, ty + 1);
                g.DrawString(legend, f, textBrush, tx, ty);
            }
        }
    }
}
