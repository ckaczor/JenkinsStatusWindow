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

            if (jenkinsProjects.Count == 0)
            {
                jenkinsProjects.Add(new JenkinsProject { Name = "VPW - Phase 2", Url = "http://jenkins-master.vertical.com:8080/job/vpw/" });
                jenkinsProjects.Add(new JenkinsProject { Name = "VPW - Phase 3", Url = "http://172.22.1.60:8080/view/Phase%203%20Apps/job/P3%20VPW/" });
                jenkinsProjects.Add(new JenkinsProject { Name = "Company Manager", Url = "http://172.22.1.60:8080/view/Phase%203%20Apps/job/P3%20Company%20Manager/" });
                jenkinsProjects.Add(new JenkinsProject { Name = "Profile", Url = "http://172.22.1.60:8080/view/Phase%203%20Apps/job/P3%20Profile/" });
            }

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
