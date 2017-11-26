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

namespace Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clienttcp = new SimpleTcpClient();
            btnDisc.Enabled = false;
            btnSend.Enabled = false;
            clienttcp.StringEncoder = Encoding.UTF8;
            clienttcp.DataReceived += Client_DataReceived;
        }

        private void Client_DataReceived(object sender, SimpleTCP.Message e)
        {

            if (e.MessageString.Length >= 7)
                command = e.MessageString.Substring(0, 5);
            if (command.Equals("/list"))
            {
                listClients.Invoke((MethodInvoker)delegate ()
                {
                    listClients.Items.Clear();
                });
                foreach (string nick in e.MessageString.Substring(6).Split(new char[0]))
                {
                    listClients.Invoke((MethodInvoker)delegate ()
                    {
                        listClients.Items.Add(nick);
                    });
                }
            }
            else
            {
                txtStatus.Invoke((MethodInvoker)delegate ()
                {
                    txtStatus.Text += e.MessageString;

                });
            }
            
            
        }

        SimpleTcpClient clienttcp;
        string command= "";
        string[] ssize;
        private void btnConnect_Click(object sender, EventArgs e)
        {
            clienttcp.Connect(txtHost.Text, Int32.Parse(txtPort.Text));
            clienttcp.Write("/nick " + txtName.Text);
            btnConnect.Enabled = false;
            btnDisc.Enabled = true;
            btnSend.Enabled = true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            clienttcp.WriteLineAndGetReply(txtMessage.Text, TimeSpan.FromSeconds(3));
        }

        private void btnDisc_Click(object sender, EventArgs e)
        {
            clienttcp.Write("/deln ");
            clienttcp.Disconnect();
            listClients.Invoke((MethodInvoker)delegate ()
            {
                listClients.Items.Clear();
            });
            btnConnect.Enabled = true;
            btnDisc.Enabled = false;
            btnSend.Enabled = false;
        }
    }
}
