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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //说明:
        //更改版本号到Assembly Information改，特殊版本更改字符串SpecialVersion，如测试版可改为“Alpha”,“Beta”
        //请使用三位版本号，每位使用一位数
        //如有需要，可修改下方字段
        //CaptureDesktop.dll和WxbPluginGUI.exe在Resources文件夹，替换掉原来的再编译即可
        //OriginalCaptureDesktop.dll是原版无限宝CaptureDesktop.dll，用于在拆卸时还原，如文件有更新，可将文件名改为OriginalCaptureDesktop.dll并替换

        private const string SpecialVersion = "Beta";    //特殊版本后缀
        private const bool SkipUpdate = false;  //是否跳过开启时的检查更新
        private const string AgreementUrl = @"https://yuhuison-1259460701.cos.ap-chengdu.myqcloud.com/mzsm.html";   //用户协议Url
        private const string InstallTipUrl = @"https://gitee.com/klxn/wxbplugin/raw/master/install.png";    //安装提示Url
        private const string InstallVideoUrl = @"https://www.bilibili.com/video/BV1Ca4y1E7mx";  //使用教程Url

        //注意:打包无需修改下方内容

        private string FileName = "";   //LoginTool.exe的名字
        private string OriginInstallButtonContent = "安装";  //原始安装按钮的Content
        private int WrongNum = 0;   //呵呵次数

        private static readonly Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version; //当前版本
        private static int CurrentVersionCode = 0;  //当前版本号

        public MainWindow()
        {
            InitializeComponent();

            //联网检查更新
            Task.Run(CheckUpdateOnline);

            //修改全局标题
            Title = "无限宝第三方插件 Ver " + CurrentVersion.Major + "." + CurrentVersion.Minor + "." + CurrentVersion.Build + " " + SpecialVersion + " 安装器";

            LocationTextBox.Text = Properties.Settings.Default.FilePath;
            FileName = Properties.Settings.Default.FileName;

            //记录当前版本号
            CurrentVersionCode = int.Parse(CurrentVersion.Major.ToString() + CurrentVersion.Minor.ToString() + CurrentVersion.Build.ToString());

            //如果有跳过更新标记 或 是XP则不联网检测更新
            if (SkipUpdate || Strings.Left(Environment.OSVersion.ToString(), 22) == "Microsoft Windows NT 5")
                return;
        }
        private void CheckUpdateOnline()
        {
            try
            {
                string WebText = GetWebCode("https://gitee.com/klxn/wxbplugin/raw/master/service.txt");
                string[] WebList;
                string ForceUpdate, LatestVersion, DownLoadURL;
                int LatestVersionCode;

                WebList = Strings.Split(WebText, "<版本>");
                if (WebList.ToList().Count < 3)
                {
                    MessageBox.Show("连接服务器失败，请检查网络连接", Title);
                    return;
                }
                LatestVersion = WebList[1];

                try
                {
                    string Num = "";
                    for (int i = 1; i <= LatestVersion.Length; i++)
                        Num += (Strings.AscW(Strings.Mid(LatestVersion, i, 1)) >= 48 && Strings.AscW(Strings.Mid(LatestVersion, i, 1)) <= 57) ? Strings.Mid(LatestVersion, i, 1) : "";

                    LatestVersionCode = int.Parse(Num);
                }
                catch
                {
                    MessageBox.Show("处理联网信息时发生错误，请向开发者反馈", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (LatestVersionCode > CurrentVersionCode)
                {
                    WebList = Strings.Split(WebText, "<强制更新>");
                    ForceUpdate = WebList[1];
                    WebList = Strings.Split(WebText, "<链接>");
                    DownLoadURL = WebList[1];

                    if (ForceUpdate == "0")
                    {
                        //非强制更新
                        if (MessageBox.Show("插件已更新，最新版本：" + LatestVersion + "\n是否跳转下载更新？", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                            Process.Start(DownLoadURL);
                    }
                    else
                    {
                        //强制更新
                        MessageBox.Show("插件有重要更新，最新版本：" + LatestVersion + "\n即将跳转下载更新", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Process.Start(DownLoadURL);

                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                MessageBox.Show("处理联网信息时发生错误，请向开发者反馈", Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        private string GetWebCode(string strURL)
        {
            Uri arg_15_0 = new Uri(strURL);
            byte[] i = new byte[1];
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

        //安装拆卸核心功能
        private void NavigateButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "无限宝登陆工具|LoginTool*.exe";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocationTextBox.Text = openFileDialog.FileName; //路径
                FileName = System.IO.Path.GetFileName(LocationTextBox.Text);    //文件名(包括后缀名)

                Properties.Settings.Default.FileName = FileName;
                Properties.Settings.Default.FilePath = LocationTextBox.Text;
                Properties.Settings.Default.Save();

                if (File.Exists(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll")) &&
                    File.Exists(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe")))
                    InstallButton.Content = "更新";
            }
        }
        private void InstallButton_Click(object sender, EventArgs e)
        {
            if (AgreementCheckBox.IsChecked == false)
            {
                if (WrongNum == 1)
                    MessageBox.Show("???", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum < 3)
                    MessageBox.Show("你呵呵什么?", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum < 5)
                    MessageBox.Show("去想想有没有漏掉什么?", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum < 10)
                    MessageBox.Show("呵呵", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum == 10)
                    MessageBox.Show("我佛了，同意个用户协议有这么难么", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum == 100)
                    MessageBox.Show("是的，你已经呵呵了100次了", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum == 1000)
                    MessageBox.Show("大佬，你的坚持和毅力已经击败了99.99%的人", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (WrongNum == 1001)
                    MessageBox.Show("但用户协议是不能跳过的", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("同意一下用户协议吧QAQ", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ++WrongNum;
                return;
            }
            if (LocationTextBox.Text == "" || FileName == "")
            {
                MessageBox.Show("请点击浏览找到 LoginTool.exe 文件", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("安装前需关闭无限宝相关进程，如有残留进程，安装器会关闭它，是否继续？", Title, MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return;

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
                        return;
                    }
                }
            }

            if (File.Exists(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll")))
                File.Delete(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll"));
            if (File.Exists(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe")))
                File.Delete(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe"));
            if (File.Exists(LocationTextBox.Text.Replace(FileName, "wxbPluginGUI.dll")))
                File.Delete(LocationTextBox.Text.Replace(FileName, "wxbPluginGUI.dll"));

            try
            {
                byte[] data = Properties.Resources.CaptureDesktop;
                Stream stream = File.Create(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll"));
                stream.Write(data, 0, data.Length);
                stream.Close();

                data = Properties.Resources.WxbPluginGUI;
                stream = File.Create(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe"));
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
            if (LocationTextBox.Text == "" || FileName == "")
            {
                MessageBox.Show("请点击浏览找到 LoginTool.exe 文件", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool isclear = true;

            if (File.Exists(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll")))
            {
                File.Delete(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll"));

                byte[] data = Properties.Resources.OriginalCaptureDesktop;
                Stream stream = File.Create(LocationTextBox.Text.Replace(FileName, "CaptureDesktop.dll"));
                stream.Write(data, 0, data.Length);
                stream.Close();

                isclear = false;
            }
            if (File.Exists(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe")))
            {
                File.Delete(LocationTextBox.Text.Replace(FileName, "WxbPluginGUI.exe"));
                isclear = false;
            }
            if (File.Exists(LocationTextBox.Text.Replace(FileName, "wxbPluginGUI.dll")))
            {
                File.Delete(LocationTextBox.Text.Replace(FileName, "wxbPluginGUI.dll"));
                isclear = false;
            }

            if (isclear)
                MessageBox.Show("没有检测到插件，无需拆卸", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("卸载成功！", Title, MessageBoxButtons.OK, MessageBoxIcon.Information);

            InstallButton.Content = "安装";
        }

        //关于
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show
            (
@"欢迎使用无限宝第三方插件安装器

安装器开发者: WXRIW，Space Time 
插件开发者: 秋小十，快乐小牛，极地萤火

(开发者名单如有缺失请找Space Time加上)

反馈请加QQ群: 904645614

使用前须知:
1. 本程序免费开源，任何人不得将本程序用于商业和违法用途
2. 安装前请认真阅读并同意用户协议和免责声明
3. 插件致力于帮助学生更好地记录笔记，请认真听课",
                Title, MessageBoxButtons.OK
            );
        }

        //同意协议相关处理
        private void InstallButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (AgreementCheckBox.IsChecked == false)
            {
                OriginInstallButtonContent = InstallButton.Content as string;
                InstallButton.Content = "呵呵";
            }
        }
        private void InstallButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (AgreementCheckBox.IsChecked == false)
                InstallButton.Content = OriginInstallButtonContent;
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
