using Common.Wpf.Extensions;
using System.Windows;

namespace JenkinsStatusWindow.Options
{
    public partial class GeneralOptionsPanel
    {
        public GeneralOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(object data)
        {
            base.LoadPanel(data);

            var settings = Properties.Settings.Default;

            StartWithWindows.IsChecked = settings.AutoStart;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            var settings = Properties.Settings.Default;

            if (StartWithWindows.IsChecked.HasValue && settings.AutoStart != StartWithWindows.IsChecked.Value)
                settings.AutoStart = StartWithWindows.IsChecked.Value;

            Application.Current.SetStartWithWindows(settings.AutoStart);
        }

        public override string CategoryName => Properties.Resources.OptionCategory_General;
    }
}
