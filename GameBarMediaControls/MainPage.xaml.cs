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

#if DEBUG
            debugPanel.Visibility = Visibility.Visible;
#else
            debugPanel.Visibility = Visibility.Collapsed;
#endif

            Init();
        }

        private GlobalSystemMediaTransportControlsSessionManager gsmtcsm = null;
        private IDictionary<string, GlobalSystemMediaTransportControlsSession> sessions = null;
        private GlobalSystemMediaTransportControlsSession selectedSession = null;
        private string selectedItem = null;

        private readonly List<int> handlersAddedHashes = new List<int>();


        private async void Init() {
            gsmtcsm = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            gsmtcsm.SessionsChanged += Gsmtcsm_SessionsChanged;
            sessionsCombo.SelectionChanged += SessionsCombo_SelectionChanged;

            GetSessions();
        }

        private async void Gsmtcsm_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                GetSessions();
            });
        }

        private async void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) {
            if (sender != selectedSession) return;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ShowMetadata();
                RefreshButtons();
            });
        }

        private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
            if (sender != selectedSession) return;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ShowMetadata();
                RefreshButtons();
            });
        }

        private async void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) {
            if (sender != selectedSession) return;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                UpdateTimeline();
            });
        }

        private void GetSessions() {
            sessions = gsmtcsm.GetSessions().ToDictionary(s => s.SourceAppUserModelId);

            sessionsCombo.Items.Clear();
            foreach (var key in sessions.Keys) {
                sessionsCombo.Items.Add(key);
            }

            // add event listeners
            foreach (var session in sessions.Values) {
                // continue if listeners already added to this session
                if(handlersAddedHashes.Contains(session.GetHashCode())) continue;

                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
                session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;

                handlersAddedHashes.Add(session.GetHashCode());
            }

            if (selectedItem != null && sessions.Keys.Contains(selectedItem)) sessionsCombo.SelectedItem = selectedItem;
            else sessionsCombo.SelectedIndex = 0;

            if (sessionsCombo.SelectedItem != null) selectedSession = sessions[sessionsCombo.SelectedItem as string];
            else selectedSession = null;
        }

        private async void ShowMetadata() {
            if (selectedSession == null) return;
            var mediaProperties = await selectedSession.TryGetMediaPropertiesAsync();
            if (mediaProperties == null) return;

            if (mediaProperties.Thumbnail != null) {
                var bitmap = new BitmapImage {
                    DecodePixelHeight = 100,
                    DecodePixelWidth = 100
                };
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

        private void RefreshButtons() {
            if (selectedSession == null) return;
            var playbackInfo = selectedSession.GetPlaybackInfo();
            if(playbackInfo == null) return;

            switch(playbackInfo.PlaybackStatus) {
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                    imgPlay.Visibility = Visibility.Collapsed;
                    imgPause.Visibility = Visibility.Visible;
                    break;
                case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                    imgPlay.Visibility = Visibility.Visible;
                    imgPause.Visibility = Visibility.Collapsed;
                    break;
            }

            btnPrev.IsEnabled = playbackInfo.Controls.IsPreviousEnabled;
            btnPlayPause.IsEnabled = playbackInfo.Controls.IsPlayPauseToggleEnabled;
            btnNext.IsEnabled = playbackInfo.Controls.IsNextEnabled;
            btnStop.IsEnabled = playbackInfo.Controls.IsStopEnabled;
        }

        private void UpdateTimeline() {
            if (selectedSession == null) return;
            var timelineProperties = selectedSession.GetTimelineProperties();
            if (timelineProperties == null) return;

            timeSlider.Minimum = Math.Round(timelineProperties.StartTime.TotalSeconds);
            timeSlider.Maximum = Math.Round(timelineProperties.EndTime.TotalSeconds);
            timeSlider.Value = Math.Round(timelineProperties.Position.TotalSeconds);

            if (timelineProperties.EndTime.TotalHours < 1.0) {
                timeText.Text = timelineProperties.Position.ToString(@"%m\:ss");
                timeText2.Text = timelineProperties.EndTime.ToString(@"%m\:ss");

            }
            else {
                timeText.Text = timelineProperties.Position.ToString(@"%h\:mm\:ss");
                timeText2.Text = timelineProperties.EndTime.ToString(@"%h\:mm\:ss");
            }
        }

        private void SessionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sessionsCombo.SelectedItem == null) return;
            selectedSession = sessions[sessionsCombo.SelectedItem as string];
            selectedItem = sessionsCombo.SelectedItem as string;
            ShowMetadata();
            RefreshButtons();
            UpdateTimeline();
        }

        private async void Button_Click_PlayPause(object sender, RoutedEventArgs e) {
            if(selectedSession != null) await selectedSession.TryTogglePlayPauseAsync();
        }

        private async void Button_Click_SkipNext(object sender, RoutedEventArgs e) {
            if (selectedSession != null) await selectedSession.TrySkipNextAsync();
        }

        private async void Button_Click_SkipPrev(object sender, RoutedEventArgs e) {
            if (selectedSession != null) await selectedSession.TrySkipPreviousAsync();
        }

        private async void Button_Click_Stop(object sender, RoutedEventArgs e) {
            if (selectedSession != null) await selectedSession.TryStopAsync();
        }

        private async void Button_Click_Shuffle(object sender, RoutedEventArgs e) {
            var tprop = selectedSession.GetTimelineProperties();
            (sender as Button).Content = tprop.Position.ToString();

            // no worky in opera spotify, so not goina implement
        }

        private async void Button_Click_Repeat(object sender, RoutedEventArgs e) {
            // no worky in opera spotify, so not goina implement
            
        }
    }
}
