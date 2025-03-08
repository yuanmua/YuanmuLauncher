using yuanmu.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace yuanmu.Entities
{
    // 实现属性变更通知接口（用于数据绑定）
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class ShortcutInfo
    {
        // 唯一标识符（自动生成）
        public string ID { get; set; } = Guid.NewGuid().ToString();

        // 所属分组ID（用于归类管理）
        public string GroupID { get; set; }
        
        // 添加排序顺序属性
        public int SortOrder { get; set; }
        
        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FileFullPath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get { return System.IO.Path.GetFileNameWithoutExtension(FileFullPath); } }
        
        // 文件重命名属性（修改时自动通知FileRenameDisp更新）
        [PropertyChanged.AlsoNotifyFor("FileRenameDisp")]
        public string FileRename { get; set; }

        // 显示用文件名（优先显示重命名，若为空则显示原始文件名）
        public string FileRenameDisp { get { return string.IsNullOrWhiteSpace(FileRename) ? FileName : FileRename; } }

        // 图标缓存私有属性
        private ImageSource FileIconSave { get; set; }
        /// <summary>
        /// 文件图标
        /// </summary>
        public ImageSource FileIcon
        {
            get
            {
                // 延迟加载：首次访问时获取图标
                if (FileIconSave == null)
                    FileIconSave = IconManager.FindIconForFilename(FileFullPath, true);
                return FileIconSave;
            }
        }

        public string FileSize { get { return System.IO.File.Exists(FileFullPath) ? Common.GetString(new System.IO.FileInfo(FileFullPath).Length) : ""; } }

        public string FileTypeDescription { get { return Common.GetFileTypeDescription(FileFullPath); } }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="FileName"></param>
        public void StartFile()
        {
            new System.Threading.Thread(() =>
            {
                try
                {
                    System.Diagnostics.Process.Start(FileFullPath);
                }
                catch { }
            }).Start();
        }

        // 将快捷方式添加到数据库
        internal void AddNewShortcutToDB()
        {
            SqliteHelper.Instance.ExecuteNonQuery(
                "insert into ShortcutInfo (GroupID,ID,FileFullPath,FileRename) values (@GroupID,@ID,@FileFullPath,@FileRename)",
                new Dictionary<string, object>()
                {
                    {"GroupID",this.GroupID },
                    {"ID",this.ID },
                    {"FileFullPath",this.FileFullPath },
                    {"FileRename",this.FileName},
                });
        }

        // 从数据库删除快捷方式
        internal void RemoveShortcutFromDB()
        {
            SqliteHelper.Instance.ExecuteNonQuery(
                "Delete from ShortcutInfo where ID=@Id",
                new Dictionary<string, object>()
                {
                    {"Id", this.ID}
                });
        }

        // 更新数据库中的重命名信息
        internal void UpdateShortcutToDB()
        {
            SqliteHelper.Instance.ExecuteNonQuery(
                "update ShortcutInfo set FileRename=@FileRename, SortOrder=@SortOrder where ID=@Id",
                new Dictionary<string, object>()
                {
                    {"FileRename", this.FileRename},
                    {"SortOrder", this.SortOrder},
                    {"Id", this.ID}
                });
        }
    }
}
