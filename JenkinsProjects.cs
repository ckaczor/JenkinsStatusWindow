using JenkinsStatusWindow.Properties;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace JenkinsStatusWindow
{
    public class JenkinsProjects : ObservableCollection<JenkinsProject>
    {
        public static JenkinsProjects Load()
        {
            var jenkinsProjects = Load(Settings.Default.JenkinsProjects);

            return jenkinsProjects;
        }

        private static JenkinsProjects Load(string serializedData)
        {
            var jenkinsProjects = JsonConvert.DeserializeObject<JenkinsProjects>(serializedData) ?? new JenkinsProjects();

            return jenkinsProjects;
        }

        public void Save()
        {
            Settings.Default.JenkinsProjects = Serialize();

            Settings.Default.Save();
        }

        private string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public JenkinsProjects Clone()
        {
            var data = Serialize();

            return Load(data);
        }
    }
}
