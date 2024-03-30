using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;

namespace YouTubePlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static WaveOutEvent? outputDevice;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton(object sender, RoutedEventArgs e)
        {
            var youtube = new YoutubeClient();

            var inputUrl = UrlBox.Text;

            if (inputUrl.Contains("list"))
            {
                //MessageBox.Show("Playlist");
                await PlayPlaylist(youtube, inputUrl);
            }
            else
            {
                await PlaySingleVideo(youtube, inputUrl);
            }
        }

        static async Task PlaySingleVideo(YoutubeClient client, string urlString)
        {
            var streamManifest = await client.Videos.Streams.GetManifestAsync(urlString);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            using (var audioStream = await client.Videos.Streams.GetAsync(audioStreamInfo))
            {
                var tempFileName = System.IO.Path.GetTempFileName();
                using (var fileStream = File.Create(tempFileName))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                var waveStream = new MediaFoundationReader(tempFileName);
                var waveChannel = new WaveChannel32(waveStream);

                outputDevice = new WaveOutEvent();
                outputDevice.Init(waveChannel);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1000);
                }

                File.Delete(tempFileName); // Удаляем временный файл после воспроизведения
            }

        }

        static async Task PlayPlaylist(YoutubeClient youtube, string playlistUrl)
        {
            var playlist = await youtube.Playlists.GetAsync(playlistUrl);
            var videos = await youtube.Playlists.GetVideosAsync(playlist.Id);

            await PlayNextVideoInPlaylist(youtube, videos.GetEnumerator());
        }

        static async Task PlayNextVideoInPlaylist(YoutubeClient youtube, IEnumerator<PlaylistVideo> videoEnumerator)
        {
            if (!videoEnumerator.MoveNext())
            {
                // Все видео из плейлиста воспроизведены
                return;
            }

            var videoId = videoEnumerator.Current.Id;

            // Воспроизведение текущего видео
            //MessageBox.Show(videoEnumerator.Current.Title);
            await PlaySingleVideo(youtube, videoId);

            // Рекурсивный вызов для воспроизведения следующего видео в плейлисте
            await PlayNextVideoInPlaylist(youtube, videoEnumerator);
        }

        private void SkipButton(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null)
                outputDevice.Stop();
        }

        //private void PauseButton(object sender, RoutedEventArgs e)
        //{
        //    if (outputDevice != null && (outputDevice.PlaybackState != PlaybackState.Paused || outputDevice.PlaybackState != PlaybackState.Stopped))
        //        outputDevice.Pause();
        //    else
        //        outputDevice.Pause();

        //}

        //private void StopButton(object sender, RoutedEventArgs e)
        //{
        //    if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
        //    {
        //        outputDevice.Stop();
        //    }
        //}
    }
}