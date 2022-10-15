﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;//命名空间关机，重启，注销，锁定，休眠，睡眠
using System.Runtime.InteropServices;//命名空间关机，重启，注销，锁定，休眠，睡眠
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Timers;

namespace udp_turn_off
{
    //by tty228
    //https://github.com/tty228
    public partial class Form1 : Form
    {
        string[] args;
        public static Form1 frm1;//一个类中调用另一个窗体的控件
        public Form1(string[] args)//检测启动参数
        {
            this.args = args;
            InitializeComponent();
            frm1 = this;//一个类中调用另一个窗体的控件
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Icon = Properties.Resources._001;
            notifyIcon1.Icon = Properties.Resources._001;
            this.BeginInvoke(new Action(() => { this.Hide(); this.Opacity = 1; }));//隐藏窗口并透明化
            //初始化设置
            if (Regedit.Read("Software\\tty228\\udp_turn_off", "countdown") == "" || Regedit.Read("Software\\tty228\\udp_turn_off", "port") == "" || Regedit.Read("Software\\tty228\\udp_turn_off", "msg") == "" || Regedit.Read("Software\\tty228\\udp_turn_off", "Shutdown_Options") == "")
            {
                Button1_Click(null, null);
            }
            else
            {
                textBox1.Text = Regedit.Read("Software\\tty228\\udp_turn_off", "countdown");
                textBox2.Text = Regedit.Read("Software\\tty228\\udp_turn_off", "port");
                textBox3.Text = Regedit.Read("Software\\tty228\\udp_turn_off", "msg");
                流光溢彩ToolStripMenuItem.Checked = false;
                switch (Regedit.Read("Software\\tty228\\udp_turn_off", "Shutdown_Options"))
                {
                    case "shutdown":
                        关机ToolStripMenuItem_Click(null, null);
                        break;
                    case "dormancy":
                        休眠ToolStripMenuItem_Click(null, null);
                        break;
                    case "sleep":
                        睡眠ToolStripMenuItem_Click(null, null);
                        break;
                    case "LockWorkStation":
                        锁定ToolStripMenuItem_Click(null, null);
                        break;
                    default:
                        关机ToolStripMenuItem_Click(null, null);
                        break;
                }
                if (Regedit.Read("Software\\Microsoft\\Windows\\CurrentVersion\\Run", "udp_turn_off") == "")
                {
                    checkBox1.Checked = false;
                    开机启动ToolStripMenuItem.Checked = false;
                }
                else
                {
                    checkBox1.Checked = true;
                    开机启动ToolStripMenuItem.Checked = true;
                }
            }
            Thread recvThread = new Thread(RecvMsg);
            recvThread.IsBackground = true;
            recvThread.Start();
            SystemEvents.PowerModeChanged += OnPowerChange; //监听电源改变事件
            SendNeedMsg("start_turn_on");
        }

        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        /// <summary>
        /// 检测UDP端口是否被占用
        /// </summary>
        /// <param name="port"> 端口号 </param>
        /// <returns></returns>
        public static bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveUdpListeners();//UDP端口
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    return inUse;
                }
            }
            return inUse;
        }

        //等待网络可用
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(ref int dwFlag, int dwReserved);

        private void Waiting_for_networking()
        {
            System.Int32 dwFlag = new int();
            while (!InternetGetConnectedState(ref dwFlag, 0))
            {
                //Application.DoEvents();
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
        }

        /// <summary>
        /// 监听UDP端口
        /// </summary>
        private void RecvMsg()
        {
            //Debug.WriteLine("recv start!");
            SystemEvents.SessionSwitch += OnSessionSwitch;
            if (PortInUse(int.Parse(textBox2.Text)) == true)
            {
                MessageBox.Show("通常每个套接字地址(协议/网络地址/端口)只允许使用一次。\n\n请重新设置。", "端口被占用");
                NotifyIcon1_MouseDoubleClick(null, null);
            }
            else
            {
                UdpClient recvClient = new UdpClient(new IPEndPoint(IPAddress.Any, int.Parse(textBox2.Text)));//接收方的IP
                while (1 == 1)
                {
                    IPEndPoint remoteHost = null;
                    byte[] recvByte = recvClient.Receive(ref remoteHost);
                    string msg = Encoding.UTF8.GetString(recvByte);
                    if (msg == textBox3.Text)
                    {
                        Countdown();
                    }
                    else if (msg == "is_the_computer_on?")
                    {
                        SendMsg("the_computer_is_on");
                    }
                    else if (msg == "color")
                    {
                        流光溢彩ToolStripMenuItem.Checked = true;
                        System.Diagnostics.Process.Start("C:/Program Files/Prismatik/Prismatik.exe");
                    }
                    else if (msg == "genshin")
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("C:/Program Files/Prismatik/Prismatik.exe");
                            System.Diagnostics.Process.Start("C:/Users/bszydxh/游戏/genshin.bat");
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else if (msg == "color_off")
                    {
                        流光溢彩ToolStripMenuItem.Checked = false;
                        foreach (var process in Process.GetProcessesByName("Prismatik"))
                        {
                            try
                            {
                                // 杀掉这个进程。
                                process.Kill();
                                // 等待进程被杀掉。你也可以在这里加上一个超时时间（毫秒整数）。
                                process.WaitForExit();
                            }
                            catch (Exception)
                            {

                            }
                        }

                    }
                }
            }
        }

        private System.Timers.Timer timer_msg = new System.Timers.Timer();

        /// <summary>
        /// UDP 发送广播信息
        /// </summary>
        private void SendNeedMsg(string Msg)
        {
            Waiting_for_networking(); //等待网络连接
            IPEndPoint broadcastIpEndPoint;
            broadcastIpEndPoint = new IPEndPoint(IPAddress.Broadcast, 1145); //广播到 1145 端口
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            byte[] buf = Encoding.UTF8.GetBytes(Msg);
            client.Send(buf, buf.Length, broadcastIpEndPoint);

        }
        private void SendMsg(string Msg)
        {
            /*timer_msg.Elapsed += new ElapsedEventHandler(OnTimer); //创建 timer
            timer_msg.AutoReset = false; //只运行一次
            timer_msg.Start();
            void OnTimer(Object source, ElapsedEventArgs e)
            {*/
            System.Int32 dwFlag = new int();
            if (InternetGetConnectedState(ref dwFlag, 0))
            {
                Waiting_for_networking(); //等待网络连接
                IPEndPoint broadcastIpEndPoint;
                broadcastIpEndPoint = new IPEndPoint(IPAddress.Broadcast, 1145); //广播到 1145 端口
                UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                byte[] buf = Encoding.UTF8.GetBytes(Msg);
                client.Send(buf, buf.Length, broadcastIpEndPoint);
            }
            /*timer_msg.Stop();*/
            /* }*/
        }

        /// <summary>
        /// 监听 电源改变事件
        /// </summary>
        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    SendMsg("system_turn_on");
                    break;
                case PowerModes.Suspend:
                    SendMsg("the_computer_is_about_to_shut_down");
                    break;
            }
        }
        private void OnSessionSwitch(object s, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    关灯ToolStripMenuItem_Click(null,null);
                    break;
                case SessionSwitchReason.SessionUnlock:
                    SendMsg("turn_on");
                    break;
            }
        }
        //休眠、睡眠
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        //锁定
        [DllImport("user32")]
        public static extern void LockWorkStation();

        //注销
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        //窗口倒计时
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string IpClassName, string IpWindowName);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        /// <summary>
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        const int WM_CLOSE = 0x10;
        int t;

        /// <summary>
        /// 执行关机操作
        /// </summary>
        private void Turn_off()
        {
            if (关机ToolStripMenuItem.Checked == true)
            {
                Process.Start("shutdown", "/s /t 0");  // 参数 /s 的意思是要关闭计算机 参数 /t 0 的意思是告诉计算机 0 秒之后执行命令
            }
            if (休眠ToolStripMenuItem.Checked == true)
            {
                SetSuspendState(true, true, true);
            }
            if (睡眠ToolStripMenuItem.Checked == true)
            {
                SetSuspendState(false, true, true);
            }
            if (锁定ToolStripMenuItem.Checked == true)
            {
                LockWorkStation();
            }
        }

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); //窗体置顶
        public const uint SWP_NOMOVE = 0x0002; //不调整窗体位置
        public const uint SWP_NOSIZE = 0x0001; //不调整窗体大小
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        private void Timer1_Tick(object sender, EventArgs e)
        {
            IntPtr hwnd = FindWindow(null, "系统将于" + t.ToString() + "秒后关机");
            t = t - 1;
            SetWindowText(hwnd, "系统将于" + t.ToString() + "秒后关机");
            SetWindowPos(hwnd, HWND_TOPMOST, 1, 1, 1, 1, SWP_NOMOVE | SWP_NOSIZE);
            if (hwnd == IntPtr.Zero && t != 0)//如果窗口已经关闭
            {
                timer1.Enabled = false;
            }
            if (t == 0)
            {
                timer1.Enabled = false;
                SendMessage(hwnd, WM_CLOSE, 0, 0);
                Turn_off();
            }
        }

        /// <summary>
        /// 开启关机倒计时
        /// </summary>
        private void Countdown()
        {
            DialogResult result;
            t = int.Parse(textBox1.Text);
            timer1.Enabled = true;
            result = MessageBox.Show("系统关机提示：\n\n 立即关机请点击【确定】\n\n 取消请点击【取消】", "系统将于" + t + "秒后关机", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.OK)
            {
                Turn_off();
            }
            else if (t > 0)
            {
                SendMsg("the_computer_is_on");
            }
        }

        private void 关机ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            关机ToolStripMenuItem.Checked = true;
            休眠ToolStripMenuItem.Checked = false;
            睡眠ToolStripMenuItem.Checked = false;
            锁定ToolStripMenuItem.Checked = false;
            comboBox1.Text = "关机";
            Regedit.Save("Software\\tty228\\udp_turn_off", "Shutdown_Options", "shutdown");
        }

        private void 休眠ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            关机ToolStripMenuItem.Checked = false;
            休眠ToolStripMenuItem.Checked = true;
            睡眠ToolStripMenuItem.Checked = false;
            锁定ToolStripMenuItem.Checked = false;
            comboBox1.Text = "休眠";
            Regedit.Save("Software\\tty228\\udp_turn_off", "Shutdown_Options", "dormancy");
        }

        private void 睡眠ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            关机ToolStripMenuItem.Checked = false;
            休眠ToolStripMenuItem.Checked = false;
            睡眠ToolStripMenuItem.Checked = true;
            锁定ToolStripMenuItem.Checked = false;
            comboBox1.Text = "睡眠";
            Regedit.Save("Software\\tty228\\udp_turn_off", "Shutdown_Options", "sleep");
        }

        private void 锁定ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            关机ToolStripMenuItem.Checked = false;
            休眠ToolStripMenuItem.Checked = false;
            睡眠ToolStripMenuItem.Checked = false;
            锁定ToolStripMenuItem.Checked = true;
            comboBox1.Text = "锁定";
            Regedit.Save("Software\\tty228\\udp_turn_off", "Shutdown_Options", "LockWorkStation");
        }

        private void 开机启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (开机启动ToolStripMenuItem.Checked == false)
            {
                checkBox1.Checked = true;
                开机启动ToolStripMenuItem.Checked = true;
                Regedit.Save("Software\\Microsoft\\Windows\\CurrentVersion\\Run", "udp_turn_off", Process.GetCurrentProcess().MainModule.FileName);
            }
            else
            {
                checkBox1.Checked = false;
                开机启动ToolStripMenuItem.Checked = false;
                Regedit.Delete("Software\\Microsoft\\Windows\\CurrentVersion\\Run", "udp_turn_off");
            }
        }

        /// <summary>
        /// 托盘右键退出
        /// </summary>
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// 双击托盘显示设置窗体
        /// </summary>
        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true; this.Opacity = 100;
        }
        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotifyIcon1_MouseDoubleClick(null, null);
        }

        //窗体关闭事件
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //点击关闭时最小化到托盘
            this.WindowState = FormWindowState.Minimized;
            e.Cancel = true;
            this.BeginInvoke(new Action(() => { this.Hide(); this.Opacity = 1; }));//隐藏窗口并透明化
            //判断是否为 windows 关闭事件
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                SendMsg("the_computer_is_about_to_shut_down");
            }
        }

        /// <summary>
        /// 恢复默认设置
        /// </summary>
        private void Button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "5";
            textBox2.Text = "8080";
            textBox3.Text = "turn_off_the_computer";
            关机ToolStripMenuItem_Click(null, null);
            checkBox1.Checked = false;
        }

        /// <summary>
        /// 限制输入（只能输入数字）
        /// </summary>
        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
                e.Handled = true;
        }

        /// <summary>
        /// 限制输入（只能输入0-100）
        /// </summary>
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
                textBox1.Text = 0.ToString();
            int number = int.Parse(textBox1.Text);
            textBox1.Text = number.ToString();
            if (number <= 100)
            {
                return;
            }
            textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
            textBox1.SelectionStart = textBox1.Text.Length;
        }

        /// <summary>
        /// 限制输入（只能输入数字）
        /// </summary>
        private void TextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
                e.Handled = true;
        }

        /// <summary>
        /// 限制输入（只能输入0-65535）
        /// </summary>
        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text == "")
                textBox2.Text = 0.ToString();
            int number = int.Parse(textBox2.Text);
            textBox2.Text = number.ToString();
            if (number <= 65535)
            {
                return;
            }
            textBox2.Text = textBox2.Text.Substring(0, textBox2.Text.Length - 1);
            textBox2.SelectionStart = textBox2.Text.Length;
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void Button2_Click(object sender, EventArgs e)
        {
            Regedit.Save("Software\\tty228\\udp_turn_off", "countdown", textBox1.Text);
            Regedit.Save("Software\\tty228\\udp_turn_off", "port", textBox2.Text);
            Regedit.Save("Software\\tty228\\udp_turn_off", "msg", textBox3.Text);
            switch (comboBox1.Text)
            {
                case "关机":
                    关机ToolStripMenuItem_Click(null, null);
                    break;
                case "休眠":
                    休眠ToolStripMenuItem_Click(null, null);
                    break;
                case "睡眠":
                    睡眠ToolStripMenuItem_Click(null, null);
                    break;
                case "锁定":
                    锁定ToolStripMenuItem_Click(null, null);
                    break;
                default:
                    关机ToolStripMenuItem_Click(null, null);
                    break;
            }
            if (checkBox1.Checked != 开机启动ToolStripMenuItem.Checked)
            {
                开机启动ToolStripMenuItem_Click(null, null);
            }
            Application.Restart();
        }

        /// <summary>
        /// 关于
        /// </summary>
        private void Button3_Click(object sender, EventArgs e)
        {
            AboutBox1 about_box = new AboutBox1();
            about_box.Show(this);
        }

        private void 开灯ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendMsg("turn_on");
        }

        private void 关灯ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (流光溢彩ToolStripMenuItem.Checked == true)
            {
                流光溢彩ToolStripMenuItem.Checked = false;
                foreach (var process in Process.GetProcessesByName("Prismatik"))
                {
                    try
                    {
                        // 杀掉这个进程。
                        process.Kill();
                        // 等待进程被杀掉。你也可以在这里加上一个超时时间（毫秒整数）。
                        process.WaitForExit();
                    }
                    catch (Exception)
                    {

                    }
                }
            }


            SendMsg("turn_off");
        }

        private void 流光溢彩ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!流光溢彩ToolStripMenuItem.Checked)
            {
                SendMsg("computer");
            }
            else
            {
                SendMsg("normal_light");
            }
        }
    }
}