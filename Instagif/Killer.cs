using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Instagif
{
    static class Killer
    {
        public static void KillOthers()
        {
            try
            {
                var pid = Process.GetCurrentProcess().Id;
                var proceses = Process.GetProcesses();
                foreach (Process p in proceses)
                {
                    if (p.ProcessName.ToLower().StartsWith("instagif"))
                    {
                        if (p.Id != pid)
                        {
                            p.Kill();
                        }
                    }
                }
            }
            catch { }
        }
    }
}
