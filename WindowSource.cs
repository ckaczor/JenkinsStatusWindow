using CredentialManagement;
using FloatingStatusWindowLibrary;
using JenkinsStatusWindow.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace JenkinsStatusWindow
{
    public class TextResult
    {
        public StringBuilder WindowText { get; set; } = new StringBuilder();
        public StringBuilder TooltipText { get; set; } = new StringBuilder();
    }

    public class WindowSource : IWindowSource, IDisposable
    {
        private readonly FloatingStatusWindow _floatingStatusWindow;
        private readonly Timer _refreshTimer;
        private readonly Dispatcher _dispatcher;
        private readonly JenkinsProjects _jenkinsProjects;

        internal WindowSource()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            _floatingStatusWindow = new FloatingStatusWindow(this);
            _floatingStatusWindow.SetText(Resources.Loading);

            _jenkinsProjects = JenkinsProjects.Load();

            _refreshTimer = new Timer(60000) { AutoReset = false };
            _refreshTimer.Elapsed += HandleTimerElapsed;

            Task.Factory.StartNew(UpdateText);
        }

        public void Dispose()
        {
            _refreshTimer.Dispose();

            _floatingStatusWindow.Save();
            _floatingStatusWindow.Dispose();
        }

        public string Name => Resources.Name;

        public Icon Icon => Resources.ApplicationIcon;

        public string WindowSettings
        {
            get
            {
                return Settings.Default.WindowSettings;
            }
            set
            {
                Settings.Default.WindowSettings = value;
                Settings.Default.Save();
            }
        }

        private void HandleTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            var textResult = GetText();

            // Update the window on the main thread
            _dispatcher.Invoke(() =>
            {
                _floatingStatusWindow.SetText(textResult.WindowText.ToString());

                _floatingStatusWindow.IconToolTipText = textResult.TooltipText.ToString();
            });

            _refreshTimer.Start();
        }

        private TextResult GetText()
        {
            var textResult = new TextResult();

            if (_jenkinsProjects.Count == 0)
                return textResult;

            textResult.WindowText.AppendLine(Resources.LastBuildNumberHeader);

            foreach (var jenkinsProject in _jenkinsProjects)
            {
                if (textResult.WindowText.Length > 0)
                    textResult.WindowText.AppendLine();

                if (textResult.TooltipText.Length > 0)
                    textResult.TooltipText.AppendLine();

                var buildNumberUrl = string.Format(Settings.Default.JenkinsUrlTemplate, jenkinsProject.Url);

                var buildNumberUri = new Uri(buildNumberUrl);

                var credential = new Credential
                {
                    Target = buildNumberUri.Host,
                    PersistanceType = PersistanceType.Enterprise,
                    Type = CredentialType.Generic
                };
                credential.Load();

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(buildNumberUrl);

                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Method = "POST";

                var credentialBuffer = new UTF8Encoding().GetBytes(credential.Username + ":" + credential.Password);

                httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(credentialBuffer);

                httpWebRequest.PreAuthenticate = true;

                try
                {
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    var stream = httpResponse.GetResponseStream();

                    if (stream == null)
                        continue;

                    string lastBuildNumber;

                    using (var streamReader = new StreamReader(stream))
                        lastBuildNumber = streamReader.ReadToEnd();

                    textResult.WindowText.AppendFormat(Resources.LastBuildNumberWindowTemplate, jenkinsProject.Name, lastBuildNumber);
                    textResult.TooltipText.AppendFormat(Resources.LastBuildNumberTooltipTemplate, jenkinsProject.Name, lastBuildNumber);
                }
                catch (WebException excp)
                {
                    var stream = excp.Response.GetResponseStream();

                    if (stream == null)
                        continue;

                    using (var streamReader = new StreamReader(stream))
                    {
                        var errorText = streamReader.ReadToEnd();

                        textResult.WindowText.AppendFormat(Resources.LastBuildNumberWindowTemplate, jenkinsProject.Name, errorText);
                        textResult.TooltipText.AppendFormat(Resources.LastBuildNumberTooltipTemplate, jenkinsProject.Name, errorText);
                    }
                }
            }

            return textResult;
        }
    }
}
