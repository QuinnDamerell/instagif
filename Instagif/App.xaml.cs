using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Instagif
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Hooker m_hooker;
        GifSelector m_selector;

        public App()
        {
            // keep us alive after the main window closes.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Clippy.Test();

            // Setup the keyboard hooker
            m_hooker = new Hooker();
            m_hooker.GlobalHotKeyInvoked += Hooker_GlobalHotKeyInvoked;

            // Kill any other processes
            Killer.KillOthers();

#if DEBUG
            OpenWindow();
#else
            // Setup to start on run.
            Starter.SetStartup();

            // Set the keyboard hook
            m_hooker.SetHook();
#endif
        }

        private void Hooker_GlobalHotKeyInvoked()
        {
            if(m_selector != null && (m_selector.Visibility != Visibility.Visible || !m_selector.IsActive))
            {
                m_selector.Close();
                m_selector = null;
            }
            if(m_selector == null)
            {
                OpenWindow();
            }
            else
            {
                m_selector.Close();
                m_selector = null;
            }
        }

        private void OpenWindow()
        {
            m_selector = new GifSelector();
            m_selector.ShowInTaskbar = false;
            m_selector.Show();
            m_selector.Activate();
            m_selector.Focus();
            m_selector.Topmost = true;
        }
    }
}
