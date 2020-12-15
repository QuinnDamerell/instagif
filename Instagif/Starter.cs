using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Instagif
{
    class Starter
    {
        const string c_appName = "Instagif";

        public static void SetStartup()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if(path.ToLower().EndsWith(".dll"))
            {
                // Remove the .dll
                path = path.Substring(0, path.Length - 4);
                // Add .exe
                path = path + ".exe";
            }          

            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            rk.SetValue(c_appName, path);        
        }
    }
}
