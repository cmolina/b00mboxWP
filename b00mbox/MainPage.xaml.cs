using System;
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
using RestSharp;
using System.Threading;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace b00mbox
{
    public partial class MainPage : PhoneApplicationPage
    {
        IsolatedStorageSettings settings;
        ObservableCollection<B00mbox> b00mboxs;
        IDictionary<string, object> state;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains("b00mboxs"))
                b00mboxs = settings["b00mboxs"] as ObservableCollection<B00mbox>;
            else
                b00mboxs = new ObservableCollection<B00mbox>();
            b00mboxs.CollectionChanged += b00mboxs_CollectionChanged;
            b00mboxs_CollectionChanged(b00mboxs, null);
            b00mboxList.ItemsSource = b00mboxs;
            state = PhoneApplicationService.Current.State;
        }

        void b00mboxs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (b00mboxs.Count > 0)
                blockEmptyList.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnAddExisting_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/ExistingB00mbox.xaml", UriKind.Relative));
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            state["b00mboxs"] = b00mboxs;
            NavigationService.Navigate(new Uri("/NewB00mbox.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (state.ContainsKey("contributorsURL") || state.ContainsKey("viewURL"))
            {
                var contributorsURL = state.ContainsKey("contributorsURL") ? state["contributorsURL"] as String : String.Empty;
                var viewURL = state["viewURL"] as String;
                var name = state["name"] as String; 
                
                // Add the b00mbox to the list
                var b00mbox = new B00mbox(name, contributorsURL, viewURL);
                b00mboxs.Add(b00mbox);
                settings["b00mboxs"] = b00mboxs;
                settings.Save();
                // Remove the parameters from the state
                if (state.ContainsKey("contributorsURL")) state.Remove("contributorsURL");
                state.Remove("viewURL");
                state.Remove("name");
            }
            base.OnNavigatedTo(e);
        }

        private void b00mboxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var b00mbox = e.AddedItems[0] as B00mbox;
                PhoneApplicationService.Current.State["b00mbox"] = b00mbox;
                NavigationService.Navigate(new Uri("/B00mboxView.xaml", UriKind.Relative));
            }
        }
    }

    [DataContract]
    public class B00mbox: INotifyPropertyChanged
    {
        private string _name;
        private string _contributorsURL;
        private string _viewURL;

        [DataMember]
        public String Name
        {
            get { return _name; }
            set { if (value != _name) { NotifyPropertyChanged("Name"); _name = value.Substring(value.IndexOf(": ")+2); } }
        }
        [DataMember]
        public String ContributorsURL
        {
            get { return _contributorsURL; }
            set { if (value != _contributorsURL) { NotifyPropertyChanged("ContributorsURL"); _contributorsURL = value; } }
        }
        [DataMember]
        public String ViewURL
        {
            get { return _viewURL; }
            set { if (value != _viewURL) { NotifyPropertyChanged("ViewURL"); _viewURL = value; } }
        }

        public B00mbox(string name, string contributorsURL, string viewURL)
        {
            _name = name;
            _contributorsURL = contributorsURL;
            _viewURL = viewURL;
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

}