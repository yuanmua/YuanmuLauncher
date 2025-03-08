using yuanmu.Entities;
using yuanmu.Modules;
using ModernMessageBoxLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
using System.Drawing;
using System.Windows.Interop;
using Point = System.Windows.Point;
using System.Collections.Specialized;


namespace yuanmu
{
    /// <summary>
    /// FileListView.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class FileListView : UserControl
    {
        public ObservableCollection<ShortcutInfo> Files { get; set; }

        public GroupInfo GroupInfo { get; set; }

        // 拖拽相关字段
        private System.Windows.Point startPoint;
        private bool isDragging;
        private ShortcutInfo draggedItem;


        public FileListView()
        {
            InitializeComponent();
            this.DataContext = this;

            //lbFiles.PreviewMouseMove += ListBox_MouseMove;


            // 添加以下两行确保事件路由
            lbFiles.AddHandler(ListBoxItem.PreviewDragOverEvent, new DragEventHandler(ListBoxItem_PreviewDragOver), true);
            lbFiles.AddHandler(ListBoxItem.DropEvent, new DragEventHandler(LbFiles_Drop), true);
        }
        // 新增事件处理方法
        private void ListBoxItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = false; // 允许事件继续传递
        }

        #region 事件
        /// <summary>
        /// 双击打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (((ListBoxItem)sender).Content as ShortcutInfo).StartFile();
        }

        /// <summary>
        /// 切换大图标模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MIBigIcon_Click(object sender, RoutedEventArgs e)
        {
            lbFiles.Style = (Style)this.FindResource("ListBoxBigIcon");
        }

        /// <summary>
        /// 切换列表模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MIStretching_Click(object sender, RoutedEventArgs e)
        {
            lbFiles.Style = (Style)this.FindResource("ListBoxStretching");
        }

        /// <summary>
        /// 文件拖拽
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbFiles_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (GroupInfo != null)
                {
                    foreach (string fileNameBase in ((System.Array)e.Data.GetData(DataFormats.FileDrop)))
                    {
                        string filePath = fileNameBase;
                        string fileReName = null;
                        if (System.IO.Path.GetExtension(filePath) == ".lnk")
                        {
                            IShellLink vShellLink = (IShellLink)new ShellLink();
                            IPersistFile vPersistFile = vShellLink as IPersistFile;
                            vPersistFile.Load(filePath, 0);
                            StringBuilder vStringBuilder = new StringBuilder(260);
                            vShellLink.GetPath(vStringBuilder, vStringBuilder.Capacity, out WIN32_FIND_DATA vWIN32_FIND_DATA, SLGP_FLAGS.SLGP_RAWPATH);
                            filePath = vStringBuilder.ToString();
                            fileReName = System.IO.Path.GetFileNameWithoutExtension(fileNameBase);
                        }
                        if (Files.Where(t => t.FileFullPath == filePath).Count() == 0)
                        {
                            var shortinfo = new ShortcutInfo()
                            {
                                GroupID = GroupInfo.ID,
                                FileFullPath = filePath,
                                FileRename = fileReName,
                            };
                            shortinfo.AddNewShortcutToDB();
                            Files.Add(shortinfo);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
            finally
            {
                e.Handled = true; // 确保标记事件已处理
            }

        }

        /// <summary>
        /// 拖拽完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbFiles_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                e.Effects = DragDropEffects.None;
                //e.Effects = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop) && GroupInfo != null)
                    e.Effects = DragDropEffects.Link;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
            e.Handled = true;
        }

        /// <summary>
        /// 快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (((ListBox)sender).SelectedItem != null)
                {
                    if (e.Key == Key.Delete)
                    {
                        e.Handled = true;
                    }
                    else if (e.Key == Key.F2)
                    {
                        Rename(((ListBox)sender).SelectedValue as ShortcutInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MIDelFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<ShortcutInfo> toBeDelete = new List<ShortcutInfo>();
                foreach (ShortcutInfo File in lbFiles.SelectedItems)
                {
                    toBeDelete.Add(File);
                    File.RemoveShortcutFromDB();
                }

                toBeDelete.ForEach(File => Files.Remove(File));

            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MIOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (ShortcutInfo File in lbFiles.SelectedItems)
                {
                    File.StartFile();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
        }
        private void MIRename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Rename((sender as MenuItem).DataContext as ShortcutInfo);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 重置文件列表
        /// </summary>
        /// <param name="group"></param>
        public void SetFileList(GroupInfo group)
        {
            this.GroupInfo = group;
            this.Files = group?.ListShortcutInfo;
            this.DataContext = this;
        }

        private void Rename(ShortcutInfo shortcutInfo)
        {
            var msg = new ModernMessageBox("请设置一个用于显示的别名", "设置显示名", ModernMessageboxIcons.Question, "确定", "取消")
            {
                TextBoxText = shortcutInfo.FileRenameDisp,
                TextBoxVisibility = Visibility.Visible,
                Owner = MessageBoxHelper.GlobalParentWindow
            };
            msg.ShowDialog();
            if (msg.Result == ModernMessageboxResult.Button1)
                shortcutInfo.FileRename = msg.TextBoxText;

            shortcutInfo.UpdateShortcutToDB();


        }
        #endregion

        // 发送到桌面
        private void MISendToDesktop_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShortcutInfo item in lbFiles.SelectedItems)
            {
                CreateDesktopShortcut(item);
            }
        }

        private void MIOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (ShortcutInfo File in lbFiles.SelectedItems)
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "Explorer.exe";
                    p.StartInfo.Arguments = "/select," + File.FileFullPath;
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
        }

        private void MIAdminRun_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShortcutInfo File in lbFiles.SelectedItems)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Verb = "runas",
                    //设置启动动作,确保以管理员身份运行
                    FileName = File.FileFullPath
                };
                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch
                {
                    return;
                }
            }

        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (lbFiles.SelectedItem != null)
            {
                (((FrameworkElement)sender).ContextMenu.Items.GetItemAt(5) as MenuItem).Visibility =
                    (lbFiles.SelectedItem as ShortcutInfo).FileFullPath.ToLower().EndsWith(".exe") ? Visibility.Visible : Visibility.Collapsed;
            }
        }




        // 省略其他代码

        private string CreateDesktopShortcut(ShortcutInfo shortcut)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = System.IO.Path.Combine(desktopPath, 
                    (string.IsNullOrEmpty(shortcut.FileRename) ? 
                    System.IO.Path.GetFileNameWithoutExtension(shortcut.FileFullPath) : 
                    shortcut.FileRename) + ".lnk");

                IShellLink link = (IShellLink)new ShellLink();
                link.SetDescription("快捷方式 " + shortcut.FileRenameDisp);
                link.SetPath(shortcut.FileFullPath);
                
                // 设置工作目录为目标文件所在目录
                string workingDir = System.IO.Path.GetDirectoryName(shortcut.FileFullPath);
                link.SetWorkingDirectory(workingDir);

                // 保存快捷方式
                IPersistFile file = (IPersistFile)link;
                file.Save(shortcutPath, false);
                return shortcutPath;
                //MessageBoxHelper.ShowMessage("快捷方式已创建到桌面");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex);
            }
            return "";

        }


        #region 拖到桌面支持
        private Point _dragStartPoint;

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(null);
                if (Math.Abs(currentPosition.X - _dragStartPoint.X) > 5 ||
                    Math.Abs(currentPosition.Y - _dragStartPoint.Y) > 5)
                {
                    StartDragOperation();
                }
            }
        }

        private void StartDragOperation()
        {
            try
            {  // 添加边界检查
                if (lbFiles.SelectedIndex < 0 || lbFiles.SelectedIndex >= Files.Count)
                    return;
                var selectedItem = lbFiles.SelectedItem as ShortcutInfo;
                if (selectedItem != null)
                {
                        var data = new DataObject();
                        // 设置文件路径列表
                    data.SetFileDropList(new StringCollection { selectedItem.FileFullPath });
                        // 设置URL
                        data.SetData("UniformResourceLocator", selectedItem.FileFullPath);
                    // 添加这一行，确保设置ShortcutInfo对象
                    data.SetData(typeof(ShortcutInfo), selectedItem);

                    DragDrop.DoDragDrop(lbFiles, data, DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBoxHelper.ShowErrorMessage("操作失败：当前选择项已不存在\n"+ ex);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage("拖拽操作发生意外错误" + ex);
            }
        }
        #endregion


        #region 拖动排序实现
        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!IsDragSourceValid(e.Data)) return;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;

            var listBox = (ListBox)sender;
            var point = e.GetPosition(listBox);

            // 显示插入位置指示器
            var index = GetInsertionIndex(point);
            ShowInsertionMarker(index);
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (!IsDragSourceValid(e.Data)) return;

            var sourceItem = e.Data.GetData(typeof(ShortcutInfo)) as ShortcutInfo;
            var listBox = (ListBox)sender;
            var point = e.GetPosition(listBox);
            // 加强索引校验逻辑
            int newIndex = Math.Min(GetInsertionIndex(point), Files.Count);

            if (sourceItem != null && newIndex >= 0)
            {
                // 添加存在性检查
                if (Files.Contains(sourceItem))
                {
                    Files.Remove(sourceItem);
                    // 确保插入位置不超过集合范围
                    int safeIndex = Math.Min(newIndex, Files.Count);
                    Files.Insert(safeIndex, sourceItem);
                    UpdateSortOrderInDatabase();
                }
            }

            ClearInsertionMarker();
            e.Handled = true;
        }

        private bool IsDragSourceValid(System.Windows.IDataObject data)
        {
            // 检查是否为内部拖放操作（ShortcutInfo类型）
            if (data.GetDataPresent(typeof(ShortcutInfo)) &&
                data.GetData(typeof(ShortcutInfo)) is ShortcutInfo)
            {
                return true;
            }
            
            // 检查是否为文件拖放操作
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                return true;
            }
            
            return false;
        }
        private int GetInsertionIndex(Point point)
        {
            var hit = VisualTreeHelper.HitTest(lbFiles, point);
            var item = hit?.VisualHit.GetParent<ListBoxItem>();

            // 添加空值检查和索引范围限制
            return item != null ?
                Math.Min(lbFiles.ItemContainerGenerator.IndexFromContainer(item), Files.Count) :
                Files.Count;
        }

        private InsertionAdorner _currentAdorner;

        private void ShowInsertionMarker(int index)
        {
            ClearInsertionMarker();

            var item = lbFiles.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
            if (item != null)
            {
                _currentAdorner = new InsertionAdorner(item);
            }
        }

        private void ClearInsertionMarker()
        {
            _currentAdorner?.Remove();
            _currentAdorner = null;
        }

        private void UpdateSortOrderInDatabase()
        {
            // 更新所有项的排序顺序到数据库
            for (int i = 0; i < Files.Count; i++)
            {
                Files[i].SortOrder = i;
                Files[i].UpdateShortcutToDB();
            }
        }
        #endregion



    



    }




    // 添加InsertionAdorner类
    public class InsertionAdorner : Adorner
    {
        private readonly AdornerLayer _layer;
        private readonly System.Windows.Media.Brush _brush;
        private readonly System.Windows.Media.Pen _pen;

        public InsertionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _layer = AdornerLayer.GetAdornerLayer(adornedElement);
            _layer.Add(this);
            _brush = new SolidColorBrush(Colors.DodgerBlue);
            _pen = new System.Windows.Media.Pen(_brush, 2);
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(AdornedElement.RenderSize);
            drawingContext.DrawLine(_pen, new Point(rect.Left, rect.Top), new Point(rect.Left, rect.Bottom));
        }

        public void Remove()
        {
            _layer.Remove(this);
        }
    }

    // 添加可视化树扩展类
    public static class VisualTreeExtensions
    {
        public static IEnumerable<T> GetVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;
                foreach (var nestedChild in GetVisualChildren<T>(child))
                    yield return nestedChild;
            }
        }
        public static T GetParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

    }
}
