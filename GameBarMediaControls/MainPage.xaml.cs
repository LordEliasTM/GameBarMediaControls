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
using Windows.UI.Xaml.Media.Imaging;

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

        private async void Gsmtcsm_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
            await sessionsCombo.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                debugOutput.Text = debugOutput.Text + args.ToString() + "\n";
                GetSessions();
            });
        }

        private void GetSessions() {
            sessions = gsmtcsm.GetSessions().ToDictionary(s => s.SourceAppUserModelId);

            sessionsCombo.Items.Clear();
            foreach (var key in sessions.Keys) {
                sessionsCombo.Items.Add(key);
            }

            if (selectedItem != null && sessions.Keys.Contains(selectedItem)) sessionsCombo.SelectedItem = selectedItem;
            else sessionsCombo.SelectedIndex = 0;

            if (sessionsCombo.SelectedItem != null) session = sessions[sessionsCombo.SelectedItem as string];
            else session = null;
        }

        private async void GetMetadata() {
            if (session == null) return;
            var mediaProperties = await session.TryGetMediaPropertiesAsync();

            if (mediaProperties.Thumbnail != null) {
                var bitmap = new BitmapImage();
                bitmap.DecodePixelHeight = 100;
                bitmap.DecodePixelWidth = 100;
                var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                stream.Seek(0);
                await bitmap.SetSourceAsync(stream);
                thumbnail.Source = bitmap;
            }
            else thumbnail.Source = null;

            if (mediaProperties.Title != null && mediaProperties.Subtitle != null) {
                title.Text = mediaProperties.Title;
                artist.Text = mediaProperties.Artist;
            }
            else {
                title.Text = "";
                artist.Text = "";
            }
        }

        // TODO implement events fired by the session

        private void SessionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sessionsCombo.SelectedItem == null) return;
            session = sessions[sessionsCombo.SelectedItem as string];
            selectedItem = sessionsCombo.SelectedItem as string;
            GetMetadata();
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

        private async void Button_Click(object sender, RoutedEventArgs e) {
            if (session != null) {
                var timelineProperties = session.GetTimelineProperties();
                var playbackInfo = session.GetPlaybackInfo();
                var mediaProperties = await session.TryGetMediaPropertiesAsync();
                timeSlider.Maximum = session.GetTimelineProperties().EndTime.Seconds;
                timeSlider.Value = session.GetTimelineProperties().Position.Seconds;

                var bitmap = new BitmapImage();
                var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                stream.Seek(0);
                await bitmap.SetSourceAsync(stream);
                thumbnail.Source = bitmap;

                title.Text = mediaProperties.Title;
            }
        }
    }
}
