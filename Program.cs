using System;
using System.Threading;
using System.Windows.Forms;

namespace CustomISLC
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i].ToLowerInvariant();
                    if (a == "-purge")
                    {
                        NativeMethods.PurgeStandbyList(false);
                        return;
                    }
                    if (a == "-runonce")
                    {
                        AppConfig cfg = AppConfig.Load();
                        NativeMethods.MemorySnapshot snap = NativeMethods.GetMemorySnapshot();
                        ulong standbyLimit = 1024;
                        ulong freeLimit = 8000;
                        ulong.TryParse(cfg.StandbyListValue, out standbyLimit);
                        ulong.TryParse(cfg.FreeMemoryValue, out freeLimit);
                        if (snap.StandbyListMB >= standbyLimit && snap.FreeMemoryMB < freeLimit)
                        {
                            NativeMethods.PurgeStandbyList(false);
                        }
                        return;
                    }
                }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "Global\\CustomISLC_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("O Custom ISLC já está em execução (verifique a bandeja do sistema).",
                        "Custom ISLC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(args));
            }
        }
    }
}
