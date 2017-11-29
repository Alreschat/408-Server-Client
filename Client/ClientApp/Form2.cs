using RichTextBoxExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class Form2 : Form
    {
        private Form1 form1;
        public NetworkStream networkStream;
        private System.Windows.Forms.RichTextBox textboxy;
        private string playingAgainst;
        public Form2()
        {
            InitializeComponent();
        }

        public Form2(Form1 form1)
        {
            this.form1 = form1;
            this.networkStream = form1.networkStream;
            this.textboxy = form1.textboxy;
            this.playingAgainst = form1.playingAgainst;
            InitializeComponent();
        }

        private void surrButton_Click(object sender, EventArgs e)
        {
            byte[] list_bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMed" + playingAgainst + "\0");
            networkStream.Write(list_bytesToWrite, 0, list_bytesToWrite.Length);
            textboxy.AppendText("Sent surrender message, opponent \"" + playingAgainst + "\" has won the game!", Color.Black);
            form1.enableChallenge();
            this.Close();
        }
    }
}
