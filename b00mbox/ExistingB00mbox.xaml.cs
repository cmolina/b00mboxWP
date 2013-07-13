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

namespace b00mbox
{
    public partial class ExistingB00mbox : PhoneApplicationPage
    {
        IDictionary<string, object> state;
        static Mutex m = new Mutex(true, "obtaining parameters");

        public ExistingB00mbox()
        {
            InitializeComponent();
            state = PhoneApplicationService.Current.State;
        }

        private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            var url = existingUrlBox.Text;
            // Fix if the URL doesn't contains the protocol
            if (!url.StartsWith("http"))
                url = "http://" + url;

            // Load this URL to get the viewURL
            webBrowser.Navigate(new Uri(url, UriKind.Absolute));
        }

        private void existingUrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!String.IsNullOrWhiteSpace(existingUrlBox.Text))
                sendBtn.IsEnabled = true;
        }

        private void getTexts(Object o)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var url = webBrowser.Source.AbsoluteUri;

                    if (url.Contains("b00mbox"))
                    {
                        // It is a contributors' URL
                        state["contributorsURL"] = url;
                        // Get the view URL
                        var script = "document.getElementsByTagName('input')[4].value";
                        var viewURL = webBrowser.InvokeScript("eval", script) as String;
                        state["viewURL"] = viewURL;
                        // Get the name
                        script = "document.querySelectorAll('html body div div.coolfont form#thisform div')[0].innerHTML.trim()";
                        var name = webBrowser.InvokeScript("eval", script) as String;
                        state["name"] = name.Substring(name.IndexOf(": ")+2);
                    }
                    else if (url.Contains("slidebomb"))
                    {
                        // It is a view's URL
                        state["viewURL"] = url;
                        // Get the name
                        var script = "document.getElementsByTagName('meta')[10].content";
                        var name = webBrowser.InvokeScript("eval", script) as String;
                        state["name"] = name;
                    }
                    else
                    {
                        MessageBox.Show("You've entered a wrong URL");
                        return;
                    }
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    ThreadPool.QueueUserWorkItem(getTexts);
                }
            });
        }

        private void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(getTexts);
        }

        private void pasteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                existingUrlBox.Text = Clipboard.GetText();
            }
            catch(Exception)
            {

            }
        }
    }
}