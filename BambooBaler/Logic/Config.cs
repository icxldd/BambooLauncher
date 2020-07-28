using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BambooBaler.Logic
{
    public class ProjectInfo
    {
        public string Name { get; set; }
        public string SvrUrl { get; set; }
        public string ProjDir { get; set; }
    }

    public class Config
    {
        public static Config Instance { get; } = new Config();

        public List<ProjectInfo> Projects { get; set; } = new List<ProjectInfo>();

        public string Load()
        {
            Projects.Clear();

            string filename = "config.json";
            if (File.Exists(filename) == false)
            {
                ProjectInfo temp = new ProjectInfo();
                temp.Name = "test";
                temp.SvrUrl = "http://testserver.com";
                temp.ProjDir = "D:\\test-proj";
                Projects.Add(temp);
                var str = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filename, str, Encoding.UTF8);
                return "";
            }
            try
            {
                string json = File.ReadAllText(filename, Encoding.UTF8);

                var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
                if (cfg?.Projects != null)
                    Projects = cfg.Projects;

                return "";
            }catch(Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
