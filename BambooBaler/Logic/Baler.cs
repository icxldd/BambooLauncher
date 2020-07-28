using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LauncherCommon;

namespace BambooBaler.Logic
{
    public class Baler
    {
        public static Baler Instance { get; } = new Baler();

        public VersionsModel Version = new VersionsModel();
        public Dictionary<string, FileModel> StagedVersFiles = new Dictionary<string, FileModel>();
        public List<FileModel> Changes = new List<FileModel>();
        public VerInfoModel ChangesVer = new VerInfoModel();
        public List<VerInfoModel> Vers = new List<VerInfoModel>();

        public ProjectInfo CurServer = new ProjectInfo();

        //serial vs its file list
        public Dictionary<int, List<FileModel>> VersFileListMap = new Dictionary<int, List<FileModel>>();

        public const string BambooBalerDirName = "BambooBaler";

        public bool checkUpdateTime = false;

        public ProgressModel progress = new ProgressModel();

        public const string DiffDirPath = "diffdir";

        HttpClient http = null;

        Baler()
        {
            ChangesVer.SerialNo = -1;
            ChangesVer.Title = "新改动";
            progress.Status = "ok";
        }

        public bool IsProjDirValid
        {
            get
            {
                if (string.IsNullOrEmpty(CurServer?.ProjDir))
                    return false;
                return Directory.Exists(CurServer.ProjDir);
            }
        }
        

        public List<FileModel> GetFileList(int? serial)
        {
            if (serial == null)
                return null;
            int nserial = (int)serial;
            if (VersFileListMap.ContainsKey(nserial))
                return VersFileListMap[nserial];
            return null;
        }

        public bool IsLocalProject { get { return string.IsNullOrEmpty(CurServer?.SvrUrl); } }

        public async Task<int> OpenProject(string serverUrl, string projDir)
        {
            Cleanup();

            //normalize path format. d:/aaa/a/. all / and end with /
            projDir = projDir.Replace('\\', '/');
            if (projDir.EndsWith("/") == false)
                projDir = projDir + "/";

            CurServer.SvrUrl = serverUrl;
            CurServer.ProjDir = projDir;
            await LoadProjDir(CurServer.ProjDir);
            return 0;
        }

        void Cleanup()
        {
            if(Directory.Exists(DiffDirPath))
            {
                Directory.Delete(DiffDirPath, true);
            }
            Directory.CreateDirectory(DiffDirPath);
        }

        string GetFullPath(string path)
        {
            if (path.StartsWith(CurServer.ProjDir, StringComparison.CurrentCultureIgnoreCase))
                return path;
            return CurServer.ProjDir + path;
        }

        string GetLocalPath(string path)
        {
            path = path.Replace('\\', '/');
            if (path.StartsWith(CurServer.ProjDir, StringComparison.CurrentCultureIgnoreCase))
                return path.Substring(CurServer.ProjDir.Length);
            if (path.StartsWith("/"))
                return path.Substring(1);
            return path;
        }

        public string GetSafeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            if (url.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                return url;
            }
            if (url.StartsWith("/"))
                url = CurServer.SvrUrl + url;
            else
                url = CurServer.SvrUrl + "/" + url;
            return url;
        }

