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
            b00mbox = PhoneApplicationService.Current.State["b00mbox"] as B00mbox;
            b00mboxNameBlock.Text = b00mbox.Name;

            ThreadPool.QueueUserWorkItem(getSongs);

            base.OnNavigatedTo(e);
        }

        private void getSongs(Object o)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    
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

            for (int v = 0; v < videosId.Length; v++)
            {
                listOfSongs.Add(new Song() { Id = videosId[v], Name=videosNames[v] });
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
                YouTube.Play(selection.Id.Substring(1, selection.Id.Length-2));
            }
        }
    }
}