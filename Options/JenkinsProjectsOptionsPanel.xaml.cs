using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JenkinsStatusWindow.Options
{
    public partial class JenkinsProjectsOptionsPanel
    {
        public JenkinsProjectsOptionsPanel()
        {
            InitializeComponent();
        }

        private JenkinsProjects JenkinsProjects => Data as JenkinsProjects;

        public override void LoadPanel(object data)
        {
            base.LoadPanel(data);

            JenkinsProjectsGrid.ItemsSource = JenkinsProjects;

            JenkinsProjectsGrid.SelectedItem = JenkinsProjects.FirstOrDefault();
            SetButtonStates();
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {

        }

        public override string CategoryName => Properties.Resources.OptionCategory_JenkinsProjects;

        private void SetButtonStates()
        {
            EditJenkinsProjectButton.IsEnabled = (JenkinsProjectsGrid.SelectedItems.Count == 1);
            DeleteJenkinsProjectButton.IsEnabled = (JenkinsProjectsGrid.SelectedItems.Count > 0);
        }

        private void AddJenkinsProject()
        {
            var jenkinsProject = new JenkinsProject();

            var jenkinsProjectWindow = new JenkinsProjectWindow();

            var result = jenkinsProjectWindow.Display(jenkinsProject, Window.GetWindow(this));

            if (result.HasValue && result.Value)
            {
                JenkinsProjects.Add(jenkinsProject);

                JenkinsProjectsGrid.SelectedItem = jenkinsProject;

                SetButtonStates();
            }
        }

        private void EditSelectedJenkinsProject()
        {
            if (JenkinsProjectsGrid.SelectedItem == null)
                return;

            var jenkinsProject = JenkinsProjectsGrid.SelectedItem as JenkinsProject;

            var jenkinsProjectWindow = new JenkinsProjectWindow();

            jenkinsProjectWindow.Display(jenkinsProject, Window.GetWindow(this));
        }

        private void DeleteSelectedJenkinsProject()
        {
            var jenkinsProject = JenkinsProjectsGrid.SelectedItem as JenkinsProject;
            var index = JenkinsProjectsGrid.SelectedIndex;

            JenkinsProjects.Remove(jenkinsProject);

            if (JenkinsProjectsGrid.Items.Count == index)
                JenkinsProjectsGrid.SelectedIndex = JenkinsProjectsGrid.Items.Count - 1;
            else if (JenkinsProjectsGrid.Items.Count >= index)
                JenkinsProjectsGrid.SelectedIndex = index;

            SetButtonStates();
        }

        private void HandleAddJenkinsProjectButtonClick(object sender, RoutedEventArgs e)
        {
            AddJenkinsProject();
        }

        private void HandleEditJenkinsProjectButtonClick(object sender, RoutedEventArgs e)
        {
            EditSelectedJenkinsProject();
        }

        private void HandleDeleteJenkinsProjectButtonClick(object sender, RoutedEventArgs e)
        {
            DeleteSelectedJenkinsProject();
        }

        private void HandleJenkinsProjectsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetButtonStates();
        }

        private void HandleJenkinsProjectsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditSelectedJenkinsProject();
        }
    }
}
