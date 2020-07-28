using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using BambooLauncher.Logic;
using LauncherCommon;

namespace BambooLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BambooMan man = BambooMan.Instance;
        bool isRunning = false;
        public ICommand ClickCommand { get; set; }

        public ICommand CloseWindowCommand { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (o, e) => { DragMove(); };
            SetTitle("绘境更新器");
            this.DataContext = this;

            ClickCommand = new RelayCommand(o => true, o =>
           {
               LaunchTargetAndQuit();
                // do something
            });

            CloseWindowCommand = new RelayCommand(o => true, o =>
            {
                System.Environment.Exit(0);
                // do something
            });

        }

        void SetTitle(string title)
        {
            Title = title + " v" + Version.Ver + "-" + Version.UpdateTm;
        }


        void initback()
        {

            //ImageBrush brush = new ImageBrush();
            //Uri bgUri = new Uri("./BambooBaler/bg.jpg", UriKind.RelativeOrAbsolute);
            //brush.ImageSource = new BitmapImage(bgUri);
            //Background = brush;
        }
        void CheckProcess()
        {
            isRunning = false;
            if (man.LocalVerions == null || string.IsNullOrEmpty(man.LocalVerions.LaunchTarget))
                return;
            var pp = Process.GetProcesses();
            string target = man.LocalVerions.LaunchTarget.Replace('\\', '/');
            foreach (var item in pp)
            {
                try
                {
                    //skip background item
                    if (item.MainWindowHandle == IntPtr.Zero)
                        continue;
                    if (item.ProcessName == "Taskmgr")
                        continue;
                    string processPath = item.MainModule.FileName.Replace('\\', '/');
                    if (processPath.EndsWith(target))
                    {
                        isRunning = true;
                    }
                }
                catch
                {
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //man.DumpConfigFile();
            status.DataContext = man.progress;
            progress.DataContext = man.progress;
            b.DataContext = man.progress;
            cc.DataContext = man.progress;
            listFiles.ItemsSource = man.Files;



            //init checks
            bool initOk = await man.Init();

            //apply skin
            await ApplySkin();
            //load notice html
            browser.Navigate($"{man.ServerUrl}/{BambooMan.BambooBalerDirName}/index.html");

            //update button state 

            //btnUpdate.IsEnabled = man.VersionDistance.HasUpdate ? true : false;
            //btnLaunch.IsEnabled = man.VersionDistance.MustUpdate ? false : true;

            if (man.VersionDistance.MustUpdate)
            {
                //btnLaunch.ToolTip = "有强制更新的补丁，请更新后再启动客户端";
                //btnLaunch.IsEnabled = false;
                man.progress.IsShowQiDong = false;
                man.progress.IsShowProgress = true;
                await Task.Delay(300);
                BtnUpdate_Click(null, null);
            }
            else
            {
                man.progress.IsShowQiDong = true;
                man.progress.IsShowProgress = false;
            }

            //launch client directly if no update.
            if (man.VersionDistance.HasUpdate == false)
            {
                //LaunchTargetAndQuit();
            }

            CheckProcess();
        }

        private async Task<bool> ApplySkin()
        {
            SkinModel skin = man.LocalSkin;
            if (skin == null)
                return false;

            const int minWidth = 300;
            const int minHeight = 300;
            var windowSize = skin.WindowSize;

            if (windowSize.Width < minWidth)
                windowSize.Width = minWidth;
            if (windowSize.Height < minHeight)
                windowSize.Height = minHeight;

            if (windowSize.Width > SystemParameters.WorkArea.Width)
                windowSize.Width = (int)SystemParameters.WorkArea.Width;
            if (windowSize.Height > SystemParameters.WorkArea.Height)
                windowSize.Height = (int)SystemParameters.WorkArea.Height;

            Width = windowSize.Width;
            Height = windowSize.Height;
            SetTitle(skin.Title);

            if (string.IsNullOrEmpty(skin.Background) == false)
            {
                try
                {
                    string localname = skin.Background;
                    try
                    {
                        if (File.Exists(skin.Background) == false)
                        {
                            if (localname.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                            {
                                localname = "bg.jpg";
                            }
                            await man.DownloadFile(skin.Background, localname);
                        }
                    }
                    catch { }
                    //ImageBrush brush = new ImageBrush();
                    //Uri bgUri = new Uri(localname, UriKind.RelativeOrAbsolute);
                    //brush.ImageSource = new BitmapImage(bgUri);
                    //Background = brush;
                }
                catch
                { }
            }

            await Task.Delay(10);

            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (man.IsDownloading &&
                MessageBox.Show("are you sure?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        private void LaunchTargetAndQuit()
        {
            if (string.IsNullOrEmpty(man.LocalVerions?.LaunchTarget) || File.Exists(man.LocalVerions?.LaunchTarget) == false)
                return;

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = System.IO.Path.GetFullPath(man.LocalVerions?.LaunchTarget);
                p.StartInfo.Arguments = "-ThisIsBambooLauncher -NoVerifyGC";
                p.Start();
            }
            catch
            {
                return;
            }
            this.Close();
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            CheckProcess();
            if (man.IsDownloading)
            {
                MessageBox.Show("更新中无法启动，请等待更新完毕。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(man.LocalVerions.LaunchTarget) || File.Exists(man.LocalVerions.LaunchTarget) == false)
            {
                MessageBox.Show("没有找到启动文件，请尝试更新或者修复一下。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (isRunning)
            {
                MessageBox.Show("已经在运行了，不建议多开。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LaunchTargetAndQuit();
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckProcess();
            if (isRunning)
            {
                MessageBox.Show("客户端运行中，建议关闭后再更新。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool bOk = await man.Update();
            if (bOk)
            {
                btnLaunch.IsEnabled = true;
                MessageBox.Show("更新成功！请点击启动按钮启动客户端", "", MessageBoxButton.OK, MessageBoxImage.Information);
                man.progress.IsShowQiDong = true;
                //LaunchTargetAndQuit();
            }
            else
            {
                MessageBox.Show("更新失败，请查看日志文件以获取更多信息", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnRepair_Click(object sender, RoutedEventArgs e)
        {
            //btnUpdate.IsEnabled = true;
            var res = MessageBox.Show("修复检查需要较长时间，并且根据网速会有较长时间的下载过程，请预留足够多的时间再开始。\r\n\r\n 要开始不？", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;
            await man.FullCompare();

            tabContent.Visibility = Visibility.Visible;
            tabContent.SelectedIndex = 1;
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (tabContent.Visibility == Visibility.Collapsed)
            {
                tabContent.Visibility = Visibility.Visible;
                tabContent.SelectedIndex = 1;
            }
            else
            {
                if (tabContent.SelectedIndex != 1)
                    tabContent.SelectedIndex = 1;
                else
                    tabContent.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnNotice_Click(object sender, RoutedEventArgs e)
        {
            if (tabContent.Visibility == Visibility.Collapsed)
            {
                tabContent.Visibility = Visibility.Visible;
                tabContent.SelectedIndex = 0;
            }
            else
            {
                if (tabContent.SelectedIndex != 0)
                    tabContent.SelectedIndex = 0;
                else
                    tabContent.Visibility = Visibility.Collapsed;
            }
        }
    }
}
