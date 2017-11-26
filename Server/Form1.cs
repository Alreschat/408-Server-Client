using System;
using SimpleTCP;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Server
{
    public partial class Form1 : Form
    {
        
        public void sendList()
        {
            foreach (var item in userbase)
            {
                item.Key.GetStream().Write(Encoding.UTF8.GetBytes("/list "), 0, 6);
                foreach (var items in userbase)
                {
                    item.Key.GetStream().Write(Encoding.UTF8.GetBytes(items.Value), 0, items.Value.Length);
                    item.Key.GetStream().Write(Encoding.UTF8.GetBytes(" "), 0, 1);
                }
            }
            
        }

        public Form1()//Starts it up
        {
            InitializeComponent();
        }

        SimpleTcpServer servertcp; //server tcp
        string uname; //Username of the current client
        Dictionary<System.Net.Sockets.TcpClient, string> userbase = new Dictionary<System.Net.Sockets.TcpClient, string>(); //Holds username as value, tcpclient as key

        private void Form1_Load(object sender, EventArgs e) // Create tcp server, configure it, disable stop button
        {
            servertcp = new SimpleTcpServer();
            servertcp.Delimiter = 0x13;
            servertcp.StringEncoder = Encoding.UTF8;
            servertcp.DataReceived += Server_DataReceived;
            btnStop.Enabled = false;
        }

        private void Server_DataReceived(object sender, SimpleTCP.Message e) // when data is recieved
        {
            string command = "";
            string delname = "";
            if (e.MessageString.Length >= 6) // so it doesn't crush when length < 6
            {
               command = e.MessageString.Substring(0, 5);
            }
            if (command.Equals("/nick")) // assign nick
            {
                uname = e.MessageString.Substring(6);
                while (userbase.ContainsValue(uname))
                {
                    uname = uname + "1";
                }
                userbase.Add(e.TcpClient, uname);
                listClients.Invoke((MethodInvoker)delegate ()
                {
                    listClients.Items.Add(uname);
                });
                txtStatus.Invoke((MethodInvoker)delegate ()
                {
                    txtStatus.Text += uname + " just connected\n" + System.Environment.NewLine;
                });

                sendList();
            }
            else if (command.Equals("/deln")) // disconnect, remove nick
            {
                delname = userbase[e.TcpClient];
                userbase.Remove(e.TcpClient);
                listClients.Invoke((MethodInvoker)delegate ()
                {
                    listClients.Items.Remove(delname);
                });
                txtStatus.Invoke((MethodInvoker)delegate ()
                {
                    txtStatus.Text += delname + " just disconnected" + System.Environment.NewLine;
                });
                sendList();
            }
            else
            {
                txtStatus.Invoke((MethodInvoker)delegate ()
                {
                    txtStatus.Text += e.MessageString;
                    e.ReplyLine(string.Format("You said: {0}", e.MessageString));

                });
            }
        }

        private void btnStart_Click(object sender, EventArgs e) // start server
        {
            btnStop.Enabled = true;
            btnStart.Enabled = false;
            txtStatus.Text += "Server starting..." + System.Environment.NewLine;
            System.Net.IPAddress ip = System.Net.IPAddress.Parse(txtHost.Text);
            servertcp.Start(ip, Convert.ToInt32(txtPort.Text));
        }

        private void btnStop_Click(object sender, EventArgs e) // stop server
        {
            servertcp.Stop();
            btnStop.Enabled = false;
            btnStart.Enabled = true;
        }
    }
}
