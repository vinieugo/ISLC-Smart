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
        private Label _lblTotalMemValue;
        private Label _lblStandbyMemValue;
        private Label _lblStandbySubDetail;
        private Label _lblFreeMemValue;
        private Label _lblModifiedMemValue;
        private Label _lblPurgeCountValue;
        private Label _lblStatusBadge;
        private MemoryBarControl _memoryBar;

        // Painel 2: Regras de Limpeza Automática
        private Panel _cardConfig;
        private NumericUpDown _numStandbyLimit;
        private NumericUpDown _numFreeMemLimit;
        private ComboBox _cmbCondition;
        private ComboBox _cmbPollingRate;

        // Painel 3: Resolução do Timer do Windows
        private Panel _cardTimer;
        private Label _lblCurrentTimerVal;
        private Label _lblMinMaxTimerVal;
        private CheckBox _chkCustomTimer;
        private NumericUpDown _numWantedTimer;

        // Painel 4 (NOVO): Otimização de Entrega de Imagem & Frametime
        private Panel _cardFrameOpt;
        private CheckBox _chkSmartLowPrioPurge;
        private CheckBox _chkFlushModified;
        private CheckBox _chkAutoBoostFg;
        private Label _lblBoostedProcessVal;
        private CheckBox _chkMmcssGpuTweaks;
        private CheckBox _chkUltimatePowerPlan;
        private Label _lblActivePowerPlanVal;

        // Painel 5: Opções Gerais & Atalho Global
        private Panel _cardOptions;
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
        private ToolStripMenuItem _trayItemToggle;
        private Icon _appIcon;
        private bool _suppressEvents = false;

        public MainForm(string[] args)
        {
            _config = AppConfig.Load();
            ApplyCommandLineOverrides(args);

            InitializeComponent();
            LoadConfigToUI();
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

            _lblActivePowerPlanVal.Text = "Plano Ativo: " + NativeMethods.GetActivePowerPlanName();

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
            Text = "Custom ISLC Pro - Otimizador de Memória, GPU & Entrega de Quadros" + (isAdmin ? " [Administrador]" : " [Sem Privilégio Admin]");
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
            Label lblMemTitle = CreateSectionTitle("STATUS DA MEMÓRIA DO SISTEMA EM TEMPO REAL", 14, 10);
            _lblStatusBadge = new Label
            {
                Text = "● PARADO",
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

            _lblTotalMemValue = CreateMetricBox(_cardMemory, "Memória Total", "0 MB", 14, 72, Color.FromArgb(148, 163, 184), null);
            _lblStandbyMemValue = CreateMetricBox(_cardMemory, "Standby List (Cache)", "0 MB", 168, 72, Color.FromArgb(56, 189, 248), out _lblStandbySubDetail);
            _lblFreeMemValue = CreateMetricBox(_cardMemory, "Memória Livre", "0 MB", 322, 72, Color.FromArgb(74, 222, 128), null);
            _lblModifiedMemValue = CreateMetricBox(_cardMemory, "Páginas Modificadas", "0 MB", 476, 72, Color.FromArgb(251, 191, 36), null);
            _lblPurgeCountValue = CreateMetricBox(_cardMemory, "Limpezas Realizadas", "0", 630, 72, Color.FromArgb(192, 132, 252), null);

            _cardMemory.Controls.Add(lblMemTitle);
            _cardMemory.Controls.Add(_lblStatusBadge);
            _cardMemory.Controls.Add(_memoryBar);

            // =====================================================================
            // PAINEL 2: REGRAS DE LIMPEZA AUTOMÁTICA
            // =====================================================================
            _cardConfig = CreateCard(16, 186, 385, 165);
            _cardConfig.Controls.Add(CreateSectionTitle("REGRAS DE LIMPEZA AUTOMÁTICA", 14, 10));

            _cardConfig.Controls.Add(CreateLabel("Standby List maior ou igual a:", 14, 40));
            _numStandbyLimit = CreateNumeric(235, 37, 95, 64, 131072, 1024);
            _cardConfig.Controls.Add(_numStandbyLimit);
            _cardConfig.Controls.Add(CreateLabel("MB", 335, 40));

            _cardConfig.Controls.Add(CreateLabel("Condição lógica de disparo:", 14, 70));
            _cmbCondition = new ComboBox
            {
                Location = new Point(235, 67),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCondition.Items.AddRange(new object[] { "E (Ambos)", "OU (Qualquer um)" });
            _cmbCondition.SelectedIndex = 0;
            _cardConfig.Controls.Add(_cmbCondition);

            _cardConfig.Controls.Add(CreateLabel("Memória Livre menor que:", 14, 100));
            _numFreeMemLimit = CreateNumeric(235, 97, 95, 64, 131072, 8000);
            _cardConfig.Controls.Add(_numFreeMemLimit);
            _cardConfig.Controls.Add(CreateLabel("MB", 335, 100));

            _cardConfig.Controls.Add(CreateLabel("Taxa de verificação (Polling):", 14, 130));
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
            _cardTimer.Controls.Add(CreateSectionTitle("RESOLUÇÃO DO TIMER DO WINDOWS", 14, 10));

            _cardTimer.Controls.Add(CreateLabel("Resolução Atual do Timer:", 14, 40));
            _lblCurrentTimerVal = new Label
            {
                Text = "1.00 ms",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                AutoSize = true,
                Location = new Point(235, 39)
            };
            _cardTimer.Controls.Add(_lblCurrentTimerVal);

            _cardTimer.Controls.Add(CreateLabel("Limites do Sistema (Min/Máx):", 14, 68));
            _lblMinMaxTimerVal = CreateLabel("15.62 ms / 0.50 ms", 235, 68);
            _cardTimer.Controls.Add(_lblMinMaxTimerVal);

            _chkCustomTimer = new CheckBox
            {
                Text = "Ativar Resolução de Timer Customizada",
                AutoSize = true,
                Location = new Point(14, 98)
            };
            _chkCustomTimer.CheckedChanged += OnCustomTimerChanged;
            _cardTimer.Controls.Add(_chkCustomTimer);

            _cardTimer.Controls.Add(CreateLabel("Resolução Desejada (ms):", 14, 128));
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
            // PAINEL 4 (NOVO): OTIMIZAÇÃO DE ENTREGA DE IMAGEM & FRAMETIME
            // =====================================================================
            _cardFrameOpt = CreateCard(16, 360, 782, 122);
            _cardFrameOpt.Controls.Add(CreateSectionTitle("OTIMIZAÇÃO DE ENTREGA DE IMAGEM, FRAMETIME & PRIORIDADE DE GPU", 14, 10));

            _chkSmartLowPrioPurge = CreateCheckBox("Limpeza Standby Inteligente (Preserva cache de alta prioridade do jogo para evitar micro-stutter)", 14, 34);
            _chkFlushModified = CreateCheckBox("Descarregar páginas modificadas antes de limpar (Flush Modified)", 14, 60);

            _chkAutoBoostFg = CreateCheckBox("Auto-Boost Janela em Foco (CPU High + I/O + Sem Power Throttling):", 14, 86);
            _lblBoostedProcessVal = new Label
            {
                Text = "Aguardando foco...",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                AutoSize = true,
                Location = new Point(422, 87)
            };

            _chkMmcssGpuTweaks = new CheckBox
            {
                Text = "Otimizar Agendador GPU (MMCSS Priority 8 + Responsividade 100% + Anti-GameDVR)",
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
                Text = "Forçar Energia Desempenho Máximo (Sem Core Parking)",
                AutoSize = true,
                Location = new Point(415, 60)
            };
            _chkUltimatePowerPlan.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                NativeMethods.SetHighPerformancePowerPlan(_chkUltimatePowerPlan.Checked);
                _lblActivePowerPlanVal.Text = "Plano Ativo: " + NativeMethods.GetActivePowerPlanName();
                SaveUIConfig();
            };

            _lblActivePowerPlanVal = new Label
            {
                Text = "Plano Ativo: Verificando...",
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
            _cardOptions.Controls.Add(CreateSectionTitle("OPÇÕES GERAIS & ATALHO GLOBAL", 14, 10));

            _chkStartMinimized = CreateCheckBox("Iniciar minimizado na bandeja", 14, 36);
            _chkAutoStartMonitoring = CreateCheckBox("Auto-iniciar monitoramento", 205, 36);
            _chkMinimizeOnClose = CreateCheckBox("Minimizar ao fechar (X)", 14, 62);
            _chkAlwaysOnTop = CreateCheckBox("Sempre no topo", 205, 62);
            _chkStartWithWindows = CreateCheckBox("Iniciar com o Windows (Admin)", 14, 88);
            _chkDarkMode = CreateCheckBox("Modo Escuro", 235, 88);

            _chkAlwaysOnTop.CheckedChanged += (s, e) => { if (!_suppressEvents) { TopMost = _chkAlwaysOnTop.Checked; SaveUIConfig(); } };
            _chkDarkMode.CheckedChanged += (s, e) => { if (!_suppressEvents) { ApplyTheme(_chkDarkMode.Checked); SaveUIConfig(); } };
            _chkStartWithWindows.CheckedChanged += OnStartWithWindowsChanged;

            _chkHotkeyEnabled = new CheckBox
            {
                Text = "Atalho Limpar:",
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
            _cardExclusions.Controls.Add(CreateSectionTitle("WORKING SET & LISTA DE EXCLUSÃO", 14, 10));

            _chkPurgeWorkingSetAuto = CreateCheckBox("Limpar Working Set de fundo junto com Standby (ignora jogo ativo)", 14, 34);
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
                Text = "nome_do_jogo"
            };
            _cardExclusions.Controls.Add(_txtExclusionProc);

            _btnAddExclusion = new Button
            {
                Text = "+ Ignorar Processo",
                Location = new Point(232, 86),
                Size = new Size(138, 24),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAddExclusion.Click += OnAddExclusionClick;
            _cardExclusions.Controls.Add(_btnAddExclusion);

            _btnRemoveExclusion = new Button
            {
                Text = "- Remover Selecionado",
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
                Text = "▶ INICIAR MONITORAMENTO",
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
                Text = "⚡ LIMPAR STANDBY LIST AGORA",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(298, 648),
                Size = new Size(255, 42),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnPurgeStandbyNow.Click += (s, e) => ExecutePurge(true, false);

            _btnPurgeWorkingSetNow = new Button
            {
                Text = "🧹 LIMPAR WORKING SETS AGORA",
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
            ToolStripMenuItem itemOpen = new ToolStripMenuItem("Abrir Painel Custom ISLC Pro", null, (s, e) => RestoreFromTray());
            itemOpen.Font = new Font(itemOpen.Font, FontStyle.Bold);
            _trayItemToggle = new ToolStripMenuItem("Iniciar Monitoramento", null, (s, e) =>
            {
                if (_isMonitoring) StopMonitoring();
                else StartMonitoring();
            });
            ToolStripMenuItem itemPurgeStandby = new ToolStripMenuItem("Limpar Standby List Agora", null, (s, e) => ExecutePurge(true, false));
            ToolStripMenuItem itemPurgeWS = new ToolStripMenuItem("Limpar Working Sets Agora", null, (s, e) => ExecutePurge(false, true));
            ToolStripMenuItem itemExit = new ToolStripMenuItem("Sair", null, (s, e) => ExitApplication());

            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                itemOpen,
                new ToolStripSeparator(),
                _trayItemToggle,
                itemPurgeStandby,
                itemPurgeWS,
                new ToolStripSeparator(),
                itemExit
            });

            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Text = "Custom ISLC Pro - Otimizador de Memória & Frametime",
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

        private Label CreateMetricBox(Panel parent, string caption, string initialValue, int x, int y, Color valueColor, out Label subDetailLabel)
        {
            Panel box = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(140, 80),
                Tag = "metricBox"
            };

            Label lblCap = new Label
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

            box.Controls.Add(lblCap);
            box.Controls.Add(lblVal);
            box.Controls.Add(subDetailLabel);
            parent.Controls.Add(box);
            return lblVal;
        }

        private Label CreateMetricBox(Panel parent, string caption, string initialValue, int x, int y, Color valueColor, object unused)
        {
            Label dummy;
            return CreateMetricBox(parent, caption, initialValue, x, y, valueColor, out dummy);
        }

        private void LoadConfigToUI()
        {
            _suppressEvents = true;
            try
            {
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
            _lblStatusBadge.Text = "● MONITORANDO ATIVO";
            _lblStatusBadge.ForeColor = Color.FromArgb(74, 222, 128);
            _btnToggleMonitor.Text = "⏹ PARAR MONITORAMENTO";
            _btnToggleMonitor.BackColor = Color.FromArgb(220, 38, 38);
            _btnToggleMonitor.ForeColor = Color.White;
            _trayItemToggle.Text = "Parar Monitoramento";
            _monitorTimer.Start();
            UpdateMemoryStats(true);
        }

        private void StopMonitoring()
        {
            _isMonitoring = false;
            _numStandbyLimit.Enabled = true;
            _numFreeMemLimit.Enabled = true;
            _cmbCondition.Enabled = true;
            _lblStatusBadge.Text = "● MONITORAMENTO PARADO";
            _lblStatusBadge.ForeColor = Color.FromArgb(248, 113, 113);
            _btnToggleMonitor.Text = "▶ INICIAR MONITORAMENTO";
            _btnToggleMonitor.BackColor = Color.FromArgb(16, 185, 129);
            _btnToggleMonitor.ForeColor = Color.White;
            _trayItemToggle.Text = "Iniciar Monitoramento";
            NativeMethods.RestoreLastBoostedProcess();
            _lblBoostedProcessVal.Text = "Parado";
            _monitorTimer.Start();
        }

        private void UpdateMemoryStats(bool checkAutoPurge)
        {
            NativeMethods.MemorySnapshot snap = NativeMethods.GetMemorySnapshot();

            _lblTotalMemValue.Text = string.Format("{0:N0} MB", snap.TotalPhysicalMB);
            _lblStandbyMemValue.Text = string.Format("{0:N0} MB", snap.StandbyListMB);
            if (_lblStandbySubDetail != null)
            {
                _lblStandbySubDetail.Text = string.Format("Frio: {0}M | Quente: {1}M", snap.LowPriorityStandbyMB, snap.HighPriorityStandbyMB);
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
                _lblBoostedProcessVal.Text = boosted;
            }

            string trayTooltip = string.Format("Custom ISLC Pro ({0})\nStandby: {1} MB | Livre: {2} MB\nLimpezas: {3}",
                _isMonitoring ? "Ativo" : "Parado", snap.StandbyListMB, snap.FreeMemoryMB, _purgeCount);
            if (trayTooltip.Length > 63)
                trayTooltip = trayTooltip.Substring(0, 63);
            _trayIcon.Text = trayTooltip;

            if (checkAutoPurge && _isMonitoring)
            {
                ulong standbyLimit = (ulong)_numStandbyLimit.Value;
                ulong freeLimit = (ulong)_numFreeMemLimit.Value;
                bool useOr = (_cmbCondition.SelectedIndex == 1);

                // Se limpeza inteligente de baixa prioridade estiver ativa, verifica se há pelo menos 64 MB de standby baixa prioridade
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

        public MemoryBarControl()
        {
            DoubleBuffered = true;
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

            string legend = string.Format("Em Uso: {0:N0} MB   |   Standby Cache: {1:N0} MB   |   Livre: {2:N0} MB", _used, _standby, _free);
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