        public async Task<string> LoadSvrFileToStr(string url)
        {
            try
            {
                var res = await http.GetAsync(GetSafeUrl(url));
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsStringAsync();
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public async Task<T> LoadSvrFileToObj<T>(string url)
        {
            try
            {
                string str = await LoadSvrFileToStr(url);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
            }
            catch
            {
                return default(T);
            }
        }

        public async Task<string> LoadProjectFile(string localPathOrUrl)
        {
            if(IsLocalProject)
            {
                return await Util.LoadLocalFileToString(GetFullPath(localPathOrUrl));
            }
            else
            {
                return await LoadSvrFileToStr(localPathOrUrl);
            }
        }

        public async Task<T> LoadProjectFile<T>(string path) where T : class
        {
            T value = null;
            string json = await LoadProjectFile(path);
            value = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return value;
        }

        public async Task<bool> LoadProjDir(string dir)
        {
            StagedVersFiles.Clear();
            Vers.Clear();
            Vers.Add(ChangesVer);
            VersFileListMap.Clear();
            VersFileListMap[ChangesVer.SerialNo] = Changes;
            Changes.Clear();
            ChangesVer.ChangeCount = 0;
            ChangesVer.DownloadSize = 0;

            Version = await LoadProjectFile<VersionsModel>($"{BambooBalerDirName}/vers.dat");
            if (Version == null || Version.FileVers == null)
            {
                Version = new VersionsModel();
                Version.FileVers = new List<VerInfoModel>();
                return false;
            }
            //load files from low version to high version by overwrite.
            for (int i = Version.FileVers.Count - 1; i >= 0; i--)
            {
                int serial = Version.FileVers[i].SerialNo;
                string path = $"{BambooBalerDirName}/list{serial}.txt";
                Vers.Add(Version.FileVers[i]);
                string liststr = await LoadProjectFile(path);
                FileList list = new FileList();
                Util.StringToFileList(liststr, list);
                VersFileListMap[serial] = list.Files;
                foreach (var file in list.Files)
                {
                    file.SyncOperateStr();
                    if ((FileOperateType)file.Operate == FileOperateType.Delete)
                    {
                        StagedVersFiles.Remove(file.LocalPath);
                        continue;
                    }
                    StagedVersFiles[file.LocalPath] = file;
                }
            }
            return true;
        }

        void CheckDirChanges(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            string[] dirs = Directory.GetDirectories(dir);

            if (GetLocalPath(dir) == BambooBalerDirName)
                return; //skip baler dir.

            foreach (var file in files)
            {
                string local = GetLocalPath(file);
                FileModel fm = null;

                progress.CurPos++;
                progress.Status = local;

                if (StagedVersFiles.ContainsKey(local))
                {
                    fm = StagedVersFiles[local];
                }

                FileInfo fi = new FileInfo(file);

                if(fm != null && fi.Length == fm.Size)
                {
                    if(checkUpdateTime == false || fi.LastWriteTime == fm.UpdateTime)
                        continue; //file not changed.
                }

                FileModel op = new FileModel();
                op.LocalPath = local;
                op.Size = fi.Length;
                op.UpdateTime = fi.LastWriteTime;
                op.Md5 = Util.Md5File(file);
                op.Operate = (int)FileOperateType.UpdateOnChanged;
                //not found in record. so its a new add file.
                if (fm == null)
                {
                    op.OperateStr = "new";
                    Changes.Add(op);
                    continue;
                }

                if(op.Md5.Equals(fm.Md5, StringComparison.CurrentCultureIgnoreCase) == false)
                { 
                    op.OperateStr = "modify";
                    Changes.Add(op);
                }
            }

            foreach (var subdir in dirs)
            {
                CheckDirChanges(subdir);
            }
        }

        int GetDirFiles(string dir)
        {
            int count = Directory.GetFiles(dir).Length;
            string[] dirs = Directory.GetDirectories(dir);
            foreach (var item in dirs)
            {
                count += GetDirFiles(item);
            }
            return count;
        }

        void CheckThread()
        {
            //compare from files
            CheckDirChanges(CurServer.ProjDir);

            //compare from record. check deleted files
            foreach (var item in StagedVersFiles.Values)
            {
                if (File.Exists(GetFullPath(item.LocalPath)) == false)
                {
                    FileModel op = new FileModel();
                    op.LocalPath = item.LocalPath;
                    op.Size = item.Size;
                    op.Md5 = item.Md5;
                    op.Operate = (int)FileOperateType.Delete;
                    op.UpdateTime = DateTime.Now;
                    op.SyncOperateStr();
                    Changes.Add(op);
                }
            }

            ChangesVer.ChangeCount = Changes.Count;
            long downloadsize = 0;
            foreach (var item in Changes)
            {
                downloadsize += item.Size;
            }
            ChangesVer.DownloadSize = downloadsize;
            ChangesVer.UpdateTime = DateTime.Now;
            checkThreadWorking = false;
            progress.Percent = 100;
            progress.Status = "ok";
        }

        bool checkThreadWorking = false;
        public async Task<bool> CheckChanges()
        {
            if (IsProjDirValid == false)
                return false;

            if (checkThreadWorking)
                return false;

            Changes.Clear();
            progress.Reset();
            progress.Total = GetDirFiles(CurServer.ProjDir);

            checkThreadWorking = true;
            Action checkaction = CheckThread;
            await Task.Run(checkaction);            
            return true;
        }
        
        public async Task<bool> CheckSkinUpdate()
        {
            var skin = await Util.LoadLocalFileToObj<SkinModel>(GetFullPath($"{BambooBalerDirName}/skin.dat"));
            if (skin == null)
            {
                Version.SkinVer = 0;
                return false;
            }
            Version.SkinVer = skin.SkinVer;
            return true;
        }

        public async Task<bool> ConfirmChanges(string verTitle, string verDetail, bool forceUpdate, string urlbase)
        {
            await CheckSkinUpdate();

            if (urlbase == null)
                urlbase = "";
            if (urlbase.Length > 0 && urlbase.EndsWith("/") == false)
                urlbase = urlbase + "/";

            int newserial = 0;
            if(Version.FileVers?.Count > 0)
            {
                newserial = Version.FileVers[0].SerialNo + 1;
            }

            //make sure baler dir existed.
            if(Directory.Exists(GetFullPath(BambooBalerDirName)) == false)
            {
                Directory.CreateDirectory(GetFullPath(BambooBalerDirName));
            }

            FileList list = new FileList();
            list.Files = Changes;
            foreach (var item in Changes)
            {
                item.Url = urlbase + item.LocalPath;
            }
            var liststr = Util.FileListToString(list);
            StreamWriter sw = new StreamWriter(GetFullPath($"{BambooBalerDirName}/list{newserial}.txt"), false, Encoding.UTF8);
            sw.WriteLine(liststr);
            sw.Close();

            VersionsModel data = new VersionsModel();
            data.SkinVer = Version.SkinVer;
            data.LaunchTarget = Version.LaunchTarget;
            data.FileVers = Version.FileVers;
            data.LauncherMd5 = Util.Md5File(GetFullPath("BambooLauncher.exe"));
            VerInfoModel newver = new VerInfoModel();
            newver.SerialNo = newserial;
            newver.Title = verTitle;
            newver.Detail = verDetail;
            newver.ForceUpdate = forceUpdate;
            newver.ChangeCount = ChangesVer.ChangeCount;
            newver.DownloadSize = ChangesVer.DownloadSize;
            newver.UrlBase = urlbase;
            newver.UpdateTime = DateTime.Now;
            data.FileVers.Insert(0, newver);
            Util.SaveObjToFile(data, GetFullPath($"{BambooBalerDirName}/vers.dat"));

            Vers.Add(newver);

            Changes.Clear();
            ChangesVer.ChangeCount = 0;
            ChangesVer.DownloadSize = 0;

            return true;
        }

        public void ResetSkinFile()
        {
            SkinModel skin = new SkinModel();
            skin.Controls = new List<SkinControlInfo>();
            skin.Controls.Add(new SkinControlInfo());
            skin.WindowSize = new Int32Rect(100, 200, 720, 1280);
            skin.Title = "Bamboo Launcher";
            skin.SkinVer = 0;
            Util.SaveObjToFile(skin, GetFullPath($"{BambooBalerDirName}/skin.dat"), true);

            Version.SkinVer = 0;
        }

        public void MergeVers()
        {
            if (Version.FileVers == null || Version.FileVers.Count == 0)
                return;
            int serial = Version.FileVers[0].SerialNo + 1;
            string urlbase = Version.FileVers[0].UrlBase;
            if (urlbase == null)
                urlbase = "";
            if (urlbase.Length > 0 && urlbase.EndsWith("/") == false)
                urlbase = urlbase + "/";

            long downloadsize = 0;
            FileList fl = new FileList();
            fl.Files = new List<FileModel>();
            foreach (var item in StagedVersFiles.Values)
            {
                if(string.IsNullOrEmpty(item.Url) || item.LocalPath.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) == false)
                    item.Url = urlbase + item.LocalPath;
                downloadsize += item.Size;
                fl.Files.Add(item);
            }
            var listdata = Util.FileListToString(fl);
            StreamWriter sw = new StreamWriter(GetFullPath($"{BambooBalerDirName}/list{serial}.txt"), false, Encoding.UTF8);
            sw.WriteLine(listdata);
            sw.Close();
            foreach (var item in Version.FileVers)
            {
                File.Delete(GetFullPath($"{BambooBalerDirName}/list{item.SerialNo}.txt"));
            }
            Version.FileVers.Clear();
            Version.FileVers.Add(ChangesVer);

            VerInfoModel merged = new VerInfoModel();
            merged.SerialNo = serial;
            merged.Title = "merged version";
            merged.UpdateTime = DateTime.Now;
            merged.ChangeCount = StagedVersFiles.Count;
            merged.DownloadSize = downloadsize;

            VersionsModel data = new VersionsModel();
            data.SkinVer = Version.SkinVer;
            data.LaunchTarget = Version.LaunchTarget;
            data.FileVers = new List<VerInfoModel>();
            data.FileVers.Add(merged);
            Util.SaveObjToFile(data, GetFullPath($"{BambooBalerDirName}/vers.dat"));
        }
    }
}
