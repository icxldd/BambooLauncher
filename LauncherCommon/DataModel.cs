using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LauncherCommon
{
    public class VerInfoModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        int serial, changecount;
        long downloadsize;
        string title, detail, urlbase;
        DateTime updatetime;
        bool forceupdate;

        public int SerialNo { get { return serial; } set { serial = value; notify("SerialNo"); } }
        public string Title { get { return title; } set { title = value; notify("Title"); } }
        public string Detail { get { return detail; } set { detail = value; notify("Detail"); } }
        public DateTime UpdateTime { get { return updatetime; } set { updatetime = value; notify("UpdateTime"); } }
        public bool ForceUpdate { get { return forceupdate; } set { forceupdate = value; notify("ForceUpdate"); } }
        public int ChangeCount { get { return changecount; } set { changecount = value; notify("ChangeCount"); } }
        public long DownloadSize { get { return downloadsize; } set { downloadsize = value; notify("DownloadSize"); notify("DownloadSizeStr"); } }
        public string UrlBase { get { return urlbase; } set { urlbase = value; notify("UrlBase"); } }

        [JsonIgnore]
        public string DownloadSizeStr { get { return new FileSize(DownloadSize).ToString(); } }
    }

    public class VersionsModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        int skinver;
        string launchTarget;
        string launcherMd5;

        public int SkinVer { get { return skinver; } set { skinver = value; notify("SkinVer"); } }
        public string LaunchTarget { get { return launchTarget; } set { launchTarget = value; notify("LaunchTarget"); } }
        public string LauncherMd5 { get { return launcherMd5; } set { launcherMd5 = value; notify("LauncherMd5"); } }
        public List<VerInfoModel> FileVers { get; set; }
    }

    public class VersionDistance
    {
        public bool HasUpdate { get; set; }
        public bool MustUpdate { get; set; }
        public bool HasSkinUpdate { get; set; }
        public int DownloadVersions { get; set; }
        public long DownloadSize { get; set; }
    }

    //------------------------------------------------------------------------------------------

    public struct Int32Rect : IFormattable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Int32Rect(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public override string ToString()
        {
            return $"{X},{Y},{Width},{Height}";
        }
        public static Int32Rect Parse(string source)
        {
            Int32Rect rect = new Int32Rect();
            if (string.IsNullOrEmpty(source))
                return rect;
            char[] spliter = { ',' };
            var ss = source.Split(spliter);
            if (ss?.Length != 4)
                return rect;
            int n;
            int.TryParse(ss[0], out n); rect.X = n;
            int.TryParse(ss[1], out n); rect.Y = n;
            int.TryParse(ss[2], out n); rect.Width = n;
            int.TryParse(ss[3], out n); rect.Height = n;
            return rect;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }
    }

    public class SkinControlInfo
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public int ZOrder { get; set; }
        public Int32Rect Rect { get; set; }
        public Dictionary<string, string> Props { get; set; }
    }

    public class SkinModel
    {
        public string SkinName { get; set; }
        public int SkinVer { get; set; }
        public string Title { get; set; }
        public string Background { get; set; }
        public Int32Rect WindowSize { get; set; }
        public List<SkinControlInfo> Controls { get; set; }
    }

    //------------------------------------------------------------------------------------------
    //file list model.
    //csv file
    //url, localpath, size, operate, md5, updatetime

    public enum FileOperateType
    {
        UpdateOnChanged,
        ForceUpdate,
        SkipOnChanged,
        Delete
    }


    public class FileModel
    {
        public string Url { get; set; }
        public string LocalPath { get; set; }
        public string Md5 { get; set; }
        public long Size { get; set; }
        public DateTime UpdateTime { get; set; }
        public int Operate { get; set; }

        [JsonIgnore]
        public string OperateStr { get; set; }
        [JsonIgnore]
        public string SizeStr { get { return new FileSize(Size).ToString(); } }

        public void SyncOperateStr()
        {
            OperateStr = ((FileOperateType)Operate).ToString();
        }
    }

    public class FileList
    {
        public string Name { get; set; }
        public List<FileModel> Files { get; set; }
    }

    public class FileOperate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        string url, localpath, md5, result, size, updatetm, operate;
        public long nsize;

        public string Url { get { return url; } set { url = value; notify("Url"); } }
        public string LocalPath { get { return localpath; } set { localpath = value; notify("LocalPath"); } }
        public string Md5 { get { return md5; } set { md5 = value; notify("Md5"); } }
        public string Size { get { return size; } set { size = value; notify("Size"); } }
        public string UpdateTime { get { return updatetm; } set { updatetm = value; notify("UpdateTime"); } }
        public string Operate { get { return operate; } set { operate = value; notify("Operate"); } }
        public string Result { get { return result; } set { result = value; notify("Result"); } }
    }

    public class ProgressModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        int percent;
        string status;
        long total, cur;
        bool _IsShowQiDong = false, _IsShowProgress = false;


        public int Percent { get { return percent; } set { percent = value; notify("Percent"); } }
        public string Status { get { return status; } set { status = value; notify("Status"); } }

        public bool IsShowQiDong { get { return _IsShowQiDong; } set { _IsShowQiDong = value; notify("IsShowQiDong"); } }

        public bool IsShowProgress { get { return _IsShowProgress; } set { _IsShowProgress = value; notify("IsShowProgress"); } }

        public long Total { get { return total; } set { total = value; Percent = total == 0 ? 0 : (int)(cur * 100 / total); notify("Total"); } }
        public long CurPos { get { return cur; } set { cur = value; Percent = total == 0 ? 0 : (int)(cur * 100 / total); notify("CurPos"); } }

        public void Reset()
        {
            total = 0;
            cur = 0;
            Percent = 0;
            Status = "";

        }
    }
}
