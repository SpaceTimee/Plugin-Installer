using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace Vizpower_Plugin_Installer_WPF
{
    public partial class MainWindow : Window
    {
        //说明:
        //更改版本号到Properties/AssemblyInfo.cs改，特殊版本更改字符串SpecialVersion，如测试版可改为“Alpha”,“Beta”
        //请使用三位版本号，每位使用一位数
        //如有需要，可修改下方字段
        //CaptureDesktop.dll和WxbPluginGUI.exe在Resources文件夹，替换掉原来的再编译即可
        //OriginalCaptureDesktop.dll是原版无限宝CaptureDesktop.dll，用于在拆卸时还原，如文件有更新，可将文件名改为OriginalCaptureDesktop.dll并替换

        private const string SpecialVersion = "";    //特殊版本后缀
        private const bool SkipUpdate = false;  //是否跳过开启时的检查更新
        private const bool KillWxbBeforeInstall = true; //安装前是否自动杀死无限宝相关进程
        private const bool KillWxbBeforeUninstall = true;   //拆卸前是否自动杀死无限宝相关进程
        private const string AgreementUrl = @"https://yuhuison-1259460701.cos.ap-chengdu.myqcloud.com/mzsm.html";   //用户协议Url
        private const string InstallTipUrl = @"https://gitee.com/klxn/wxbplugin/raw/master/install.png";    //安装提示Url
        private const string InstallVideoUrl = @"https://www.bilibili.com/video/BV1Ca4y1E7mx";  //使用教程Url

        //注意:打包请勿修改下方内容
        private static readonly Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version; //当前版本
        private static readonly int CurrentVersionCode = int.Parse(CurrentVersion.Major.ToString() + CurrentVersion.Minor.ToString() + CurrentVersion.Build.ToString());  //当前版本号

        private static string FileName = "";   //LoginTool.exe的名字
        private static string CaptureDesktopPath = "";
        private static string WxbPluginGUIExePath = "";
        private static string WxbPluginGUIDllPath = "";

        public MainWindow(string[] args)
        {
            InitializeComponent();

            //联网检查更新
            if (!SkipUpdate && Strings.Left(Environment.OSVersion.ToString(), 22) != "Microsoft Windows NT 5")
                Task.Run(CheckUpdateOnline);

            //修改全局标题
            Title = "无限宝第三方插件 " + CurrentVersion.Major + "." + CurrentVersion.Minor + "." + CurrentVersion.Build + " " + SpecialVersion + (string.IsNullOrEmpty(SpecialVersion) ? "安装器" : " 安装器");

            //填充安装路径
            if (args.Length >= 1 && File.Exists(args[0]) && Path.GetFileName(args[0]).StartsWith("LoginTool") && Path.GetFileName(args[0]).EndsWith(".exe"))
                DealWithPath(args[0]);
            else if (File.Exists(Properties.Settings.Default.FilePath))
                DealWithPath(Properties.Settings.Default.FilePath);
        }
        private void CheckUpdateOnline()
        {
            try
            {
                string WebText = GetWebCode("https://gitee.com/klxn/wxbplugin/raw/master/service.txt"); //API返回结果

                if (Strings.Split(WebText, "<版本>").ToList().Count < 3)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show("检查更新失败，请检查网络连接", Title);
                    }));
                    return;
                }

                string LatestVersion = Strings.Split(WebText, "<版本>")[1];

                int LatestVersionCode = 0;
                try
                {
                    string Num = "";
                    for (int i = 1; i <= LatestVersion.Length; ++i)
                        Num += (Strings.AscW(Strings.Mid(LatestVersion, i, 1)) >= 48 && Strings.AscW(Strings.Mid(LatestVersion, i, 1)) <= 57) ? Strings.Mid(LatestVersion, i, 1) : "";

                    LatestVersionCode = int.Parse(Num);
                }
                catch
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show("处理联网信息时发生错误，请向开发者反馈", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return;
                }
                if (LatestVersionCode > CurrentVersionCode)
                {
                    string ForceUpdate = Strings.Split(WebText, "<强制更新>")[1];
                    string DownLoadURL = Strings.Split(WebText, "<链接>")[1];

                    if (ForceUpdate == "0")
                    {
                        //非强制更新
                        Dispatcher.Invoke(new Action(() =>
                        {
                            if (MessageBox.Show("插件已有更新，最新版本：" + LatestVersion + "\n是否跳转下载更新？", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                                Process.Start(DownLoadURL);
                        }));
                    }
                    else
                    {
                        //强制更新
                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show("本版本已停用，最新版本：" + LatestVersion + "\n即将跳转下载更新", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));

                        Process.Start(DownLoadURL);

                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("处理联网信息时发生错误，请向开发者反馈", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                return;
            }
        }
        private string GetWebCode(string strURL)
        {
            Uri arg_15_0 = new Uri(strURL);
            byte[] i;
            Queue<byte> dataQue = new Queue<byte>();
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(arg_15_0);
            DateTime sTime = Conversions.ToDate("1990-09-21 00:00:00");
            httpReq.IfModifiedSince = sTime;
            httpReq.Method = "GET";
            httpReq.Timeout = 6000;
            HttpWebResponse httpResp;
            try
            {
                httpResp = (HttpWebResponse)httpReq.GetResponse();
            }
            catch (Exception arg_58_0)
            {
                ProjectData.SetProjectError(arg_58_0);
                string GetWebCode = "<title>no thing found</title>";
                ProjectData.ClearProjectError();
                return GetWebCode;
            }
            Stream ioS = httpResp.GetResponseStream();
            checked
            {
                while (ioS.CanRead)
                {
                    try
                    {
                        dataQue.Enqueue((byte)ioS.ReadByte());
                    }
                    catch (Exception arg_87_0)
                    {
                        ProjectData.SetProjectError(arg_87_0);
                        ProjectData.ClearProjectError();
                        break;
                    }
                }
                i = new byte[dataQue.Count - 1 + 1];
                int num = dataQue.Count - 1;
                for (int j = 0; j <= num; j++)
                {
                    i[j] = dataQue.Dequeue();
                }
                string tCode = Encoding.GetEncoding("UTF-8").GetString(i);
                string charSet = Strings.Replace(Conversions.ToString(GetByDiv2(tCode, "charset=", "\"")), "\"", "", 1, -1, CompareMethod.Binary);
                if (Operators.CompareString(charSet, "", false) == 0)
                {
                    if (Operators.CompareString(httpResp.CharacterSet, "", false) == 0)
                    {
                        tCode = Encoding.GetEncoding("UTF-8").GetString(i);
                    }
                    else
                    {
                        tCode = Encoding.GetEncoding(httpResp.CharacterSet).GetString(i);
                    }
                }
                else
                {
                    tCode = Encoding.GetEncoding(charSet).GetString(i);
                }
                string GetWebCode = tCode;
                if (Operators.CompareString(tCode, "", false) == 0)
                {
                    GetWebCode = "<title>no thing found</title>";
                }
                return GetWebCode;
            }
        }
        private object GetByDiv2(string code, string divBegin, string divEnd)
        {
            int lens = Strings.Len(divBegin);
            checked
            {
                object GetByDiv2;
                if (Strings.InStr(1, code, divBegin, CompareMethod.Binary) == 0)
                {
                    GetByDiv2 = "";
                }
                else
                {
                    int lgStart = Strings.InStr(1, code, divBegin, CompareMethod.Binary) + lens;
                    int lgEnd = Strings.InStr(lgStart + 1, code, divEnd, CompareMethod.Binary);
                    if (lgEnd == 0)
                    {
                        GetByDiv2 = "";
                    }
                    else
                    {
                        GetByDiv2 = Strings.Mid(code, lgStart, lgEnd - lgStart);
                    }
                }
                return GetByDiv2;
            }
        }

        //安装路径
        private void NavigateButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "无限宝登陆工具|LoginTool*.exe" };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DealWithPath(openFileDialog.FileName);
        }
        private void DealWithPath(string filePath)
        {
            LocationTextBox.Text = filePath;    //路径
            FileName = Path.GetFileName(filePath);    //文件名(包括后缀名)

            CaptureDesktopPath = LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll");
            WxbPluginGUIExePath = LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe");
            WxbPluginGUIDllPath = LocationTextBox.Text.Replace(FileName, "wxbPluginGUI.dll");

            Properties.Settings.Default.FilePath = LocationTextBox.Text;
            Properties.Settings.Default.Save();

            if (File.Exists(CaptureDesktopPath) &&
                File.Exists(WxbPluginGUIExePath))
                InstallButton.Content = "更新";
        }

        //安装拆卸
        private void InstallButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(LocationTextBox.Text) || string.IsNullOrEmpty(FileName))
            {
                MessageBox.Show("请点击浏览找到 LoginTool.exe 文件", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (string.IsNullOrEmpty(LocationTextBox.Text) || string.IsNullOrEmpty(FileName))
            {
                MessageBox.Show("请点击浏览找到 LoginTool.exe 文件", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName == "iMeeting" || process.ProcessName == "LoginTool" || process.ProcessName == "WxbPluginGUI")
                {
                    if (MessageBox.Show("发现无限宝相关进程残留，安装器会尝试自动关闭它，是否继续？", Title, MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                        return false;

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

反馈请加QQ群: 904645614

插件/安装器开发者:
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
    }
}