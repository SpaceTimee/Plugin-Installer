using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace Vizpower_Plugin_Installer
{
    public partial class MainWindow : Window
    {
        //说明:
        //更改版本号到Properties/AssemblyInfo.cs改，特殊版本更改字符串SpecialVersion，如测试版可改为“Alpha”,“Beta”
        //请使用三位版本号，每位使用一位数
        //如有需要，可修改下方字段
        //CaptureDesktop.dll和WxbPluginGUI.exe在Resources文件夹，替换掉原来的再编译即可
        //OriginalCaptureDesktop.dll是原版无限宝CaptureDesktop.dll，用于在拆卸时还原，如文件有更新，可将文件名改为OriginalCaptureDesktop.dll并替换

        private const string SpecialVersionSuffix = "";    //特殊版本后缀
        private const bool SkipUpdate = false;  //是否跳过开启时的检查更新
        private const bool KillWxbBeforeInstall = true; //安装前是否自动杀死无限宝相关进程
        private const bool KillWxbBeforeUninstall = true;   //拆卸前是否自动杀死无限宝相关进程
        private const string AgreementUrl = @"https://yuhuison-1259460701.cos.ap-chengdu.myqcloud.com/mzsm.html";   //用户协议Url
        private const string InstallTipUrl = @"https://gitee.com/klxn/wxbplugin/raw/master/install.png";    //安装提示Url
        private const string InstallVideoUrl = @"https://www.bilibili.com/video/BV1Ca4y1E7mx";  //视频教程Url

        //注意:打包请勿修改下方内容
        private static readonly Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version; //当前版本信息
        private static string CaptureDesktopPath = "";
        private static string WxbPluginGUIExePath = "";
        private static string WxbPluginGUIDllPath = "";

        public MainWindow(string[] args)
        {
            InitializeComponent();

            //联网检查更新
            if (!SkipUpdate)
                Task.Run(CheckUpdateOnline);

            //修改全局标题
            Title = "无限宝第三方插件 " + CurrentVersion.ToString().Substring(0, 5) + " " + SpecialVersionSuffix + (string.IsNullOrEmpty(SpecialVersionSuffix) ? "安装器" : " 安装器");

            //填充安装路径
            if (args.Length >= 1)
                PathTextBox.Text = args[0];
            else
                PathTextBox.Text = Properties.Settings.Default.FilePath;
        }
        private async void CheckUpdateOnline()
        {
            try
            {
                string currentVersionCode = CurrentVersion.Major.ToString() + CurrentVersion.Minor.ToString() + CurrentVersion.Build.ToString();  //当前版本号
                string httpResponseStr = await new HttpClient().GetStringAsync("https://gitee.com/klxn/wxbplugin/raw/master/service.txt");
                string latestVersion = Strings.Split(httpResponseStr, "<版本>")[1];
                string latestVersionCode = "";
                for (int i = 1; i <= latestVersion.Length; ++i)
                    latestVersionCode += (Strings.AscW(Strings.Mid(latestVersion, i, 1)) >= 48 && Strings.AscW(Strings.Mid(latestVersion, i, 1)) <= 57) ? Strings.Mid(latestVersion, i, 1) : "";

                if (int.Parse(latestVersionCode) > int.Parse(currentVersionCode))
                {
                    string forceUpdate = Strings.Split(httpResponseStr, "<强制更新>")[1];
                    string downLoadURL = Strings.Split(httpResponseStr, "<链接>")[1];

                    if (forceUpdate == "0")
                    {
                        //非强制更新
                        Dispatcher.Invoke(() =>
                        {
                            if (MessageBox.Show("插件已有更新，最新版本：" + latestVersion + "\n是否跳转下载更新？", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                                Process.Start(downLoadURL);
                        });
                    }
                    else
                    {
                        //强制更新
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("本版本已停用，最新版本：" + latestVersion + "\n即将跳转下载更新", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });

                        Process.Start(downLoadURL);

                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                Dispatcher.Invoke(() => { MessageBox.Show("检查更新失败", Title, MessageBoxButtons.OK, MessageBoxIcon.Error); });
                return;
            }
        }

        //路径处理
        private void PathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (File.Exists(PathTextBox.Text) && Path.GetFileName(PathTextBox.Text).StartsWith("LoginTool") && Path.GetFileName(PathTextBox.Text).EndsWith(".exe"))
            {
                PathTextBox.Foreground = Brushes.Black;
                DealWithPath(PathTextBox.Text);
            }
            else
                PathTextBox.Foreground = Brushes.Red;
        }
        private void DealWithPath(string filePath)
        {
            string fileName = Path.GetFileName(filePath);    //文件名(包括后缀名)

            CaptureDesktopPath = filePath.Replace(fileName, "CaptureDesktop.dll");
            WxbPluginGUIExePath = filePath.Replace(fileName, "WxbPluginGUI.exe");
            WxbPluginGUIDllPath = filePath.Replace(fileName, "wxbPluginGUI.dll");

            Properties.Settings.Default.FilePath = filePath;
            Properties.Settings.Default.Save();

            if (File.Exists(CaptureDesktopPath) && File.Exists(WxbPluginGUIExePath))
                InstallButton.Content = "更新";
        }

        //浏览路径
        private void NavigateButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "无限宝登陆工具|LoginTool*.exe", RestoreDirectory = true };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                PathTextBox.Text = openFileDialog.FileName;
        }

        //安装拆卸
        private void InstallButton_Click(object sender, EventArgs e)
        {
            if (PathTextBox.Foreground != Brushes.Black)
            {
                MessageBox.Show("请在输入框中填入正确的 LoginTool.exe 文件路径", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //杀死进程
            if (KillWxbBeforeInstall && !KillWxbProcess())
                return;

            if (File.Exists(CaptureDesktopPath))
                File.Delete(CaptureDesktopPath);
            if (File.Exists(WxbPluginGUIExePath))
                File.Delete(WxbPluginGUIExePath);
            if (File.Exists(WxbPluginGUIDllPath))
                File.Delete(WxbPluginGUIDllPath);

            try
            {
                byte[] data = Properties.Resources.CaptureDesktop;
                Stream stream = File.Create(CaptureDesktopPath);
                stream.Write(data, 0, data.Length);
                stream.Close();

                data = Properties.Resources.WxbPluginGUI;
                stream = File.Create(WxbPluginGUIExePath);
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            catch
            {
                MessageBox.Show("安装失败，请检查是否以管理员模式运行了本安装程序？是否已完全关闭无限宝？是否已关闭杀毒软件？所选目录是否正确？", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("安装成功！", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);

            InstallButton.Content = "重装";
        }
        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (PathTextBox.Foreground != Brushes.Black)
            {
                MessageBox.Show("请在输入框中填入正确的 LoginTool.exe 文件路径", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //杀死进程
            if (KillWxbBeforeUninstall && !KillWxbProcess())
                return;

            bool isclear = true;
            if (File.Exists(CaptureDesktopPath))
            {
                File.Delete(CaptureDesktopPath);

                byte[] data = Properties.Resources.OriginalCaptureDesktop;
                Stream stream = File.Create(CaptureDesktopPath);
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            if (File.Exists(WxbPluginGUIExePath))
            {
                File.Delete(WxbPluginGUIExePath);
                isclear = false;
            }
            if (File.Exists(WxbPluginGUIDllPath))
            {
                File.Delete(WxbPluginGUIDllPath);
                isclear = false;
            }

            if (isclear)
                MessageBox.Show("没有检测到插件，无需拆卸", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("卸载成功！", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);

            InstallButton.Content = "安装";
        }
        private bool KillWxbProcess()
        {
            if (MessageBox.Show("安装前安装器会尝试自动关闭无限宝相关进程，是否继续？", Title, MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return false;

            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "iMeeting" || process.ProcessName == "LoginTool" || process.ProcessName == "WxbPluginGUI")
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch
                    {
                        MessageBox.Show("关闭无限宝进程时出现错误，请尝试手动关闭无限宝");
                        return false;
                    }
                }
            }

            return true;
        }

        //关于
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show
            (
@"欢迎使用无限宝第三方插件安装器

反馈请加 QQ 群: 904645614

插件 / 安装器开发者:
秋小十，快乐小牛，极地萤火，
阳莱，凌莞，WXRIW，Space Time

使用前须知:
1. 本程序免费开源，任何人不得将本程序用于商业和违法用途
2. 安装前请认真阅读并同意用户协议和免责声明
3. 插件致力于帮助学生更好地记录笔记，请认真听课",
                Title, MessageBoxButtons.OK
            );
        }

        //同意协议相关处理
        private void AgreementCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = true;
        }
        private void AgreementCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
        }

        //链接相关处理
        private void AgreementLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(AgreementUrl);
        }
        private void TipLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(InstallTipUrl);
        }
        private void VideoLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(InstallVideoUrl);
        }

        //窗口强关热键
        private void MainWin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.W)
                Environment.Exit(0);
        }
    }
}