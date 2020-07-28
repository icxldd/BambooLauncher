using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LauncherCommon;

namespace BambooLauncher.Logic
{
    public class BambooMan
    {
        public static BambooMan Instance { get; } = new BambooMan();

        /// <summary>
        /// download server base url
        /// </summary>
        public string ServerUrl;

        public HttpClient client;
        public VersionsModel LocalVerions;
        public VersionsModel ServerVerions;
        public SkinModel LocalSkin;
        public VersionDistance VersionDistance;
        public bool IsDownloading = false;
        public System.Collections.ObjectModel.ObservableCollection<FileOperate> Files = new System.Collections.ObjectModel.ObservableCollection<FileOperate>();
        public ProgressModel progress = new ProgressModel();

        public Config config = null;

        public const string BambooBalerDirName = "BambooBaler";
        public bool HasLauncherUpdate { get; set; }

        private BambooMan()
        {
            HasLauncherUpdate = false;
        }

        public void DumpConfigFile()
        {
            VersionsModel vers = new VersionsModel();
            vers.FileVers = new List<VerInfoModel>();
            vers.FileVers.Add(new VerInfoModel());
            Util.SaveObjToFile(vers, "vers.dat", true);

            SkinModel skin = new SkinModel();
            skin.Controls = new List<SkinControlInfo>();
            skin.Controls.Add(new SkinControlInfo());
            skin.WindowSize = new Int32Rect(100, 200, 720, 1280);
            skin.Title = "Bamboo Launcher";
            Util.SaveObjToFile(skin, "skin.dat", true);

            StreamWriter sw = new StreamWriter("list.txt", false, Encoding.UTF8);
            sw.WriteLine("/skin.dat,/skin.dat,size,0,md5,updatetm");
            sw.WriteLine("http://cdn.com/aaaa.uasset,/Proj/Content/Meshes/aaa.uasset,size,0,md5,updatetm");
            sw.WriteLine(",/Proj/Content/file-to-delete.uasset,0,3,,");
            sw.Close();
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
                url = ServerUrl + url;
            else
                url = ServerUrl + "/" + url;
            return url;
        }

