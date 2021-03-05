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

namespace TcpClient
{
    public partial class FrmClient : Form
    {
        public FrmClient()
        {
            InitializeComponent();
        }

        private WatsonTcp.WatsonTcpClient client;
        private void btn_link_Click(object sender, EventArgs e)
        {
            if (btn_link.Text == "连接")
            {
                client = new WatsonTcp.WatsonTcpClient($"{txt_ip.Text.Replace(" ", "")}", int.Parse(txt_port.Text));
                client.Events.ServerConnected += OnConnected;
                client.Events.MessageReceived += OnDataReceived;
                client.Events.ServerDisconnected += OnDisconnected;
                client.Connect();
                btn_link.Text = "断开";
            }
            else
            {
                btn_link.Text = "连接";
                client.Disconnect();
            }
        }

        private void OnDisconnected(object sender, DisconnectionEventArgs e)
        {
            ShowLog($"断开连接，原因：{e.Reason}");
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
            ShowLog("连接成功");
        }

        private void ShowLog(string msg)
        {
            this.BeginInvoke((Action)delegate
            {
                ri_log.Text = ri_log.Text.Insert(0, $"{DateTime.Now.ToString("yyyyy-MM-dd HH:mm:ss")} {msg}\r\n");
            });
        }

        private void btn_sendfile_Click(object sender, EventArgs e)
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
                    client.SendAsync(ms.GetBuffer(), dict);
                }
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            client.Send(txt_msg.Text);
            txt_msg.Text = string.Empty;
        }
    }
}
