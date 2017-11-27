using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using RichTextBoxExtensions;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        private bool btStart_Click_isStop = false; //Set to true if button is in "Stop" mode
        private string serverIP;
        private int serverPort;
        private string clientName;
        private TcpClient clientTcp;
        private NetworkStream networkStream;
        private Thread thrReceive;
        private bool terminating;
        private bool requestedList;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            bool inputError = false;

            if (!btStart_Click_isStop) //If button is in "Start" mode, attempt to connect
            {
                btStart.Enabled = false;
                tbIP.ReadOnly = true;
                tbPort.ReadOnly = true;
                tbClient.ReadOnly = true;

                try
                {
                    serverIP = tbIP.Text;
                    IPAddress testIP = IPAddress.Parse(serverIP);
                }
                catch
                {
                    tbActivity.AppendText("Invalid IP Address format.", Color.Red);
                    inputError = true;
                }
                if (inputError == false)
                {
                    try
                    {
                        serverPort = Convert.ToInt32(tbPort.Text);
                    }
                    catch
                    {
                        tbActivity.AppendText("Invalid port format.", Color.Red);
                        inputError = true;
                    }
                }
                if (inputError == false && tbClient.Text == "")
                {
                    tbActivity.AppendText("Please enter a client name.", Color.Red);
                    inputError = true;
                }
                if (inputError == false)
                {
                    tbActivity.AppendText("Attempting to connect to server; IP: \"" + serverIP + "\", Port: \"" + serverPort + "\"", Color.Black);

                    try
                    {
                        clientTcp = new TcpClient(serverIP, serverPort);
                        networkStream = clientTcp.GetStream();

                        terminating = false;

                        thrReceive = new Thread(new ThreadStart(Receive))
                        {
                            IsBackground = true //Thread terminates if window is closed
                        };
                        thrReceive.Start();

                        btStart.Text = "Stop";
                        btStart_Click_isStop = true;
                    }
                    catch
                    {
                        tbActivity.AppendText("Could not connect to the specified server.", Color.Red);
                        inputError = true;
                    }
                }
                if (inputError == true)
                {
                    //Incorrect input, re-enable start button and input fields
                    btStart.Enabled = true;
                    tbIP.ReadOnly = false;
                    tbPort.ReadOnly = false;
                    tbClient.ReadOnly = false;
                }
            }
            else
            {
                terminating = true; //If button is in "Stop" mode, terminate connection
            }
        }

        private void Receive()
        {
            btStart.BeginInvoke((MethodInvoker)delegate ()
            {
                btStart.Enabled = true;
            });

            if (!terminating)
            {
                try
                {
                    byte[] bytesToRead = new byte[clientTcp.ReceiveBufferSize];

                    int byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                    if (byteCount <= 0)
                    {
                        throw new SocketException();
                    }

                    string newmessage = Encoding.ASCII.GetString(bytesToRead, 0, byteCount);
                    tbActivity.AppendText("Server: " + newmessage, Color.Black);
                    if (newmessage == "ID")
                    {
                        try
                        {
                            clientName = tbClient.Text;
                            byte[] id_bytesToWrite = ASCIIEncoding.ASCII.GetBytes("ID" + clientName);
                            networkStream.Write(id_bytesToWrite, 0, id_bytesToWrite.Length);
                            tbActivity.AppendText("Successfully sent identification.", Color.Black);

                            try
                            {
                                byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                                if (byteCount <= 0)
                                {
                                    throw new SocketException();
                                }

                                newmessage = Encoding.ASCII.GetString(bytesToRead);
                                newmessage = newmessage.Substring(0, newmessage.IndexOf("\0")); //NUL is always the final character of the newest message
                                tbActivity.AppendText("Server: " + newmessage, Color.Black);

                                if (newmessage == "FM")
                                {
                                    tbActivity.AppendText("Server rejected identification: Incorrect Format.", Color.Red);
                                    terminating = true;
                                }
                                else if (newmessage == "UN")
                                {
                                    tbActivity.AppendText("Server rejected identification: Not Unique.", Color.Red);
                                    terminating = true;
                                }
                                else if (newmessage == "OK")
                                {
                                    tbActivity.AppendText("Server accepted identification.", Color.Green);

                                    btRequest.BeginInvoke((MethodInvoker)delegate ()
                                    {
                                        btRequest.Enabled = true;
                                    });

                                    while (!terminating)
                                    {
                                        try
                                        {
                                            if (clientTcp.Client.Poll(1000, SelectMode.SelectRead))
                                            {
                                                byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);
                                            }

                                            if (byteCount <= 0)
                                            {
                                                throw new SocketException();
                                            }
                                        }
                                        catch
                                        {
                                            tbActivity.AppendText("Lost connection to server.", Color.Red);
                                            terminating = true;
                                        }

                                        if (requestedList)
                                        {
                                            try
                                            {
                                                byte[] list_bytesToWrite = ASCIIEncoding.ASCII.GetBytes("LS");
                                                networkStream.Write(list_bytesToWrite, 0, list_bytesToWrite.Length);
                                                tbActivity.AppendText("Successfully sent Lobby List request.", Color.Black);
                                                
                                                byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                                                if (byteCount <= 0)
                                                {
                                                    throw new SocketException();
                                                }

                                                newmessage = Encoding.ASCII.GetString(bytesToRead);
                                                newmessage = newmessage.Substring(0, newmessage.IndexOf("\0")); //NUL is always the final character of the newest message
                                                tbActivity.AppendText("Server: " + newmessage, Color.Black);

                                                if (newmessage.Length > 2 && newmessage.Substring(0, 2) == "LS")
                                                {
                                                    //Split recieved string into names, refresh Client List with new names
                                                    string theList = newmessage.Substring(2, newmessage.Length - 2);
                                                    char[] delimiterChars = { '\n' };
                                                    string[] splitList = theList.Split(delimiterChars);

                                                    listClients.BeginInvoke((MethodInvoker)delegate ()
                                                    {
                                                        listClients.Items.Clear();
                                                    });

                                                    foreach (string s in splitList)
                                                    {
                                                        listClients.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            listClients.Items.Add(s);
                                                        });
                                                    }
                                                }

                                            requestedList = false;
                                            }
                                            catch
                                            {
                                                tbActivity.AppendText("Failed to send Lobby List request.", Color.Red);
                                                terminating = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    tbActivity.AppendText("Unknown server response.", Color.Red);
                                    terminating = true;
                                }
                            }
                            catch
                            {
                                tbActivity.AppendText("Lost connection to server.", Color.Red);
                                terminating = true;
                            }
                        }
                        catch
                        {
                            tbActivity.AppendText("Failed to send identification.", Color.Red);
                            terminating = true;
                        }
                    }
                    else
                    {
                        tbActivity.AppendText("Server failed to request ID.", Color.Red);
                        terminating = true;
                    }
                }
                catch
                {
                    tbActivity.AppendText("Lost connection to server.", Color.Red);
                    terminating = true;
                }
            }

            tbActivity.AppendText("Disconnecting...", Color.Black);
            
            //Post-disconnect cleanup
            btRequest.BeginInvoke((MethodInvoker)delegate ()
            {
                btRequest.Enabled = false;
            });

            requestedList = false;

            clientTcp.Close();

            listClients.BeginInvoke((MethodInvoker)delegate ()
            {
                listClients.Items.Clear();
            });

            btStart.BeginInvoke((MethodInvoker)delegate ()
            {
                btStart.Text = "Start";
            });
            btStart_Click_isStop = false;
            
            tbIP.BeginInvoke((MethodInvoker)delegate ()
            {
                tbIP.ReadOnly = false;
            });
            tbPort.BeginInvoke((MethodInvoker)delegate ()
            {
                tbPort.ReadOnly = false;
            });
            tbClient.BeginInvoke((MethodInvoker)delegate ()
            {
                tbClient.ReadOnly = false;
            });
        }

        private void btRequest_Click(object sender, EventArgs e)
        {
                requestedList = true; //Set variable to true so that the "if(requestedList)" code block executes
        }
    }
}
