using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;
using LauncherCommon;
using System.IO;
using System.Diagnostics;

namespace BambooBaler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Logic.Baler baler = Logic.Baler.Instance;

        public MainWindow()
        {
            InitializeComponent();
            listVersions.ItemsSource = baler.Vers;
            Title = "Bamboo Barler " + Version.Ver + " by Omega at " + Version.UpdateTm;
            lblStatus.DataContext = baler.progress;
            progressBar.DataContext = baler.progress;
            chkCheckTime.ToolTip = "是否检查修改时间，默认是文件尺寸一样就认为文件没有改动，启用则文件尺寸一样时还要额外检查修改时间，修改时间也一样才认为没有改动。否则要核对文件md5来判断是否修改过，会增加时间。";
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;
            if(Directory.Exists(files[0]))
            {
                await baler.OpenProject("", files[0]);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var cfg = Logic.Config.Instance;
            string cfgResult = cfg.Load();
            if(string.IsNullOrEmpty(cfgResult) == false)
            {
                MessageBox.Show("配置文件加载失败，注意校验json格式，以及路径中反斜杠需要转义，用双反斜杠\r\n\r\n" + cfgResult);
            }
            cmbQuickSelect.ItemsSource = cfg.Projects;
            if (cfg.Projects.Count > 0)
                cmbQuickSelect.SelectedIndex = 0;
        }

        private async void BtnCheckChanges_Click(object sender, RoutedEventArgs e)
        {
            if (baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await baler.CheckChanges();

            listFiles.ItemsSource = null;
            listFiles.ItemsSource = baler.GetFileList(-1);

            if (listVersions.Items.Count > 0)
            {
                listVersions.SelectedIndex = -1;
                listVersions.SelectedIndex = 0;
            }
        }

        private void BtnConfirmUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(baler.Changes.Count == 0)
            {
                MessageBox.Show("没有发现改动，请在工作目录中做了改动后点击【检查改动】按钮后再试", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            ConfirmWindow confirm = new ConfirmWindow();
            confirm.Owner = this;
            confirm.ShowDialog();

            refreshData();
        }

        private async void BtnUpdateSkinVer_Click(object sender, RoutedEventArgs e)
        {
            if(baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await baler.CheckSkinUpdate();
        }

        private void BtnSelectLaunchTarget_Click(object sender, RoutedEventArgs e)
        {
            if (baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FileModel file = listFiles.SelectedItem as FileModel;
            if (file == null)
            {
                MessageBox.Show("请现在右侧选择一个.exe文件", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(file.LocalPath.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                MessageBox.Show("启动项必须是一个.exe文件才可以", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            baler.Version.LaunchTarget = file.LocalPath;
        }

        private void BtnResetSkinFile_Click(object sender, RoutedEventArgs e)
        {
            if (baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var res = MessageBox.Show("此操作会清空皮肤配置文件skin.dat，建议先备份。\r\n\r\n 要开始不？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;
            baler.ResetSkinFile();
            MessageBox.Show("skin.dat已经重置", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ListVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listFiles.ItemsSource = null;
            var ver = listVersions.SelectedItem as VerInfoModel;
            listFiles.ItemsSource = baler.GetFileList(ver?.SerialNo);
        }

        private void BtnMergeVers_Click(object sender, RoutedEventArgs e)
        {
            if (baler.IsProjDirValid == false)
            {
                MessageBox.Show("还未加载项目呢", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show("此操作会将所有版本合并为一个，适合在版本太多的时候提升运行效率。\r\n\r\n 要开始不？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;

            baler.MergeVers();
            refreshData();
            MessageBox.Show("已合并", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChkCheckTime_Checked(object sender, RoutedEventArgs e)
        {
            baler.checkUpdateTime = true;
        }

        private void ChkCheckTime_Unchecked(object sender, RoutedEventArgs e)
        {
            baler.checkUpdateTime = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        Logic.ProjectInfo lastServer = null;

        private async void CmbQuickSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var svr = cmbQuickSelect.SelectedItem as Logic.ProjectInfo;
            if (svr == null || svr == lastServer)
                return;
            //confirm before switch
            if(lastServer != null)
            {
                var msgres = MessageBox.Show($"确认切换项目到\r\n\r\n[{svr.Name} {svr.ProjDir}]？\r\n\r\n对比目录中的状态会被清空", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (msgres != MessageBoxResult.Yes)
                {
                    cmbQuickSelect.SelectedItem = lastServer;
                    return;
                }
            }
            txtServerUrl.Text = svr.SvrUrl;
            txtProjDir.Text = svr.ProjDir;
            await baler.OpenProject(svr.SvrUrl, svr.ProjDir);

            lastServer = svr;

            refreshData();
        }

        void refreshData()
        {
            lblSkinVer.DataContext = null;
            lblSkinVer.DataContext = baler.Version;
            lblLaunchTarget.DataContext = null;
            lblLaunchTarget.DataContext = baler.Version;

            listVersions.ItemsSource = null;
            listVersions.ItemsSource = baler.Vers;
        }

        private void BtnOpenDiffDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = Logic.Baler.DiffDirPath;
                p.Start();
            }
            catch
            {
                return;
            }
        }
    }
}
