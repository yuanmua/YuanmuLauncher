# LimLauncher - Windows快捷启动器

![应用截图](./docs/images/main-ui.png)
*（请添加实际截图至指定路径）*

## 🚀 功能特性
- 📂 分组管理 - 支持拖拽排序的树状结构管理
- 🖱️ 智能交互 - 拖拽排序支持分组和项目层级
- 🔑 全局热键 - Alt+W 显示/隐藏窗口
- 📥 系统集成 - 托盘图标最小化
- 🗃️ 数据持久化 - SQLite数据库存储

## 📦 安装与编译
### 环境要求
- .NET 7.0 SDK
- Windows 10/11 64位

```bash
# 还原NuGet包
nuget restore ".\yuanmu.csproj"

# 编译发布版本
msbuild /p:Configuration=Release /p:Platform="Any CPU"
```


## 🎮 使用指南
### 核心交互 操作 效果 右键分组项

删除分组（带确认对话框） Alt+拖动分组

调整分组顺序（自动保存排序索引） 双击托盘图标

恢复窗口位置
### 主题配置流程
```mermaid
graph TD
    A[打开设置窗口] --> B{选择颜色}
    B -->|RGB选择| C[实时预览效果]
    B -->|HEX输入| C
    C --> D[应用全局主题]
 ```

## ⚙️ 技术架构
```dot
digraph arch {
    node [shape=box];
    WPF -> {MahApps.Metro, MaterialDesign};
    ViewModel -> {SQLite, Settings.settings};
    HotkeyManager -> Win32API;
}
 ```

## ⚠️ 注意事项
1. 首次运行会自动创建 filelist.db 数据库文件
2. 窗口关闭策略：
   ```csharp
   protected override void OnClosing(CancelEventArgs e)
   {
       if (!_isExit) {
           e.Cancel = true;
           Hide(); // 最小化到托盘
       }
   }
    ```
3. 热键冲突可通过修改 `RegisterHotkey` 参数解决

## 🤝 贡献指南
1. 提交Issue时请包含：
    - 操作系统版本
    - .NET运行时版本
    - 复现步骤的屏幕录像
2. PR需通过编译检查：
   ```bash
   msbuild /p:Configuration=Release /t:Rebuild
    ```
借鉴了https://github.com/lim0513/LimLauncher.git这个项目 。但基本上和这个项目无关了框架转移升级到.NET 7.0，添加了修改了很多功能，没写过WPF，用项目帮助学习

