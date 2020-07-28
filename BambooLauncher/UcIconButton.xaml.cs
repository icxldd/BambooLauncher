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

namespace BambooLauncher
{
    /// <summary>
    /// Interaction logic for UcIconButton.xaml
    /// </summary>
    public partial class UcIconButton : UserControl
    {
        public UcIconButton()
        {
            InitializeComponent();
        }

        static UcIconButton()
        {
            // 3. 注册定义的依赖属性
            pathDataProperty = DependencyProperty.Register("PathData", typeof(string), typeof(UcIconButton),
                new PropertyMetadata("Icon svg path data", OnValueChanged));
        }

        public event RoutedEventHandler Click;
        public static readonly DependencyProperty pathDataProperty;

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        public string PathData
        {
            get { return (string)GetValue(pathDataProperty); }
            set { SetValue(pathDataProperty, value); rect.Data = Geometry.Parse(value); }
        }

        private static void OnValueChanged(DependencyObject dpobj, DependencyPropertyChangedEventArgs e)
        {
            // 当只发生改变时回调的方法
        }

        private void BtnLaunch_Loaded(object sender, RoutedEventArgs e)
        {
            rect.Data = Geometry.Parse(PathData);
        }
    }
}
