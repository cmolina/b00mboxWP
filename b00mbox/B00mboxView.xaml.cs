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
            else if (PhoneApplicationService.Current.State.ContainsKey("vid"))
            {
                var vid = PhoneApplicationService.Current.State["vid"] as string;
                addVideo(vid);
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
                                var viewURL = document.DocumentNode.Descendants("input").ElementAt(4).GetAttributeValue("value", "");
                                b00mbox.ViewURL = viewURL;
                            }
                        });
                    }
                    WebClient wc1 = new WebClient();
                    while (wc1.IsBusy) ;
                    wc1.DownloadStringCompleted += wc1_DownloadStringCompleted;
                    wc1.DownloadStringAsync(new Uri(b00mbox.ViewURL, UriKind.Absolute));

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
            var videosId = e.Result.Substring(i, j - i + 1).Split(',');

            str = "var videoTitles = [";
            i = e.Result.IndexOf(str) + str.Length;
            j = e.Result.IndexOf("];", i);
            var videosNames = e.Result.Substring(i, j - i + 1).Split(',');

            listOfSongs.Clear();
            for (int v = 0; v < videosId.Length; v++)
            {
                listOfSongs.Add(new Song() { Id = videosId[v].Replace("'", " "), Name = videosNames[v].Replace("'", " ") });
            }
        }

        public class Song: INotifyPropertyChanged
        {
            string _name;
            string _id;

            public String Name
            {
                get { return _name; }
                set { if (value != _name) NotifyPropertyChanged("Name"); _name = value; }
            }
            public String Id
            {
                get { return _id; }
                set { if (value != _id) NotifyPropertyChanged("Id"); _id = value; }
            }
            public String Thumbnail
            {
                get { return "http://img.youtube.com/vi/" + Id.Substring(1, _id.Length - 2) + "/mqdefault.jpg"; }
            }

            public string YoutubeURL()
            {
                return "http://www.youtube.com/watch?v=" + Id.Substring(1, _id.Length-2);
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
                YouTube.Play(selection.Id.Substring(1, selection.Id.Length-2),YouTubeQuality.Quality480P);
            }
        }
    }
}