using CredentialManagement;
using FloatingStatusWindowLibrary;
using JenkinsStatusWindow.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using Common.Wpf.Windows;
using JenkinsStatusWindow.Options;

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

        private JenkinsProjects _jenkinsProjects;
        private CategoryWindow _optionsWindow;

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
            get => Settings.Default.WindowSettings;
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
            try
            {
                var textResult = GetText();

                // Update the window on the main thread
                _dispatcher.Invoke(() =>
                {
                    _floatingStatusWindow.SetText(textResult.WindowText.ToString());

                    _floatingStatusWindow.IconToolTipText = textResult.TooltipText.ToString();
                });
            }
            catch (Exception exception)
            {
                _dispatcher.Invoke(() =>
                {
                    _floatingStatusWindow.SetText(exception.Message);

                    _floatingStatusWindow.IconToolTipText = exception.Message;
                });
            }
            finally
            {
                _refreshTimer.Interval = 60000;
                _refreshTimer.Start();
            }
        }

        private TextResult GetText()
        {
            var textResult = new TextResult();

            var enabledProjects = _jenkinsProjects.Where(p => p.Enabled).ToList();

            if (enabledProjects.Count == 0)
                return textResult;

            textResult.WindowText.AppendLine(Resources.LastBuildNumberHeader);

            foreach (var jenkinsProject in enabledProjects)
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

                var httpWebRequest = (HttpWebRequest) WebRequest.Create(buildNumberUrl);

                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Method = "POST";

                var credentialBuffer = new UTF8Encoding().GetBytes(credential.Username + ":" + credential.Password);

                httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(credentialBuffer);

                httpWebRequest.PreAuthenticate = true;

                try
                {
                    var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();

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

        public bool HasSettingsMenu => true;

        public bool HasRefreshMenu => true;

        public void ShowSettings()
        {
            var panels = new List<CategoryPanel>
            {
                new GeneralOptionsPanel(),
                new JenkinsProjectsOptionsPanel(),
                new AboutOptionsPanel()
            };

            var jenkinsProjects = JenkinsProjects.Load();

            if (_optionsWindow == null)
            {
                _optionsWindow = new CategoryWindow(jenkinsProjects, panels, Resources.ResourceManager, "OptionsWindow");
                _optionsWindow.Closed += (o, args) => { _optionsWindow = null; };
            }

            var dialogResult = _optionsWindow.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                jenkinsProjects.Save();

                _jenkinsProjects = JenkinsProjects.Load();

                Refresh();
            }
        }

        public void Refresh()
        {
            _refreshTimer.Stop();

            _floatingStatusWindow.SetText(Resources.Loading);

            _refreshTimer.Interval = 500;
            _refreshTimer.Start();
        }
    }
}
