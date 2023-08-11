using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Control;

namespace GameBarMediaControls
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            init();
        }

        private GlobalSystemMediaTransportControlsSessionManager gsmtcsm = null;
        private IDictionary<string, GlobalSystemMediaTransportControlsSession> sessions = null;
        private GlobalSystemMediaTransportControlsSession session = null;
        private string selectedItem = null;


        private async void init() {
            gsmtcsm = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            gsmtcsm.SessionsChanged += Gsmtcsm_SessionsChanged;
            sessionsCombo.SelectionChanged += SessionsCombo_SelectionChanged;

            GetSessions();
        }

        private void Gsmtcsm_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
            sessionsCombo.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, GetSessions);
        }

        private void GetSessions() {
            sessions = gsmtcsm.GetSessions().ToDictionary(s => s.SourceAppUserModelId);

            sessionsCombo.Items.Clear();
            foreach (var key in sessions.Keys) {
                sessionsCombo.Items.Add(key);
            }

            if(selectedItem != null && sessions.Keys.Contains(selectedItem)) sessionsCombo.SelectedItem = selectedItem;
            else sessionsCombo.SelectedIndex = 0;

            session = sessions[sessionsCombo.SelectedItem as string];
        }

        private void SessionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sessionsCombo.SelectedItem == null) return;
            session = sessions[sessionsCombo.SelectedItem as string];
            selectedItem = sessionsCombo.SelectedItem as string;
        }


        private async void Button_Click_PlayPause(object sender, RoutedEventArgs e) {
            if(session != null) await session.TryTogglePlayPauseAsync();
        }

        private async void Button_Click_SkipNext(object sender, RoutedEventArgs e) {
            if (session != null) await session.TrySkipNextAsync();
        }

        private async void Button_Click_SkipPrev(object sender, RoutedEventArgs e) {
            if (session != null) await session.TrySkipPreviousAsync();
        }

        private async void Button_Click_Stop(object sender, RoutedEventArgs e) {
            if (session != null) await session.TryStopAsync();
        }
    }
}
