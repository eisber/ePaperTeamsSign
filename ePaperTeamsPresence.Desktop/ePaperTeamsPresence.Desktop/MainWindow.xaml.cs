using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Text.Json;
using System.Threading;
using System.Configuration;

namespace ePaperTeamsPresence.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread pollingThread;
        private Presence lastPresence;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private async void Poll()
        {
            var storageProperties = new StorageCreationPropertiesBuilder("msal_cache.dat", "MSAL_CACHE").Build();

            IPublicClientApplication publicClientApp = PublicClientApplicationBuilder
                .Create(ConfigurationManager.AppSettings["AADAppClientId"])
                .WithRedirectUri(ConfigurationManager.AppSettings["AADAppRedirectUri"])
                .WithAuthority(AzureCloudInstance.AzurePublic, ConfigurationManager.AppSettings["AADAppTenantId"])
                .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(publicClientApp.UserTokenCache);

            var graphScopes = new List<string> { "https://graph.microsoft.com/.default" };

            var authProvider = new WPFAuthorizationProvider(publicClientApp, graphScopes);
            var graphClient = new GraphServiceClient(authProvider);

            while (true)
            {
                var presence = await graphClient
                    .Me
                    .Presence
                    .Request()
                    .GetAsync()
                    .ConfigureAwait(false);

                // move to UX thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // only update on change
                    if (lastPresence == null || 
                        presence.Activity != lastPresence.Activity ||
                        presence.Availability != lastPresence.Availability) {

                        UpdateDisplay(presence);

                        SendToDevice();
                    }

                    lastPresence = presence;
                });
                  
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        private void UpdateDisplay(Presence presence)
        {
            ePaperStatus.Text = presence.Activity;

            switch (presence.Activity)
            {
                case "InACall":
                case "InAConferenceCall":
                case "InAMeeting":
                    ePaperMain.Text = "On-Air";
                    ePaperMain.Foreground = new SolidColorBrush(Colors.Red);
                    MicMute.Visibility = Visibility.Collapsed;
                    MicMuteBorder.Visibility = Visibility.Collapsed;

                    break;

                default:
                    ePaperMain.Text = "Free";
                    ePaperMain.Foreground = new SolidColorBrush(Colors.Black);
                    MicMute.Visibility = Visibility.Visible;
                    MicMuteBorder.Visibility = Visibility.Visible;

                    break;
            }

            ePaperTime.Text = DateTime.Now.ToString("HH:mm");

            // refresh UX
            this.UpdateLayout();
        }

        private void SendToDevice()
        {
            ePaperTime.Visibility = Visibility.Hidden;
            //ePaperTemperature.Visibility = Visibility.Hidden;
            //ePaperHumidity.Visibility = Visibility.Hidden;

            var bitmapBlack = BitmapUtil.ToBytes(BitmapUtil.ColorMask(ePaper, Colors.Black));
            var bitmapRed = BitmapUtil.ToBytes(BitmapUtil.ColorMask(ePaper, Colors.Red));

            ePaperTime.Visibility = Visibility.Visible;
            //ePaperTemperature.Visibility = Visibility.Visible;
            //ePaperHumidity.Visibility = Visibility.Visible;

            // debugging
            //System.IO.File.WriteAllBytes(@"c:\tools\temp\black.bmp", bitmapBlack);
            //System.IO.File.WriteAllBytes(@"c:\tools\temp\red.bmp", bitmapRed);

            var templatePosition = new
            {
                Time = ePaperTime.TranslatePoint(new Point(0, 0), ePaper).ToIntPoint(),
                //Temperature = ePaperTemperature.TranslatePoint(new Point(0, 0), ePaper).ToIntPoint(),
                //Humidity = ePaperHumidity.TranslatePoint(new Point(0, 0), ePaper).ToIntPoint(),
            };

            // run in background
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        var multipart = new MultipartFormBuilder();

                        // upload split images
                        multipart.AddFile("red", "red", bitmapRed);
                        multipart.AddFile("black", "red", bitmapBlack);

                        // upload
                        multipart.AddField("template", JsonSerializer.Serialize(templatePosition));

                        // send to Raspberry Pi
                        client.UploadMultipart(
                           ConfigurationManager.AppSettings["UploadUrl"], 
                           "POST", 
                           multipart);
                    }
                }
                catch (Exception ex)
                {
                    // TODO: improve
                    Console.WriteLine(ex.Message);
                }
            });
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.pollingThread = new Thread(Poll);
            this.pollingThread.IsBackground = true;
            this.pollingThread.Start();
        }
    }
}
