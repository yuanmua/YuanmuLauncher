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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using yuanmu.Modules;

namespace yuanmu
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public string AccentColor { get; set; } = "#0078D7";
        public string BackgroundColor { get; set; } = "#3C3C3C";
        public bool RunAtStartup { get; set; }
        public string ShortcutPath { get; set; } = "";

        public SolidColorBrush AccentColorBrush =>
            (SolidColorBrush)new BrushConverter().ConvertFromString(AccentColor);

        public SolidColorBrush BackgroundColorBrush =>
            (SolidColorBrush)new BrushConverter().ConvertFromString(BackgroundColor);

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            this.DataContext = this;
        }

        private void LoadSettings()
        {
            // 从配置文件或应用设置加载
            // 这里使用简单的方式，实际项目中可以使用配置文件或注册表
            AccentColor = Properties.Settings.Default.AccentColor ?? "#0078D7";
            BackgroundColor = Properties.Settings.Default.BackgroundColor ?? "#3C3C3C";
            ShortcutPath = Properties.Settings.Default.ShortcutPath ?? "";
            RunAtStartup = IsStartupItemExists();
        }

        private void SaveSettings()
        {
            // 保存到配置文件或应用设置
            Properties.Settings.Default.AccentColor= AccentColor;
            Properties.Settings.Default.BackgroundColor= BackgroundColor;
            Properties.Settings.Default.ShortcutPath = ShortcutPath;
            Properties.Settings.Default.Save();

            SetStartup(RunAtStartup);
        }

        private void BtnSelectShortcutPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ShortcutPath = dialog.SelectedPath;
            }
        }

        private bool IsStartupItemExists()
        {
            string startupPath = Environment.GetFolderPath(
                Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupPath, "LimLauncher.lnk");
            return System.IO.File.Exists(shortcutPath);
        }

        private void SetStartup(bool enable)
        {
            string startupPath = Environment.GetFolderPath(
                Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupPath, "LimLauncher.lnk");

            if (enable)
            {
                // 创建开机启动快捷方式
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                CreateStartupShortcut(exePath, shortcutPath);
            }
            else
            {
                if (System.IO.File.Exists(shortcutPath))
                    System.IO.File.Delete(shortcutPath);
            }
        }

        private void CreateStartupShortcut(string targetPath, string shortcutPath)
        {
            try
            {
                IShellLink link = (IShellLink)new ShellLink();
                link.SetDescription("LimLauncher 启动项");
                link.SetPath(targetPath);

                // 设置工作目录为目标文件所在目录
                string workingDir = System.IO.Path.GetDirectoryName(targetPath);
                link.SetWorkingDirectory(workingDir);

                // 保存快捷方式
                //IPersistFile file = (IPersistFile)link;
                //file.Save(shortcutPath, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建启动项失败: " + ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
}