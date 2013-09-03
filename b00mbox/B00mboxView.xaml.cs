using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MyToolkit.Multimedia;
using RestSharp;

namespace b00mbox
{
    public partial class B00mboxView : PhoneApplicationPage
    {
        B00mbox b00mbox;
        ObservableCollection<Song> listOfSongs;

        public B00mboxView()
        {
            InitializeComponent(); 
            listOfSongs = new ObservableCollection<Song>();
            listOfSongs.CollectionChanged += listOfSongs_CollectionChanged;
            b00mboxList.ItemsSource = listOfSongs;
        }

        void listOfSongs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (listOfSongs.Count > 0)
                blockEmptyList.Visibility = System.Windows.Visibility.Collapsed;
            else
                blockEmptyList.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnAddSong_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AddVideos.xaml", UriKind.Relative));
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(getSongs);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var newB00mbox = PhoneApplicationService.Current.State["b00mbox"] as B00mbox;
            if (b00mbox != newB00mbox)
            {
                b00mbox = newB00mbox;
                b00mboxNameBlock.Text = b00mbox.Name;
                ThreadPool.QueueUserWorkItem(getSongs);
            }
            if (PhoneApplicationService.Current.State.ContainsKey("vid"))
            {
                var vid = PhoneApplicationService.Current.State["vid"] as string;
                addVideo(vid);
                PhoneApplicationService.Current.State.Remove("vid");
            }

            base.OnNavigatedTo(e);
        }

        private void addVideo(string vid)
        {
            var client = new RestClient();
            var request = new RestRequest(b00mbox.ContributorsURL, Method.POST);
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Host", "b00mbox.com");
            request.AddHeader("Referer", b00mbox.ContributorsURL);
            request.AddParameter("_submit_check", "Submit Check");
            request.AddParameter("linkurl", vid);
            request.AddParameter("submit.x", 0);
            request.AddParameter("submit.y", 0);
            request.AddParameter("vidurl", vid);

            var response = client.ExecuteAsync(request, (r, a) =>
            {
                if (r.ResponseStatus == ResponseStatus.Completed)
                    // Get the songs again
                    ThreadPool.QueueUserWorkItem(getSongs);
            });
        }

        private void getSongs(Object o)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (b00mbox.ViewURL == null)
                    {
                        // Get the view url from the contributor's URL
                        var client = new RestClient();
                        var request = new RestRequest(b00mbox.ContributorsURL);
                        var async = client.ExecuteAsync(request, (r, a) =>
                        {
                            if (r.ResponseStatus == ResponseStatus.Completed)
                            {
                                var document = new HtmlAgilityPack.HtmlDocument();
                                document.LoadHtml(r.Content);

                                var inputs = document.DocumentNode.Descendants("input");
                                var viewUrlIndex = 4;
                                if (inputs.Count() > viewUrlIndex)
                                {
                                    var viewURL = inputs.ElementAt(viewUrlIndex).GetAttributeValue("value", "");
                                    b00mbox.ViewURL = viewURL;

                                    // Now we have the View URI, try to look for the songs again
                                    ThreadPool.QueueUserWorkItem(getSongs);
                                }
                            }
                        });
                    }
                    else
                    {
                        // If we have the View URI, we have the videos
                        WebClient wc1 = new WebClient();
                        while (wc1.IsBusy) ;
                        wc1.DownloadStringCompleted += wc1_DownloadStringCompleted;
                        wc1.DownloadStringAsync(new Uri(b00mbox.ViewURL, UriKind.Absolute));
                    }

                }
                catch (Exception)
                {
                    ThreadPool.QueueUserWorkItem(getSongs);
                }
            });
        }

        void wc1_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var str = "var videos = [";
            var i = e.Result.IndexOf(str) + str.Length;
            var j = e.Result.IndexOf("];", i);
            var videosId = e.Result.Substring(i, j - i).Split(',');

            str = "var videoTitles = [";
            i = e.Result.IndexOf(str) + str.Length;
            j = e.Result.IndexOf("];", i);
            str = e.Result.Substring(i, j - i);
            var videosNames = new string[videosId.Length];
            for (int k = 0, length = videosNames.Length, quotePos = 0; k < length; k++)
            {
                var firstQuote = str.IndexOf('\'', quotePos);
                var nextQuote = str.IndexOf('\'', firstQuote + 1);
                videosNames[k] = str.Substring(firstQuote + 1, nextQuote - firstQuote - 1);
                quotePos = nextQuote + 1;
            }
            
            listOfSongs.Clear();
            Song lastSong = null;
            for (int v = 0; v < videosId.Length; v++)
            {
                var id = videosId[v].Replace("'", "");

                // Avoid repeated songs
                if (lastSong != null && lastSong.Id == id)
                    continue;

                var name = HttpUtility.HtmlDecode(videosNames[v]);
                if (string.IsNullOrEmpty(name))
                {
                    name = "Loading...";
                    AddVideos.YoutubeGetNameCompleted += (_i, _n) =>
                    {
                        var indexVideo = listOfSongs.FirstOrDefault(_v => _v.Id == _i);
                        if (indexVideo != null)
                            indexVideo.Name = _n;
                    };
                    try
                    {
                        AddVideos.YoutubeGetName(id);
                    }
                    catch (Exception) { };
                }

                lastSong = new Song()
                {
                    Id = id,
                    Name = name
                }; 
                listOfSongs.Add(lastSong);
            }
        }

        public class Song: INotifyPropertyChanged
        {
            string _name;
            string _id;

            public string Name
            {
                get { return _name; }
                set { NotifyPropertyChanged("Name"); _name = value; }
            }
            public string Id
            {
                get { return _id; }
                set { NotifyPropertyChanged("Id"); _id = value; }
            }
            public String Thumbnail
            {
                get { return "http://img.youtube.com/vi/" + _id + "/mqdefault.jpg"; }
            }

            public string YoutubeURL()
            {
                return "http://www.youtube.com/watch?v=" + _id;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private void b00mboxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count > 0)
            {
                var selection = e.AddedItems[0] as Song;
                YouTube.Play(selection.Id, YouTubeQuality.Quality480P);
            }
        }
    }
}