        public async Task<string> LoadSvrFileToStr(string url)
        {
            try
            {
                var res = await client.GetAsync(GetSafeUrl(url));
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

        public async Task<string> DownloadFile(string url, string localPath)
        {
            try
            {
                string dir = Path.GetDirectoryName(localPath);
                if(string.IsNullOrEmpty(dir) == false)
                    Directory.CreateDirectory(dir);
                if(File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
                var res = await client.GetAsync(GetSafeUrl(url));
                if (res.IsSuccessStatusCode)
                {
                    var stream = await res.Content.ReadAsStreamAsync();
                    using (FileStream fs = new FileStream(localPath, FileMode.OpenOrCreate))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }
                return "ok";
            }catch(Exception ex)
            {
                return ex.Message;
            }
        }


        private async Task<bool> LoadLocalData()
        {
            LocalVerions = await Util.LoadLocalFileToObj<VersionsModel>("vers.dat");
            LocalSkin = await Util.LoadLocalFileToObj<SkinModel>("skin.dat");

            return true;
        }

        private VersionDistance CheckUpdates(VersionsModel svrVer, VersionsModel localVer)
        {
            progress.Reset();

            VersionDistance dst = new VersionDistance();
            dst.HasUpdate = false;
            dst.MustUpdate = false;
            dst.HasSkinUpdate = false;
            dst.DownloadSize = 0;
            dst.DownloadVersions = 0;
            
            //server versions is empty.
            if (svrVer == null || svrVer.FileVers == null || svrVer.FileVers.Count == 0)
            {
                progress.Status = "请求服务器数据失败";
                return dst;
            }

            //local versions is empty
            if(localVer == null || localVer.FileVers == null || localVer.FileVers.Count == 0)
            {
                dst.HasUpdate = true;
                dst.MustUpdate = true;
                dst.HasSkinUpdate = true;
                dst.DownloadVersions = 0;
                dst.DownloadSize = 0;
                foreach (var item in svrVer.FileVers)
                {
                    dst.DownloadSize += item.DownloadSize;
                    dst.DownloadVersions++;
                    if (item.ForceUpdate)
                        dst.MustUpdate = true;
                }
                progress.Status = Util.GetDistanceReportString(dst);
                return dst;
            }

            //check skin update
            dst.HasSkinUpdate = svrVer.SkinVer > localVer.SkinVer;

            //local is newer than server, this is not possible, maybe changed by user.
            VerInfoModel svrLatest = svrVer.FileVers[0];
            VerInfoModel localLatest = localVer.FileVers[0];
            if (localLatest.SerialNo >= svrLatest.SerialNo)
            {
                return dst;
            }

            dst.DownloadVersions = 0;
            dst.HasUpdate = true;
            for (int i = svrVer.FileVers.Count - 1; i >= 0; i--)
            {
                VerInfoModel ver = svrVer.FileVers[i];
                if (ver.SerialNo <= localLatest.SerialNo)
                    continue;
                dst.DownloadSize += ver.DownloadSize;
                dst.DownloadVersions++;
                if (ver.ForceUpdate)
                    dst.MustUpdate = true;
            }

            progress.Status = Util.GetDistanceReportString(dst);
            return dst;
        }

        private async Task<bool> UpdateSkin()
        {
            if (VersionDistance.HasSkinUpdate == false)
                return true;

            var skin = await LoadSvrFileToObj<SkinModel>($"{BambooBalerDirName}/skin.dat");
            if (skin == null)
                return true;

            LocalSkin = skin;
            Util.SaveObjToFile(LocalSkin, "skin.dat");

            if(string.IsNullOrEmpty(skin.Background) == false && File.Exists(skin.Background) == false)
            {
                await DownloadFile(skin.Background, skin.Background);
            }

            return true;
        }


        private async Task<bool> GetDownloadList(VersionsModel svrVers)
        {
            if (VersionDistance.HasUpdate == false || svrVers == null)
                return false;

            int startVer = 0;
            if(LocalVerions?.FileVers?.Count > 0)
            {
                startVer = LocalVerions.FileVers[0].SerialNo;
            }
            Dictionary<string, FileModel> files = new Dictionary<string, FileModel>();
            //load each version list. merge to files dictionary. new version overwrite old one.
            for (int i = svrVers.FileVers.Count - 1; i >= 0; i--)
            {
                VerInfoModel ver = svrVers.FileVers[i];
                string liststr = await LoadSvrFileToStr($"{BambooBalerDirName}/list{ver.SerialNo}.txt");
                FileList list = new FileList();
                Util.StringToFileList(liststr, list);
                if (list.Files == null)
                    continue;
                foreach (var item in list.Files)
                {
                    files[item.LocalPath] = item;
                }
            }

            //convert file list to file operate list.
            Files.Clear();
            foreach (var item in files.Values)
            {
                Files.Add(Util.FileModelToFileOperate(item));
            }

            return false;
        }
        

        public async Task<bool> Init()
        {
            if(File.Exists("launcher.dat"))
            {
                config = await Util.LoadLocalFileToObj<Config>("launcher.dat");
            }
            if(config == null || string.IsNullOrEmpty(config.ServerUrl))
            {
                config = new Config();
                config.ServerUrl = "http://127.0.0.1:4500";
                Util.SaveObjToFile(config, "launcher.dat");
            }
            ServerUrl = config.ServerUrl;

            client = new HttpClient();
            client.BaseAddress = new Uri(ServerUrl);
            client.Timeout = TimeSpan.FromSeconds(300); //5 minutes

            //load local data first.
            await LoadLocalData();

            //load server data
            ServerVerions = await LoadSvrFileToObj<VersionsModel>($"{BambooBalerDirName}/vers.dat");

            //check launcher update
            if(string.IsNullOrEmpty(ServerVerions?.LauncherMd5) == false)
            {
                string exeName = Process.GetCurrentProcess().MainModule.FileName;
                string exeDir = Path.GetDirectoryName(exeName);
                string launchermd5 = Util.Md5File(exeName);
                HasLauncherUpdate = !launchermd5.Equals(ServerVerions.LauncherMd5, StringComparison.CurrentCultureIgnoreCase);

                if (string.IsNullOrEmpty(launchermd5))
                {
                    HasLauncherUpdate = false;
                }

                if (HasLauncherUpdate)
                {
                    string oldname = "oldlauncher.exe";
                    //remove oldone.
                    if (File.Exists(oldname))
                    {
                        try
                        {
                            File.Delete(oldname);
                        }
                        catch { }
                    }
                    //rename cur exe
                    try
                    {
                        File.Move(exeName, exeDir + "/" + oldname);
                    }
                    catch { }

                    //download new file
                    await DownloadFile("BambooLauncher.exe", exeName);
                }
            }

            //check update
            VersionDistance = CheckUpdates(ServerVerions, LocalVerions);

            await UpdateSkin();

            await GetDownloadList(ServerVerions);

            return ServerVerions != null;
        }

        public async Task<bool> FullCompare()
        {
            if (LocalVerions != null)
                LocalVerions.FileVers = new List<VerInfoModel>();//clear local records.
            VersionDistance = new VersionDistance();
            VersionDistance.HasUpdate = true;
            return await GetDownloadList(ServerVerions);
        }

        public async Task<bool> Update()
        {
            bool hasAnyError = false;
            progress.Reset();
            progress.Total = Files.Count;
            long lastDownloadSize = VersionDistance.DownloadSize;

            foreach (var item in Files)
            {
                FileOperateType operate = (FileOperateType)Enum.Parse(typeof(FileOperateType), item.Operate);
                progress.CurPos++;

                string sizeStr = new FileSize(lastDownloadSize).ToString();
                //progress.Status = item.LocalPath + "\r\n总剩余 " + sizeStr + " " + Util.FileSizeToDownloadTime(lastDownloadSize);
                lastDownloadSize -= item.nsize;
                if(lastDownloadSize < 0)
                    lastDownloadSize = 0;


                //local file existed and not in force update mode. then check whether need skip.
                if (File.Exists(item.LocalPath) && operate != FileOperateType.ForceUpdate)
                {
                    if(operate == FileOperateType.Delete)
                    {
                        File.Delete(item.LocalPath);
                        item.Result = "deleted";
                        continue;
                    }

                    FileInfo fi = new FileInfo(item.LocalPath);
                    if(fi.Length == item.nsize)
                    {
                        string md5 = Util.Md5File(item.LocalPath);
                        if(md5 == item.Md5) // same as server
                        {
                            item.Result = "skiped(same)";
                            continue;
                        }
                    }

                    // changed but shoud skip changed local files.
                    if (operate == FileOperateType.SkipOnChanged)
                    {
                        item.Result = "skiped(skip on changed)";
                        continue;
                    }
                }
                progress.Status = item.LocalPath + "\r\n总剩余 " + sizeStr + " " + Util.FileSizeToDownloadTime(lastDownloadSize);
                //update the file.
                item.Result = await DownloadFile(item.Url, item.LocalPath);
                if (item.Result != "ok")
                {
                    hasAnyError = true;
                }
            }
            //update state
            if(hasAnyError)
            {
                //should treat as update failed?
            }
            LocalVerions = ServerVerions;
            Util.SaveObjToFile(LocalVerions, "vers.dat");
            IsDownloading = false;
            await Task.Delay(100);
            progress.Percent = 100;
            progress.Status = "ok";
            return true;
        }
    }
}
