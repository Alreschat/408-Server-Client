using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RichTextBoxExtensions;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private bool accept;
        private bool btStart_Click_isStop = false; //Set to true if button is in "Stop" mode
        private int serverPort;
        private TcpListener tcpListener;
        private OrderedDictionary clientDatabase = new OrderedDictionary(); //Stores pairs of Client Names and their corresponding Sockets; ordered so first Client added is at the top
        private List<TcpClient> clientList = new List<TcpClient>();
        private Object clientLock = new object(); //Lock to ensure only one thread accesses shared structures at any given time

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (!btStart_Click_isStop)
            {
                tbPort.ReadOnly = true;
                accept = true;

                try
                {
                    serverPort = Convert.ToInt32(tbPort.Text);
                    IPAddress ipAddress = System.Net.IPAddress.Parse(tbIP.Text);

                    tcpListener = new TcpListener(ipAddress, serverPort);
                    tcpListener.Start();
                    tbActivity.AppendText("Started listening for incoming connections on port " + tbPort.Text + ".", Color.Green);

                    Thread thrAccept = new Thread(new ThreadStart(Accept))
                    {
                        IsBackground = true //Thread terminates if window is closed
                    };
                    thrAccept.Start();

                    btStart.Text = "Stop";
                    btStart_Click_isStop = true;
                }
                catch
                {
                    tbActivity.AppendText("Cannot create a server with the specified IP/port number, check the port number and try again.", Color.Red);
                    tbPort.ReadOnly = false;
                }
            }
            else
            {
                tcpListener.Stop();

                //Do clean-up once stopped listening for connections
                int listSize = clientList.Count();
                for (int ctr = 0; ctr < listSize; ctr++)
                {
                    clientList[ctr].Close();
                }
                clientList.Clear();

                accept = false;

                listClients.Items.Clear();

                tbActivity.AppendText("Stopped listening for incoming connections.", Color.Green);

                btStart.Text = "Start";
                btStart_Click_isStop = false;

                tbPort.ReadOnly = false;
            }
        }

        //Thread to accept new incoming connections
        private void Accept()
        {
            while (accept)
            {
                try
                {
                    clientList.Add(tcpListener.AcceptTcpClient());
                    Thread thrReceive;
                    thrReceive = new Thread(new ThreadStart(Receive))
                    {
                        IsBackground = true //Thread terminates if window is closed
                    };
                    thrReceive.Start();
                }
                catch
                {
                    if (accept) //If server was still accepting connections, a socket error occured; else the listener was stopped voluntarily
                    {
                        tbActivity.AppendText("Listener has stopped working.", Color.Red);
                    }
                }
            }
        }

        //Thread initially requests identification from Client; once the connection is accepted, it waits for the Lobby List to be requested
        private void Receive()
        {
            TcpClient clientTcp = clientList[clientList.Count - 1];
            NetworkStream networkStream = clientTcp.GetStream();
            bool terminating = false;
            byte[] initial_BytesToRead = new byte[clientTcp.ReceiveBufferSize];
            string clientName = "";
            
            try
            {
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes("ID");
                networkStream.Write(bytesToSend, 0, bytesToSend.Length);
            }
            catch
            {
                tbActivity.AppendText("New client attempted to connect, failure in requesting identification.", Color.Purple);

                clientTcp.Close();
                clientList.Remove(clientTcp);
                terminating = true;
            }

            if (terminating == false)
            {
                tbActivity.AppendText("New client attempting to connect, requested identification.", Color.Black);

                try
                {
                    int byteCount = networkStream.Read(initial_BytesToRead, 0, clientTcp.ReceiveBufferSize);

                    if (byteCount <= 0)
                    {
                        throw new SocketException();
                    }
                }
                catch
                {
                    tbActivity.AppendText("Client disconnected prior to identification response.", Color.Purple);

                    clientTcp.Close();
                    clientList.Remove(clientTcp);
                    terminating = true;
                }
            }

            if (terminating == false)
            {
                string newmessage = Encoding.ASCII.GetString(initial_BytesToRead);
                newmessage = newmessage.Substring(0, newmessage.IndexOf("\0")); //NUL is always the final character of the newest message
                tbActivity.AppendText("Client response to identification request: " + newmessage, Color.Black);

                if (newmessage.Length <= 2 || newmessage.Substring(0, 2) != "ID") //Identification response must begin with "ID" and the name must be at least 1 character long
                {
                    tbActivity.AppendText("Client identification response not in correct format, connection refused.", Color.Purple);

                    try
                    {
                        byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes("FM"); //Incorrect format code
                        networkStream.Write(bytesToSend, 0, bytesToSend.Length);
                        tbActivity.AppendText("Successfully responded to incorrectly formatted identification.", Color.Purple);
                    }
                    catch
                    {
                        tbActivity.AppendText("Failed to respond to incorrectly formatted identification.", Color.Purple);
                    }
                    
                    terminating = true;
                }
                else
                {
                    string name_substring = newmessage.Substring(2, newmessage.Length-2);

                    bool uniqueName = true;
                    lock (clientLock)
                    {
                        if (listClients.FindStringExact(name_substring) != ListBox.NoMatches) //Check if name is already in use
                        {
                            uniqueName = false;
                        }
                    }

                    if (!uniqueName)
                    {
                        tbActivity.AppendText("Client name \"" + name_substring + "\" not unique, connection refused.", Color.Purple);

                        try
                        {
                            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes("UN"); //Non-unique name code
                            networkStream.Write(bytesToSend, 0, bytesToSend.Length);
                            tbActivity.AppendText("Successfully responded to non-unique identification.", Color.Purple);
                        }
                        catch
                        {
                            tbActivity.AppendText("Failed to respond to non-unique identification.", Color.Purple);
                        }
                                
                        terminating = true;
                    }
                    else
                    {
                        try
                        {
                            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes("OK");
                            networkStream.Write(bytesToSend, 0, bytesToSend.Length);
                            tbActivity.AppendText("Successfully responded to unique identification.", Color.Black);

                            clientName = name_substring;

                            //Name is unique, add to list
                            lock (clientLock)
                            {
                                clientDatabase.Add(clientName, clientTcp);

                                listClients.BeginInvoke((MethodInvoker)delegate ()
                                {
                                    listClients.Items.Add(clientName);
                                });
                            }
                        }
                        catch
                        {
                            tbActivity.AppendText("Failed to respond to unique identification.", Color.Purple);
                            terminating = true;
                        }
                    }
                }
            }

            //If name is unique, establish connection
            while (!terminating)
            {
                try
                {
                    Byte[] bytesToRead = new byte[clientTcp.ReceiveBufferSize];
                    int byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                    if (byteCount <= 0)
                    {
                        throw new SocketException();
                    }

                    string newmessage = Encoding.ASCII.GetString(bytesToRead);
                    newmessage = newmessage.Substring(0, byteCount);
                    tbActivity.AppendText("\"" + clientName + "\": " + newmessage, Color.Black);
                    if (newmessage == "LS") //List request code
                    {
                        //Client requested Lobby List
                        tbActivity.AppendText("Client \"" + clientName + "\" has requested the Lobby List.", Color.Blue);

                        try
                        {
                            string textList;

                            lock (clientLock)
                            {
                                int databaseSize = clientDatabase.Count;
                                ////tbActivity.AppendText("List Size: " + databaseSize.ToString(), Color.YellowGreen);

                                //Copy Client names from database to string array, then form the Lobby List
                                String[] clientNames = new String[databaseSize];
                                clientDatabase.Keys.CopyTo(clientNames, 0);

                                textList = "LS" + clientNames[0];

                                int listSize = clientNames.Length;
                                for (int ctr = 1; ctr < listSize; ctr++)
                                {
                                        textList = textList + "\n" + clientNames[ctr]; //Seperate names using newline as it cannot be a character in a client's name
                                }
                                textList = textList + "\0"; //NUL is always the final character of the newest message
                                ////tbActivity.AppendText("List: " + textList, Color.YellowGreen);
                            }

                            byte[] bufferList = ASCIIEncoding.ASCII.GetBytes(textList);
                            networkStream.Write(bufferList, 0, bufferList.Length);

                            tbActivity.AppendText("Sent Lobby List to Client \"" + clientName + "\".", Color.Blue);
                        }
                        catch
                        {
                            tbActivity.AppendText("Client \"" + clientName + "\" disconnected before lobby list was sent.", Color.Purple);
                            terminating = true;
                        }
                    }
                }
                catch
                {
                    tbActivity.AppendText("Client \"" + clientName + "\" has disconnected.", Color.Purple);
                    terminating = true;
                }
            }

            lock (clientLock)
            {
                clientDatabase.Remove(clientName);

                listClients.BeginInvoke((MethodInvoker)delegate ()
                {
                    listClients.Items.Remove(clientName);
                });
            }

            //Close client connection
            clientTcp.Close();
            clientList.Remove(clientTcp);
        }
    }
}
