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
                var contributors = boxContributors.Text;
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
                MessageBox.Show(ex.Message, "Something happened", MessageBoxButton.OK);
                NavigationService.GoBack();
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
                    var contributorsURL = webBrowser.Source.ToString();
                    
                    var script = "document.querySelector('html body div div.coolfont form#thisform div').innerHTML.trim()";
                    var name = evalJavascript(script) as String;
                    name = name.Substring(name.IndexOf(": ") + 2);

                    // Add this b00mbox to the list
                    var state = PhoneApplicationService.Current.State;
                    state["name"] = name;
                    state["contributorsURL"] = contributorsURL;

                    NavigationService.GoBack();
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
                String.IsNullOrWhiteSpace(boxDescription.Text));
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}