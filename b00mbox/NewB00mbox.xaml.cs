using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.Threading;

namespace b00mbox
{
    public partial class NewB00mbox : PhoneApplicationPage
    {
        B00mbox actual;

        public NewB00mbox()
        {
            InitializeComponent();
            webBrowser.IsScriptEnabled = true;
            webBrowser.Navigate(new Uri("http://b00mbox.com/bb_create.php", UriKind.Absolute));
        }

        private void btnSendData_Click(object sender, EventArgs e)
        {
            try
            {
                // first load the data collected in the page
                var name = boxName.Text;
                var setName = "document.getElementsByTagName('input')[0].value = \"" + name + "\"";
                var description = boxDescription.Text;
                var setDescription = "document.getElementsByTagName('textarea')[0].value = \"" + description + "\"";
                var contributors = acbContributors.Text;
                var setContributors = "document.getElementsByTagName('textarea')[1].value = \"" + contributors + "\"";
                evalJavascript(setName);
                evalJavascript(setDescription);
                evalJavascript(setContributors);

                // prepare thw webBrowser to the request
                webBrowser.LoadCompleted += webBrowser_LoadCompleted;

                // send data
                var send = "document.querySelector(\"input[name='submit']\").click();";
                evalJavascript(send);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(sendData);
        }

        private void sendData(Object o)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // We are in the Contributors' page
                    var contributorsURL = webBrowser.Source.AbsolutePath;
                    var script = "document.getElementsByTagName('input')[4].value";
                    var viewURL = evalJavascript(script) as String;
                    script = "document.querySelectorAll('html body div div.coolfont form#thisform div')[0].innerHTML.trim()";
                    var name = evalJavascript(script) as String;

                    // Add this b00mbox to the list
                    var b00mboxs = PhoneApplicationService.Current.State["b00mboxs"] as ObservableCollection<B00mbox>;
                    actual = new B00mbox(name, contributorsURL, viewURL);
                    b00mboxs.Add(actual);

                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
                catch (Exception ex)
                {
                    ThreadPool.QueueUserWorkItem(sendData);
                }
            });
        }

        private object evalJavascript(string script)
        {
            return webBrowser.InvokeScript("eval", script);
        }

        private void box_TextChanged(object sender, RoutedEventArgs e)
        {
            // enable/disable send button
            var btn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            btn.IsEnabled = !(String.IsNullOrWhiteSpace(boxName.Text) ||
                String.IsNullOrWhiteSpace(boxDescription.Text) || String.IsNullOrWhiteSpace(acbContributors.Text));
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}