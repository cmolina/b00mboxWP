﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace b00mbox
{
    public partial class AddVideos : PhoneApplicationPage
    {
        static string MyDevKey = "AI39si5oXXP4VM3hsbLJ6uYfiOcvPqvUIYESFgMuckmTmcuzO265y14W3e3GeRN3iW9NZYPtqhlMi_bF4yefv4ImVLPvV7SmHw";
        string vidUri;
        List<string> Uris = new List<string>();
        List<string> titles = new List<string>();
        
        public AddVideos()
        {
            InitializeComponent();
        }

        private void tbSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            tbSearch.Text = "";
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            wc_todo("http://gdata.youtube.com/feeds/api/videos?v=2&alt=jsonc&max-results=5&q=" + tbSearch.Text + "&key=" + MyDevKey + "&max-results=5");

        }

        private void wc_todo(string Uri)
        {
            WebClient wc1 = new WebClient();
            WebClient wc2 = new WebClient();
            try
            {
                if (!(wc1.IsBusy))
                {
                    wc1.DownloadStringAsync(new Uri(Uri, UriKind.Absolute));
                    wc1.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                }
                else if (!(wc2.IsBusy))
                {
                    wc2.DownloadStringAsync(new Uri(Uri, UriKind.Absolute));
                    wc2.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs c)
        {
            var o = JObject.Parse(c.Result);
            var videos = from v in o["data"]["items"].Children() select new ydata { url = "http://www.youtube.com/watch?v="+(string)v["id"], title = (string)v["title"], description = (string)v["description"], thumbnail = (string)v["thumbnail"]["sqDefault"], mobile = (string)v["content"]["6"] };
            lbHasil.ItemsSource = videos;
            foreach (var vid in videos)
            {
                Uris.Add(vid.url);
                titles.Add(vid.title);
            }
        }

        private void lbHasil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var state = PhoneApplicationService.Current.State;
            if (lbHasil.SelectedIndex != -1)
            {
                vidUri = Uris[lbHasil.SelectedIndex];
                state["title"] = titles[lbHasil.SelectedIndex];
                state["vid"] = vidUri;
                NavigationService.GoBack();
            }
        }

        public static event YoutubeNameEventHandler YoutubeGetNameCompleted = delegate { };

        public static void YoutubeGetName(string id)
        {
            var client = new RestClient();
            var request = new RestRequest(
                "http://gdata.youtube.com/feeds/api/videos/" + id + "?v=2&alt=json", 
                Method.GET);

            client.ExecuteAsync(request, (r, a) =>
            {
                if (r.ResponseStatus == ResponseStatus.Completed)
                {
                    var o = JObject.Parse(r.Content);
                    var name = (string)o["entry"]["title"]["$t"];

                    YoutubeGetNameCompleted(id, name);
                }
            });
        }
    }

    public delegate void YoutubeNameEventHandler(string id, string name);

    public class ydata
    {
        public string url { get; set; }
        public string mobile { get; set; }
        public string title { get; set; }
        public string thumbnail { get; set; }
        public string description { get; set; }
    }
}