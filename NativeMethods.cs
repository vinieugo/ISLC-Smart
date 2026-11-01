using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CustomISLC
{
    public static class NativeMethods
    {
        public const int SystemMemoryListInformation = 80;
        public const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        public const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        public const string SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;

        public const int WM_HOTKEY = 0x0312;

        // Process Information Classes
        private const int ProcessIoPriority = 33;
        private const int ProcessPagePriority = 39;
        private const int ProcessPowerThrottling = 4;
        private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;

        public enum SYSTEM_MEMORY_LIST_COMMAND : int
        {
            MemoryCaptureAccessedBits = 0,
            MemoryCaptureAndResetAccessedBits = 1,
            MemoryEmptyWorkingSets = 2,
            MemoryFlushModifiedList = 3,
            MemoryPurgeStandbyList = 4,
            MemoryPurgeLowPriorityStandbyList = 5,
            MemoryCommandMax = 6
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_MEMORY_LIST_INFORMATION
        {
            public UIntPtr ZeroPageCount;
            public UIntPtr FreePageCount;
            public UIntPtr ModifiedPageCount;
            public UIntPtr ModifiedNoWritePageCount;
            public UIntPtr BadPageCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public UIntPtr[] PageCountByPriority;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public UIntPtr[] RepurposedPagesByPriority;
            public UIntPtr ModifiedPageCountPageFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public void Init()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtSetSystemInformation(int InfoClass, ref int Info, int Length);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtQuerySystemInformation(int InfoClass, IntPtr Info, int Length, out int ReturnLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtSetInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, ref int ProcessInformation, int ProcessInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessInformation(IntPtr hProcess, int ProcessInformationClass, ref PROCESS_POWER_THROTTLING_STATE ProcessInformation, int ProcessInformationSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static bool _privilegesInitialized = false;
        private static int _lastBoostedPid = -1;
        private static string _lastBoostedName = "Nenhum";

        private static readonly HashSet<string> _systemProcessesToIgnore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer", "dwm", "SearchHost", "SearchApp", "ShellExperienceHost",
            "StartMenuExperienceHost", "ApplicationFrameHost", "Taskmgr", "cmd",
            "powershell", "pwsh", "conhost", "WindowsTerminal", "CustomISLC",
            "svchost", "csrss", "winlogon", "fontdrvhost", "sihost", "ctfmon",
            "TextInputHost", "LockApp", "SystemSettings"
        };

        public static void EnsurePrivileges()
        {
            if (!_privilegesInitialized)
            {
                EnablePrivilege(SE_PROFILE_SINGLE_PROCESS_NAME);
                EnablePrivilege(SE_INCREASE_QUOTA_NAME);
                EnablePrivilege(SE_INC_BASE_PRIORITY_NAME);
                _privilegesInitialized = true;
            }
        }

        public static bool EnablePrivilege(string privilegeName)
        {
            IntPtr tokenHandle = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
                    return false;

                long luid;
                if (!LookupPrivilegeValue(null, privilegeName, out luid))
                    return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                return AdjustTokenPrivileges(tokenHandle, false, ref tp, Marshal.SizeOf(tp), IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                    CloseHandle(tokenHandle);
            }
        }

        public static bool PurgeStandbyList(bool lowPriorityOnly)
        {
            EnsurePrivileges();
            int command = (int)(lowPriorityOnly
                ? SYSTEM_MEMORY_LIST_COMMAND.MemoryPurgeLowPriorityStandbyList
                : SYSTEM_MEMORY_LIST_COMMAND.MemoryPurgeStandbyList);
            uint status = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));
            return status == 0;
        }

        public static bool FlushModifiedList()
        {
            EnsurePrivileges();
            int command = (int)SYSTEM_MEMORY_LIST_COMMAND.MemoryFlushModifiedList;
            uint status = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));
            return status == 0;
        }

        public static int EmptyProcessWorkingSets(IEnumerable<string> exclusionList)
        {
            EnsurePrivileges();

            HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (exclusionList != null)
            {
                foreach (string item in exclusionList)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        string clean = item.Trim();
                        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            clean = clean.Substring(0, clean.Length - 4);
                        excluded.Add(clean);
                    }
                }
            }

            // Também protege o processo em foco (jogo atual) de ter o Working Set esvaziado!
            uint fgPid = 0;
            IntPtr fgHwnd = GetForegroundWindow();
            if (fgHwnd != IntPtr.Zero)
                GetWindowThreadProcessId(fgHwnd, out fgPid);

            int count = 0;
            int currentPid = Process.GetCurrentProcess().Id;
            Process[] processes = Process.GetProcesses();
            foreach (Process proc in processes)
            {
                try
                {
                    if (proc.Id <= 4 || proc.Id == currentPid || proc.Id == (int)fgPid)
                        continue;

                    if (excluded.Contains(proc.ProcessName))
                        continue;

                    if (EmptyWorkingSet(proc.Handle))
                        count++;
                }
                catch
                {
                }
                finally
                {
                    proc.Dispose();
                }
            }
            return count;
        }

        public struct MemorySnapshot
        {
            public ulong TotalPhysicalMB;
            public ulong StandbyListMB;
            public ulong LowPriorityStandbyMB;
            public ulong HighPriorityStandbyMB;
            public ulong FreeMemoryMB;
            public ulong AvailableMemoryMB;
            public ulong ModifiedMemoryMB;
            public uint MemoryLoadPercent;
        }

        public static MemorySnapshot GetMemorySnapshot()
        {
            EnsurePrivileges();
            MemorySnapshot snap = new MemorySnapshot();

            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.Init();
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                snap.TotalPhysicalMB = memStatus.ullTotalPhys / (1024 * 1024);
                snap.AvailableMemoryMB = memStatus.ullAvailPhys / (1024 * 1024);
                snap.MemoryLoadPercent = memStatus.dwMemoryLoad;
            }

            int size = Marshal.SizeOf(typeof(SYSTEM_MEMORY_LIST_INFORMATION));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                int returnLength;
                uint status = NtQuerySystemInformation(SystemMemoryListInformation, ptr, size, out returnLength);
                if (status == 0)
                {
                    SYSTEM_MEMORY_LIST_INFORMATION info = (SYSTEM_MEMORY_LIST_INFORMATION)Marshal.PtrToStructure(ptr, typeof(SYSTEM_MEMORY_LIST_INFORMATION));
                    ulong pageSize = 4096UL;

                    ulong freePages = info.FreePageCount.ToUInt64() + info.ZeroPageCount.ToUInt64();
                    ulong standbyPages = 0;
                    ulong lowPrioPages = 0;
                    ulong highPrioPages = 0;

                    if (info.PageCountByPriority != null)
                    {
                        for (int i = 0; i < info.PageCountByPriority.Length; i++)
                        {
                            ulong p = info.PageCountByPriority[i].ToUInt64();
                            standbyPages += p;
                            if (i <= 4)
                                lowPrioPages += p;
                            else
                                highPrioPages += p;
                        }
                    }
                    ulong modifiedPages = info.ModifiedPageCount.ToUInt64() + info.ModifiedNoWritePageCount.ToUInt64();

                    snap.FreeMemoryMB = (freePages * pageSize) / (1024 * 1024);
                    snap.StandbyListMB = (standbyPages * pageSize) / (1024 * 1024);
                    snap.LowPriorityStandbyMB = (lowPrioPages * pageSize) / (1024 * 1024);
                    snap.HighPriorityStandbyMB = (highPrioPages * pageSize) / (1024 * 1024);
                    snap.ModifiedMemoryMB = (modifiedPages * pageSize) / (1024 * 1024);
                    return snap;
                }
            }
            catch
            {
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            snap.FreeMemoryMB = snap.AvailableMemoryMB;
            return snap;
        }

        public static void GetTimerResolution(out double minMs, out double maxMs, out double currentMs)
        {
            uint min, max, cur;
            if (NtQueryTimerResolution(out min, out max, out cur) == 0)
            {
                minMs = min / 10000.0;
                maxMs = max / 10000.0;
                currentMs = cur / 10000.0;
            }
            else
            {
                minMs = 15.625;
                maxMs = 0.5;
                currentMs = 1.0;
            }
        }

        public static double SetTimerResolution(double desiredMs, bool enable)
        {
            uint desired100Ns = (uint)Math.Round(desiredMs * 10000.0);
            uint current100Ns;
            NtSetTimerResolution(desired100Ns, enable, out current100Ns);
            return current100Ns / 10000.0;
        }

        // =========================================================================
        // OTIMIZAÇÃO DE ENTREGA DE IMAGEM: AUTO-BOOST DO PROCESSO EM FOCO
        // =========================================================================
        public static string UpdateForegroundProcessBoost(bool enabled)
        {
            if (!enabled)
            {
                RestoreLastBoostedProcess();
                return "Desativado";
            }

            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return _lastBoostedName;

                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid <= 4)
                    return _lastBoostedName;

                if ((int)pid == _lastBoostedPid)
                    return _lastBoostedName;

                using (Process proc = Process.GetProcessById((int)pid))
                {
                    string procName = proc.ProcessName;
                    if (_systemProcessesToIgnore.Contains(procName))
                    {
                        return _lastBoostedName;
                    }

                    // Restaura o processo anterior antes de impulsionar o novo
                    RestoreLastBoostedProcess();

                    EnsurePrivileges();

                    // 1. Prioridade de CPU Alta e PriorityBoost ativado
                    proc.PriorityBoostEnabled = true;
                    if (proc.PriorityClass == ProcessPriorityClass.Normal || proc.PriorityClass == ProcessPriorityClass.BelowNormal)
                    {
                        proc.PriorityClass = ProcessPriorityClass.High;
                    }

                    // 2. Desativar Power Throttling (EcoQoS) para evitar queda de clock no meio do frame
                    PROCESS_POWER_THROTTLING_STATE powerState = new PROCESS_POWER_THROTTLING_STATE
                    {
                        Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                        ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                        StateMask = 0 // 0 = Desativa estrangulamento de velocidade
                    };
                    SetProcessInformation(proc.Handle, ProcessPowerThrottling, ref powerState, Marshal.SizeOf(powerState));

                    // 3. Prioridade de Página de Memória (5 = Normal/Máxima para apps) e I/O (2 = Normal/Alta)
                    int pagePrio = 5;
                    NtSetInformationProcess(proc.Handle, ProcessPagePriority, ref pagePrio, sizeof(int));

                    int ioPrio = 2;
                    NtSetInformationProcess(proc.Handle, ProcessIoPriority, ref ioPrio, sizeof(int));

                    _lastBoostedPid = (int)pid;
                    _lastBoostedName = string.Format("{0}.exe (PID {1})", procName, pid);
                }
            }
            catch
            {
            }

            return _lastBoostedName;
        }

        public static void RestoreLastBoostedProcess()
        {
            if (_lastBoostedPid > 4)
            {
                try
                {
                    using (Process prev = Process.GetProcessById(_lastBoostedPid))
                    {
                        if (prev.PriorityClass == ProcessPriorityClass.High)
                        {
                            prev.PriorityClass = ProcessPriorityClass.Normal;
                        }
                    }
                }
                catch
                {
                }
                _lastBoostedPid = -1;
                _lastBoostedName = "Nenhum";
            }
        }

        // =========================================================================
        // OTIMIZAÇÃO DO AGENDADOR MULTIMÍDIA / GPU (MMCSS) E GAMEDVR
        // =========================================================================
        public static bool ApplyFrameDeliverySystemTweaks(bool enable)
        {
            try
            {
                using (RegistryKey sysProfile = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (sysProfile != null)
                    {
                        // 0 = 100% de responsividade para entrega de quadros em tempo real (padrão Windows = 20)
                        sysProfile.SetValue("SystemResponsiveness", enable ? 0 : 20, RegistryValueKind.DWord);
                        // 0xFFFFFFFF = Desativa estrangulamento de pacotes multimídia/rede
                        sysProfile.SetValue("NetworkThrottlingIndex", enable ? unchecked((int)0xFFFFFFFF) : 10, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey gamesTask = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"))
                {
                    if (gamesTask != null)
                    {
                        gamesTask.SetValue("GPU Priority", enable ? 8 : 2, RegistryValueKind.DWord);
                        gamesTask.SetValue("Priority", enable ? 6 : 2, RegistryValueKind.DWord);
                        gamesTask.SetValue("Scheduling Category", enable ? "High" : "Medium", RegistryValueKind.String);
                        gamesTask.SetValue("SFIO Priority", enable ? "High" : "Normal", RegistryValueKind.String);
                    }
                }

                // Desativar GameDVR / Captura de Fundo que causa engasgos no swapchain DWM
                using (RegistryKey gameConfig = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
                {
                    if (gameConfig != null)
                    {
                        gameConfig.SetValue("GameDVR_Enabled", enable ? 0 : 1, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey gameDvr = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"))
                {
                    if (gameDvr != null)
                    {
                        gameDvr.SetValue("AppCaptureEnabled", enable ? 0 : 1, RegistryValueKind.DWord);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool AreFrameDeliveryTweaksActive()
        {
            try
            {
                using (RegistryKey sysProfile = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (sysProfile != null)
                    {
                        object val = sysProfile.GetValue("SystemResponsiveness");
                        if (val != null && Convert.ToInt32(val) == 0)
                            return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        // =========================================================================
        // PLANO DE ENERGIA DESEMPENHO MÁXIMO (SEM CORE PARKING)
        // =========================================================================
        public static bool SetHighPerformancePowerPlan(bool enable)
        {
            try
            {
                // GUIDs oficiais do Windows:
                // Ultimate Performance: e9a42b02-d5df-448d-aa00-03f14749eb61
                // High Performance:     8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
                // Balanced:             381b4222-f694-41f0-9685-ff5bb260df2e
                if (enable)
                {
                    string listOutput = RunCommandOutput("powercfg.exe", "/list");
                    string targetGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"; // High Performance

                    if (listOutput.IndexOf("e9a42b02-d5df-448d-aa00-03f14749eb61", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
                    }
                    else
                    {
                        // Tenta habilitar o plano Ultimate Performance nativo do Windows
                        string dupOut = RunCommandOutput("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                        if (!string.IsNullOrEmpty(dupOut))
                        {
                            // Extrai GUID criado caso tenha sucesso
                            int idx = dupOut.IndexOf(":");
                            if (idx >= 0 && dupOut.Length >= idx + 38)
                            {
                                string candidate = dupOut.Substring(idx + 1).Trim().Split(' ')[0].Trim();
                                if (candidate.Length == 36)
                                    targetGuid = candidate;
                            }
                        }
                    }

                    RunCommandOutput("powercfg.exe", "/setactive " + targetGuid);
                    return true;
                }
                else
                {
                    RunCommandOutput("powercfg.exe", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetActivePowerPlanName()
        {
            try
            {
                string output = RunCommandOutput("powercfg.exe", "/getactivescheme");
                if (!string.IsNullOrEmpty(output))
                {
                    int openParen = output.LastIndexOf('(');
                    int closeParen = output.LastIndexOf(')');
                    if (openParen >= 0 && closeParen > openParen)
                    {
                        return output.Substring(openParen + 1, closeParen - openParen - 1).Trim();
                    }
                }
            }
            catch
            {
            }
            return "Padrão";
        }

        private static string RunCommandOutput(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(2500);
                    return output;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
