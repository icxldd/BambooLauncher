using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LauncherCommon
{
    public class Util
    {
        public static string FileListToString(FileList list)
        {
            if (list?.Files?.Count == 0)
                return "";
            StringBuilder sb = new StringBuilder();
            foreach (var f in list.Files)
            {
                sb.AppendLine($"{f.Url},{f.LocalPath},{f.Size},{f.Operate},{f.Md5},{f.UpdateTime}");
            }
            return sb.ToString();
        }

        public static int StringToFileList(string str, FileList list)
        {
            if (list == null)
                return 0;
            if (list.Files == null)
                list.Files = new List<FileModel>();

            int count = 0;
            char[] spliter = { ',' };
            try
            {
                StringReader sr = new StringReader(str);
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null)
                        break;
                    var ss = line.Split(spliter);
                    if (ss?.Length != 6)
                        continue;
                    FileModel file = new FileModel();
                    file.Url = ss[0];
                    file.LocalPath = ss[1];
                    long size = 0;
                    long.TryParse(ss[2], out size);
                    file.Size = size;
                    long.TryParse(ss[3], out size);
                    file.Operate = (int)size;
                    file.Md5 = ss[4];
                    DateTime tm;
                    DateTime.TryParse(ss[5], out tm);
                    file.UpdateTime = tm;
                    list.Files.Add(file);
                }
                sr.Close();
            }
            catch
            {
                return 0;
            }
            return count;
        }

        public static string Md5String(string str)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static string Md5File(string path)
        {
            try
            {
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
                var md5 = System.Security.Cryptography.MD5.Create();
                var bytes = md5.ComputeHash(file);
                file.Close();
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
            catch
            {
                return "";
            }
        }

        public static async Task<T> LoadLocalFileToObj<T>(string path)
        {
            try
            {
                StreamReader sr = new StreamReader(path, Encoding.UTF8);
                string str = await sr.ReadToEndAsync();
                sr.Close();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
            }
            catch
            {
                return default(T);
            }
        }

        public static async Task<string> LoadLocalFileToString(string path)
        {
            try
            {
                StreamReader sr = new StreamReader(path, Encoding.UTF8);
                string str = await sr.ReadToEndAsync();
                sr.Close();
                return str;
            }
            catch
            {
                return "";
            }
        }

        public static void SaveObjToFile(object obj, string path, bool prettyFormat = false)
        {
            Newtonsoft.Json.Formatting format = prettyFormat ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            string str = Newtonsoft.Json.JsonConvert.SerializeObject(obj, format);
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
            StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8);
            sw.WriteLine(str);
            sw.Close();
        }

        public static string SecondsToTimeStr(int seconds)
        {
            if (seconds < 60)
                return seconds + "秒";
            if (seconds < 3600)
                return string.Format("{0:.00}分钟", seconds / 60.0);

            return string.Format("{0:.00}小时", seconds / 3600.0);
        }

        public static string FileSizeToDownloadTime(long fileSize)
        {

            //string selfDownspeed = "";

            string selfDownspeed = Math.Round(GlobalClass.netSpeed.GetDownSpeed(), 2).ToString();
            //int secondWith200k = (int)Math.Ceiling(fileSize / 204800.0);
            //int secondWith2M = (int)Math.Ceiling(fileSize / 2097152.0);
            return $"当前下载速度为{selfDownspeed}KB/s";
        }

        public static string GetDistanceReportString(VersionDistance dst)
        {
            StringBuilder sb = new StringBuilder();

            if (dst.HasSkinUpdate)
                sb.Append("有皮肤更新，");
            if (dst.MustUpdate)
                sb.Append("有强制更新，");
            else if (dst.HasUpdate)
                sb.Append("有更新，");

            if (dst.HasUpdate)
            {
                string size = new FileSize(dst.DownloadSize).ToString();
                sb.Append($"需下载{dst.DownloadVersions}个补丁，大约{size}。\r\n。");
            }

            return sb.ToString();
        }

        public static FileOperate FileModelToFileOperate(FileModel item)
        {
            FileOperate op = new FileOperate();
            op.LocalPath = item.LocalPath;
            op.Md5 = item.Md5;
            op.Operate = ((FileOperateType)item.Operate).ToString();
            op.Result = "";
            op.nsize = item.Size;
            op.Size = new FileSize((ulong)item.Size).ToString();
            op.UpdateTime = item.UpdateTime.ToString();
            op.Url = item.Url;
            return op;
        }
    }
}
