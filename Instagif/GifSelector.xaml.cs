using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using GiphyDotNet.Model.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace Instagif
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GifSelector
    {
        const int c_closedWindowHeight = 85;
        const int c_openWindowHeight = 500;

        Giphy m_giphy = new Giphy("Z5X6hHAlnH79Bv2P4RkUHHaL68l9UMHX");

        object m_resultLock = new object();
        GiphySearchResult m_currentResults = null;
        int m_currentResultShown = 0;

        List<string> c_placeHolder = new List<string>()
        {
            "So Forth And Search",
            "What Are You Looking For?",
            "The Power Of The Internet Is At Your Fingers..."
        };

        public GifSelector()
        {
            InitializeComponent();

            // Set the random place holder text.
            ui_search.Tag = c_placeHolder[(new Random().Next(c_placeHolder.Count - 1))];

            // Setup style
            AcrylicWindowStyle = SourceChord.FluentWPF.AcrylicWindowStyle.None;
            TintColor = Color.FromRgb(80, 80, 80);
            TintOpacity = 0.2;

            // Add event listeners
            Loaded += GifSelector_Loaded;
            Deactivated += GifSelector_Deactivated;
        }

        private void GifSelector_Deactivated(object sender, EventArgs e)
        {
            // When we lose focus close, don't bother if this throws.
            try
            {
#if DEBUG
#else
                Close();
#endif
            }
            catch
            {  }
        }

        private void GifSelector_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the search box and center the windwo.
            ui_search.Focus();

            CenterWindowOnScreen();
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = c_openWindowHeight;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            QueueSearch(ui_search.Text);
        }

        private void Search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                // On enter, copy to the clipboard and move on.
                GiphySearchResult local = null;
                lock(m_resultLock)
                {
                    local = m_currentResults;
                }
                if (local != null && local.Data.Length > 0)
                {
                    Clippy.SetClipboardContent(local.Data[m_currentResultShown]);
                }
                Close();
            }
            if(e.Key == Key.Right || e.Key == Key.Down)
            {
                ShowNextImage(true);
                e.Handled = true;
            }
            if (e.Key == Key.Left || e.Key == Key.Up)
            {
                ShowNextImage(false);
                e.Handled = true;
            }
        }

        int m_currentSearchIndex = 0;
        System.Timers.Timer m_searchDelay = null;
        DateTime m_lastSearchTime = DateTime.Now;

        void QueueSearch(string query)
        {
            // If there is a running timer, stop it.
            if(m_searchDelay != null)
            {
                m_searchDelay.Stop();
                m_searchDelay = null;
            }

            // Ensure we query sometimes, even if they keep typing.
            if(DateTime.Now - m_lastSearchTime > TimeSpan.FromMilliseconds(500))
            {
                DoSearch(query);
                return;
            }

            // Start a delay timer, so we search 300ms after they are done typing.
            m_searchDelay = new System.Timers.Timer();
            m_searchDelay.Interval = 200;
            m_searchDelay.Elapsed += (sender, args) =>
            {
                // Stop our timer
                m_searchDelay.Stop();

                // Do the query.
                DoSearch(query);
            };
            m_searchDelay.Start();
        }

        bool m_hasInitalQuery = false;
        void DoSearch(string query)
        {
            // Set current time as the last time we searched
            m_lastSearchTime = DateTime.Now;

            // Clean up the query
            query = query.Trim();

            // If we don't have anything close the results.
            if(String.IsNullOrWhiteSpace(query))
            {
                if (m_hasInitalQuery)
                {
                    ShowError("Go Forth And Search!");
                }
                return;
            }

            m_hasInitalQuery = true;
            m_currentSearchIndex++;
            int localIndex = m_currentSearchIndex;

            Thread t = new Thread(async ()=>{
                try
                {
                    // Run the web query.
                    var searchParameter = new SearchParameter()
                    {
                        Query = query
                    };
                    var gifResult = await m_giphy.GifSearch(searchParameter);

                    // If we didn't get results, show it.
                    if(gifResult.Data.Length == 0)
                    {
                        ShowError("No Results");
                        return;
                    }

                    // Jump back to the UI thread.
                    await Dispatcher.BeginInvoke(new Action(() => {

                        // Ensure we are still the current search.
                        if (localIndex == m_currentSearchIndex)
                        {
                            // Set the results.
                            lock(m_resultLock)
                            {
                                m_currentResults = gifResult;
                                m_currentResultShown = 0;
                            }

                            // Hide any error UI.
                            HideIfVisible(ui_error);

                            // Show the first result.
                            ShowResult(gifResult.Data[0]);

                            // Ensure the results are showing.
                            EnsureWindowOpened();
                        }
                    }));                
                }
                catch(Exception e)
                {
                    ShowError("Failed to query Giphy "+e.Message);
                }
            });
            t.Start();
        }

        private void ShowResult(GiphyDotNet.Model.GiphyImage.Data data)
        {
            // Get the gif into the media element.
            ui_result.Source = new Uri(data.Images.Original.Mp4);

            // Fade it in.
            FadeIn(ui_result);
        }

        private void ShowNextImage(bool forward)
        {
            GiphyDotNet.Model.GiphyImage.Data data = null;
            lock (m_resultLock)
            {
                if(m_currentResults == null)
                {
                    return;
                }
                if (forward)
                {
                    if (m_currentResultShown + 1 >= m_currentResults.Data.Length)
                    {
                        return;
                    }
                    m_currentResultShown++;
                }
                else
                {
                    if(m_currentResultShown -1 < 0)
                    {
                        return;
                    }
                    m_currentResultShown--;
                }
                data = m_currentResults.Data[m_currentResultShown];
            }
            if (data != null)
            {
                ShowResult(data);
            }
        }

        private async void ShowError(string error)
        {
            await Dispatcher.BeginInvoke(new Action(() => {
                EnsureWindowOpened();
                ui_error.Text = error;
                ui_error.Visibility = Visibility.Visible;
                HideIfVisible(ui_result);
                FadeIn(ui_error);
            }));    
        }

        bool m_isWindowOpened = false;
        Storyboard m_currentWindowAnimation = null;

        private void EnsureWindowOpened()
        {
            if(m_isWindowOpened)
            {
                return;
            }
            OpenOrCloseWindowResults();
        }

        private void EnsureWindowClosed()
        {
            if (!m_isWindowOpened)
            {
                return;
            }
            OpenOrCloseWindowResults();
        }

        private void OpenOrCloseWindowResults()
        {
            try
            {
                if (m_currentWindowAnimation != null && m_currentWindowAnimation.GetCurrentState() != ClockState.Stopped)
                {
                    m_currentWindowAnimation.Stop();
                }
            }
            catch { }

            var open = new DoubleAnimation()
            {
                From = m_isWindowOpened ? c_openWindowHeight : c_closedWindowHeight,
                To = m_isWindowOpened ? c_closedWindowHeight : c_openWindowHeight,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };
            m_isWindowOpened = !m_isWindowOpened;
            Storyboard.SetTarget(open, this);
            Storyboard.SetTargetProperty(open, new PropertyPath(Window.HeightProperty));
            m_currentWindowAnimation = new Storyboard();
            m_currentWindowAnimation.Children.Add(open);
            m_currentWindowAnimation.Begin();
        }

        private void FadeIn(FrameworkElement obj)
        {
            var open = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(open, obj);
            Storyboard.SetTargetProperty(open, new PropertyPath(FrameworkElement.OpacityProperty));
            var sb = new Storyboard();
            sb.Children.Add(open);
            obj.Visibility = Visibility.Visible;
            sb.Begin();
        }

        private void HideIfVisible(FrameworkElement obj)
        {
            if(obj.Visibility == Visibility.Visible)
            {
                FadeOut(obj, () =>
                {
                    obj.Visibility = Visibility.Hidden;
                    return 0;
                });
            }
        }

        private void FadeOut(FrameworkElement obj, Func<int> doneCallback)
        {
            var open = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(open, obj);
            Storyboard.SetTargetProperty(open, new PropertyPath(FrameworkElement.OpacityProperty));
            var sb = new Storyboard();
            sb.Children.Add(open);
            sb.Completed += (object sender, EventArgs e) => doneCallback();
            sb.Begin();
        }

        private void ui_MediaEnded(object sender, RoutedEventArgs e)
        {
            ui_result.Position = TimeSpan.Zero;
            ui_result.Play();
        }
    }
}
