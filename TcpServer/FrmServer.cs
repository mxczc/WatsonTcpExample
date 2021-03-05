using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WatsonTcp;

namespace TcpServer
{
    public partial class FrmServer : Form
    {
        public FrmServer()
        {
            InitializeComponent();
        }
        private WatsonTcp.WatsonTcpServer server;
        private void btn_link_Click(object sender, EventArgs e)
        {
            if (btn_link.Text == "启动")
            {
                server = new WatsonTcp.WatsonTcpServer($"{txt_ip.Text.Replace(" ", "")}", int.Parse(txt_port.Text));
                server.Events.ClientConnected += OnConnected;
                server.Events.MessageReceived += OnDataReceived;
                server.Events.ClientDisconnected += OnDisconnected;
                btn_link.Text = "停止";
                server.Start();
            }
            else
            {
                btn_link.Text = "启动";
                server.DisconnectClients(MessageStatus.Normal);
                server.Dispose();
            }
        }
        private void OnDisconnected(object sender, DisconnectionEventArgs e)
        {
            this.BeginInvoke((Action)delegate
            {
                cbo_client.Items.Remove(e.IpPort);
            });
            ShowLog($"客户端断开连接，原因：{e.Reason}");
        }

        private void OnDataReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Metadata?.ContainsKey("file") ?? false)
            {
                string path = $"{ AppDomain.CurrentDomain.BaseDirectory }\\{ DateTime.Now.ToString("yyyyMMddHHmmssfff")}{e.Metadata["file"]}";

                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    fs.Write(e.Data, 0, e.Data.Length);
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }
                ShowLog($"收到文件，路径{path}");
            }
            else
            {
                ShowLog(Encoding.UTF8.GetString(e.Data));
            }
        }

        private void OnConnected(object sender, ConnectionEventArgs e)
        {
            this.BeginInvoke((Action)delegate
            {
                cbo_client.Items.Add(e.IpPort);
            });
            ShowLog("客户端连接成功");
        }

        private void ShowLog(string msg)
        {
            this.BeginInvoke((Action)delegate
            {
                ri_log.Text += $"{DateTime.Now.ToString("yyyyy-MM-dd HH:mm:ss")} {msg}\r\n";
            });
        }

        private void FrmServer_Load(object sender, EventArgs e)
        {

        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            if (cbo_client.Text != "")
            {
                server.Send(cbo_client.Text, txt_msg.Text);
                txt_msg.Text = string.Empty;
            }
            else
            {
                MessageBox.Show("请先选中客户端");
            }
        }

        private void btn_sendfile_Click(object sender, EventArgs e)
        {
            if (cbo_client.Text != "")
            {
                FileDialog file = new OpenFileDialog();
                if (file.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream fs = new FileStream(file.FileName, FileMode.Open, FileAccess.Read))
                    {
                        Dictionary<object, object> dict = new Dictionary<object, object>();
                        dict.Add("file", Path.GetExtension(file.FileName));
                        MemoryStream ms = new MemoryStream();
                        fs.CopyTo(ms);
                        server.SendAsync(cbo_client.Text, ms.GetBuffer(), dict);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选中客户端");
            }
        }
    }
}
