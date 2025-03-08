using yuanmu.Entities;
using yuanmu.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TextBox = System.Windows.Controls.TextBox;
using System.Windows.Forms;
using Application = System.Windows.Application; // 明确指定使用 WPF 的 Application

namespace yuanmu
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow: MahApps.Metro.Controls.MetroWindow
    {
        private string JsonFile => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filelist.json");
        public ObservableCollection<GroupInfo> Groups { get; set; } = new ObservableCollection<GroupInfo>();
        private string OldCellValue;

        private NotifyIcon _notifyIcon;
        private bool _isExit;

        public bool MinInTaskbar { get; set; } = true;



        //private object? _draggedItem;
        //private System.Windows.Controls.DataGrid? dg_Groups;
        //private System.Windows.Controls.ScrollViewer? scv;
        //private System.Windows.Controls.Primitives.ToggleButton? tog_Topmost;


        private void LoadInfos()
        {

            try
            {
                var groups = SqliteHelper.Instance.ExecuteQuery<GroupInfo>(
                    "SELECT * FROM GroupInfo ORDER BY OrderIndex");

                foreach (var group in groups)
                {
                    // 确保OrderIndex有合法值
                    if (group.OrderIndex < 0) group.OrderIndex = 0;
                    group.LoadShortcuts();
                    Groups.Add(group);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载分组失败: {ex.Message}");
                // 可在此添加日志记录
            }


        }

        public MainWindow()
        {
            // 检查是否存在SortOrder列
            if (!ColumnExists("GroupInfo", "OrderIndex"))
            {
                SqliteHelper.Instance.ExecuteNonQuery(
                    "ALTER TABLE GroupInfo ADD COLUMN OrderIndex INTEGER DEFAULT 0");
                // 处理迁移异常
                System.Windows.MessageBox.Show("数据库升级完成");
            }
            if (!ColumnExists("ShortcutInfo", "SortOrder"))
            {
                SqliteHelper.Instance.ExecuteNonQuery(
                    "ALTER TABLE ShortcutInfo ADD COLUMN SortOrder INTEGER DEFAULT 0");
                // 处理迁移异常
                System.Windows.MessageBox.Show("数据库升级完成");
            }

            InitializeComponent();

            if (System.IO.File.Exists(JsonFile))
            {
                var groups = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<GroupInfo>>(System.IO.File.ReadAllText(JsonFile));

                foreach (var GroupInfo in groups)
                {
                    string GroupID = Guid.NewGuid().ToString();
                    GroupInfo.ID = GroupID;
                    GroupInfo.AddNewGroupToDB();

                    foreach (var ShortcutInfo in GroupInfo.ListShortcutInfo)
                    {
                        ShortcutInfo.GroupID = GroupID;
                        ShortcutInfo.ID = Guid.NewGuid().ToString();
                        ShortcutInfo.AddNewShortcutToDB();
                    }
                }
                System.IO.File.Delete(JsonFile);
            }

            LoadInfos();

            if (Groups.Count == 0)
            {
                Groups = new ObservableCollection<GroupInfo>();
                AddNewGroup("默认分组");
            }
            this.Height = Properties.Settings.Default.LastHeight;
            this.Width = Properties.Settings.Default.LastWidth;
            this.Top = Properties.Settings.Default.LastTop;
            this.Left = Properties.Settings.Default.LastLeft;
            this.Topmost = Properties.Settings.Default.TopMost;
            this.MinInTaskbar = Properties.Settings.Default.MinInTaskBar;
            tog_Topmost.IsChecked = Properties.Settings.Default.TopMost;


            this.DataContext = this;


            Loaded += (s, e) => RegisterHotkey();


        }

        #region 事件
        // 在MainWindow类中添加
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            ApplyColorSettings();
        }
        private void ApplyColorSettings()
        {
            try
            {
                // 更新颜色资源
                var accentColor = (Color)ColorConverter.ConvertFromString(
                    Properties.Settings.Default.AccentColor ?? "#0078D7");
                var backgroundColor = (Color)ColorConverter.ConvertFromString(
                    Properties.Settings.Default.BackgroundColor ?? "#3C3C3C"); 
            Application.Current.Resources["AccentColor"] = accentColor;
            Application.Current.Resources["BackgroundColor"] = backgroundColor;
            Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(backgroundColor);
            // 强制刷新所有窗口
            foreach (Window window in Application.Current.Windows)
            {
                window.InvalidateVisual();
            }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage("应用颜色设置失败: " + ex.Message);
            }
        }
        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Groups.Count > 0)
            {
                dg_Groups.SelectedIndex = 0;
            }
            MessageBoxHelper.GlobalParentWindow = this;
        }

        /// <summary>
        /// 添加分组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            AddNewGroup();
        }


        /// <summary>
        /// 滚轮穿透
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        /// <summary>
        /// 分组名编辑完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var presenter = e.EditingElement as ContentPresenter;
            var textBox = FindVisualChild<TextBox>(presenter);

            if (textBox != null && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = OldCellValue;
            }
        }

        /// <summary>
        /// 更改选中分组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                FileView.SetFileList((GroupInfo)e.AddedItems[0]);
            }
        }

        /// <summary>
        /// 分组名预存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            // 从ContentPresenter中获取TextBox
            var presenter = e.EditingElement as ContentPresenter;
            var textBox = FindVisualChild<TextBox>(presenter);
            OldCellValue = textBox?.Text ?? string.Empty;
        }
        // 新增可视化树查找辅助方法
        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T result)
                    return result;
                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MyNotifyIcon.Dispose();
            SaveSettings();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
        /// <summary>
        /// 分组名删除快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (e.Key == Key.Delete && ((System.Windows.Controls.DataGrid)sender).SelectedItem != null && !((System.Windows.Controls.DataGrid)sender).IsEditing())
            {
                e.Handled = true;
            }
        }

        private void MIDel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxHelper.ShowYesNoMessage("确定要永久性的删除此分组吗？", "删除确认") == ModernMessageBoxLib.ModernMessageboxResult.Button1)
            {
                var group = ((System.Windows.Controls.MenuItem)sender).DataContext as GroupInfo;
                group.RemoveGroupFromDB();
                Groups.Remove(group);
                if (Groups.Count == 0) FileView.SetFileList(AddNewGroup("默认分组"));
            }

        }
        #endregion

        #region 方法

        /// <summary>
        /// 添加分组
        /// </summary>
        /// <param name="defualtGroupName"></param>
        /// <returns></returns>
        private GroupInfo AddNewGroup(string defualtGroupName = "新分组")
        {
            //GroupInfo GroupNew = new GroupInfo() { GroupName = defualtGroupName };
            GroupInfo GroupNew = new GroupInfo()
            {
                GroupName = defualtGroupName,
                OrderIndex = Groups.Count // 设置初始排序值为当前最后位置
            };

            GroupNew.AddNewGroupToDB();
            Groups.Add(GroupNew);
            dg_Groups.SelectedIndex = dg_Groups.Items.Count - 1;
            scv.ScrollToEnd();
            return GroupNew;
        }

        /// <summary>
        /// 保存分组
        /// </summary>
        //private void Save()
        //{
        //    string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(Groups);
        //    System.IO.File.WriteAllText(JsonFile, strJson);
        //}

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveSettings()
        {
            Properties.Settings.Default.LastHeight = this.Height;
            Properties.Settings.Default.LastWidth = this.Width;
            Properties.Settings.Default.TopMost = this.Topmost;
            Properties.Settings.Default.LastTop = this.Top;
            Properties.Settings.Default.LastLeft = this.Left;
            Properties.Settings.Default.TopMost = this.Topmost;
            Properties.Settings.Default.MinInTaskBar = this.MinInTaskbar;
            Properties.Settings.Default.Save();
        }

        #endregion

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            (((FrameworkElement)sender).DataContext as ShortcutInfo).StartFile();
        }

        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.Activate();
            }
        }

        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            this.ShowInTaskbar = this.WindowState != WindowState.Minimized || MinInTaskbar;
        }

        private void FileView_Loaded(object sender, RoutedEventArgs e)
        {

        }


        private void InitTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("./icon.ico"), // 替换为你的图标
                Visible = true,
                Text = "yuanmu"
            };
            _notifyIcon.MouseDoubleClick += (s, e) => ShowWindow();
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("打开", null, (s, e) => ShowWindow());
            _notifyIcon.ContextMenuStrip.Items.Add("退出", null, (s, e) => ExitApplication());
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _isExit = true;
            //_notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExit)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                //_notifyIcon.Dispose();
            }
        }

        private void RegisterHotkey()
        {
            // 初始化热键管理器
            HotkeyManager.Initialize(this);

            // 注册显示窗口的快捷键
            HotkeyManager.RegisterHotkey("ShowWindow", Key.W, ModifierKeys.Alt, ShowWindow);
            // 注册隐藏窗口的快捷键
            HotkeyManager.RegisterHotkey("HideWindow", Key.S, ModifierKeys.Alt, Hide);
        }


        // // 注册新快捷键
        // HotkeyManager.RegisterHotkey("ShowWindow", Key.W, ModifierKeys.Alt, ShowWindow);

        // // 更新已存在的快捷键
        // HotkeyManager.UpdateHotkey("ShowWindow", Key.T, ModifierKeys.Control);

        // // 注销快捷键
        // HotkeyManager.UnregisterHotkey("ShowWindow");

        // // 获取所有已注册的快捷键
        // var hotkeys = HotkeyManager.GetAllHotkeys();











        // 在窗口类中添加成员变量，用于记录拖拽起点和拖动项
        private Point _startPoint;
        private object _draggedItem;

        private void dg_Groups_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            // 尝试获取被点击的 DataGridRow
            DataGridRow row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row != null)
            {
                _draggedItem = row.Item;
            }
        }

        private void dg_Groups_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
            {
                Point currentPos = e.GetPosition(null);
                if (Math.Abs(currentPos.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPos.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // 发起拖放操作
                    DragDrop.DoDragDrop(dg_Groups, _draggedItem, System.Windows.DragDropEffects.Move);
                }
            }
        }

        private void dg_Groups_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var droppedData = e.Data.GetData(typeof(GroupInfo)) as GroupInfo;
            if (droppedData == null)
                return;

            // 获取目标项所在的行
            DataGridRow targetRow = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (targetRow == null)
                return;

            var targetData = targetRow.Item as GroupInfo;
            if (targetData == null || droppedData.Equals(targetData))
                return;

            // 假设 Groups 是绑定到 DataGrid 的 ObservableCollection<GroupInfo>
            var groups = dg_Groups.ItemsSource as ObservableCollection<GroupInfo>;
            if (groups != null)
            {
                int oldIndex = groups.IndexOf(droppedData);
                int newIndex = groups.IndexOf(targetData);

                if (oldIndex != newIndex)
                {
                    groups.Move(oldIndex, newIndex);
                    UpdateAllGroupOrders(); // 新增：更新所有分组顺序
                }
            }
        }
        // 新增：更新全部分组顺序到数据库
        private void UpdateAllGroupOrders()
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                var group = Groups[i];
                group.OrderIndex = i;
                group.UpdateOrderIndexToDB();
            }
        }

        // 辅助方法：查找 VisualTree 中的指定类型父元素
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }
        private bool ColumnExists(string tableName, string columnName)
        {
            try
            {
                var sql = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
                var result = SqliteHelper.Instance.ExecuteScalar(sql);
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception)
            {
                // 如果查询失败，为了安全起见返回 true，避免重复添加列
                return true;
            }

        }




    }

}
