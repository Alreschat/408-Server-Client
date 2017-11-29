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
        public NetworkStream networkStream;
        private Thread thrReceive;
        private bool terminating;
        private bool clickedList;
        private bool requestedList;
        private bool clickedChallenge;
        private bool requestedChallenge;
        private bool playingGame;
        public string playingAgainst;
        private int listClients_selectedIndex;
        static Form2 gameform;
        public System.Windows.Forms.RichTextBox textboxy;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clickedList = false;
            requestedList = false;
            clickedChallenge = false;
            requestedChallenge = false;
            playingGame = false;
            listClients_selectedIndex = -1;
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
                textboxy = tbActivity;

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

        public void Receive()
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
                    tbActivity.AppendText("Server: " + newmessage, Color.YellowGreen);
                    if (newmessage == "ID")
                    {
                        try
                        {
                            clientName = tbClient.Text;
                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("ID" + clientName);
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
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
                                tbActivity.AppendText("Server: " + newmessage, Color.YellowGreen);

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
                                        if (clickedList == true && terminating == false)
                                        {
                                            try
                                            {
                                                byte[] list_bytesToWrite = ASCIIEncoding.ASCII.GetBytes("LS");
                                                networkStream.Write(list_bytesToWrite, 0, list_bytesToWrite.Length);
                                                requestedList = true;
                                                tbActivity.AppendText("Successfully sent Lobby List request.", Color.Black);
                                            }
                                            catch
                                            {
                                                tbActivity.AppendText("Failed to send Lobby List request.", Color.Red);
                                                terminating = true;
                                                break;
                                            }

                                            clickedList = false;
                                        }

                                        if (clickedChallenge == true && terminating == false)
                                        {
                                            clickedChallenge = false;

                                            if (playingGame == false)
                                            {
                                                string name = "";
                                                if (listClients_selectedIndex >= 0 && listClients_selectedIndex < listClients.Items.Count)
                                                {
                                                    name = listClients.Items[listClients_selectedIndex].ToString();
                                                }
                                                if (name != "" && name != clientName)
                                                {
                                                    try
                                                    {
                                                        byte[] list_bytesToWrite = ASCIIEncoding.ASCII.GetBytes($"CH{name}\0");
                                                        networkStream.Write(list_bytesToWrite, 0, list_bytesToWrite.Length);
                                                        tbActivity.AppendText($"Successfully sent challenge request.", Color.Black);

                                                        requestedChallenge = true;
                                                    }
                                                    catch
                                                    {
                                                        tbActivity.AppendText("Failed to send challenge request.", Color.Red);
                                                        terminating = true;

                                                        challenge.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            challenge.Enabled = true;
                                                        });
                                                    }
                                                }
                                                else
                                                {
                                                    challenge.BeginInvoke((MethodInvoker)delegate ()
                                                    {
                                                        challenge.Enabled = true;
                                                    });
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    byte[] list_bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMed" + playingAgainst + "\0");
                                                    networkStream.Write(list_bytesToWrite, 0, list_bytesToWrite.Length);
                                                    tbActivity.AppendText("Sent surrender message, opponent \"" + playingAgainst + "\" has won the game!", Color.Black);
                                                }
                                                catch
                                                {
                                                    tbActivity.AppendText("Failed to send surrender message.", Color.Red);
                                                    terminating = true;
                                                }

                                                playingGame = false;
                                                challenge.BeginInvoke((MethodInvoker)delegate ()
                                                {
                                                    challenge.Text = "Challenge";
                                                    challenge.Enabled = true;
                                                });
                                            }
                                        }

                                        try
                                        {
                                            if (clientTcp.Client.Poll(1000, SelectMode.SelectRead))
                                            {
                                                byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                                                if (byteCount <= 0)
                                                {
                                                    throw new SocketException();
                                                }

                                                newmessage = Encoding.ASCII.GetString(bytesToRead);
                                                newmessage = newmessage.Substring(0, newmessage.IndexOf("\0")); //NUL is always the final character of the newest message
                                                tbActivity.AppendText("Server: " + newmessage, Color.YellowGreen);

                                                if (newmessage.Length > 2)
                                                {
                                                    if (requestedChallenge == false && newmessage.Substring(0, 2) == "CH")
                                                    {
                                                        //Recieved challenge, display challenge window
                                                        string challengerName = newmessage.Substring(2);

                                                        //DISABLE BUTTONS WHILE MESSAGE BOX IS OPEN
                                                        btStart.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            btStart.Enabled = false;
                                                        });
                                                        challenge.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            challenge.Enabled = false;
                                                        });
                                                        btRequest.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            btRequest.Enabled = false;
                                                        });
                                                        //////////////////////////////////////////
                                                        DialogResult dialogResult = MessageBox.Show(challengerName + " has challenged you. Would you like to accept?", "A new challenger!", MessageBoxButtons.YesNo);
                                                        //ENABLE BUTTONS AFTER CLOSING MESSAGE BOX
                                                        btRequest.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            btRequest.Enabled = true;
                                                        });
                                                        challenge.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            challenge.Enabled = true;
                                                        });
                                                        btStart.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            btStart.Enabled = true;
                                                        });
                                                        //////////////////////////////////////////
                                                        if (dialogResult == DialogResult.Yes)
                                                        {
                                                            //Accept challenge
                                                            try
                                                            {
                                                                bytesToWrite = ASCIIEncoding.ASCII.GetBytes("AC" + challengerName + "\0");
                                                                networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                                                tbActivity.AppendText("Accepted challenge.", Color.Black);
                                                                Application.EnableVisualStyles();
                                                                Application.SetCompatibleTextRenderingDefault(false);
                                                                gameform = new Form2(this);
                                                                Application.Run(gameform);
                                                                playingAgainst = challengerName;
                                                                playingGame = true;
                                                                challenge.BeginInvoke((MethodInvoker)delegate ()
                                                                {
                                                                    challenge.Enabled = false;
                                                                });
                                                            }
                                                            catch
                                                            {
                                                                tbActivity.AppendText("Failed to accept challenge.", Color.Red);
                                                                terminating = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //Decline challenge
                                                            try
                                                            {
                                                                bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DE" + challengerName + "\0");
                                                                networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                                                tbActivity.AppendText("Declined challenge.", Color.Black);
                                                            }
                                                            catch
                                                            {
                                                                tbActivity.AppendText("Failed to decline challenge.", Color.Red);
                                                                terminating = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else if (requestedChallenge == true)
                                                    {
                                                        if (newmessage.Substring(0, 2) == "AC")
                                                        {
                                                            string challengedName = newmessage.Substring(2);

                                                            //Other client accepted the sent challenge
                                                            tbActivity.AppendText(challengedName + " accepted the challenge.", Color.Black);

                                                            requestedChallenge = false;
                                                            playingAgainst = challengedName;
                                                            playingGame = true;
                                                            Application.EnableVisualStyles();
                                                            Application.SetCompatibleTextRenderingDefault(false);
                                                            gameform = new Form2(this);
                                                            Application.Run(gameform);
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Enabled = false;
                                                            });
                                                        }

                                                        else if (newmessage.Substring(0, 2) == "DE")
                                                        {
                                                            string challengedName = newmessage.Substring(2);

                                                            //Other client declined the sent challenge
                                                            tbActivity.AppendText(challengedName + " declined the challenge.", Color.Black);

                                                            requestedChallenge = false;
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Enabled = true;
                                                            });
                                                        }

                                                        else if (newmessage.Substring(0, 2) == "DX")
                                                        {
                                                            string challengedName = newmessage.Substring(2);
                                                            //Selected client is no longer connected to server, they disconnected after last time Lobby List was requested
                                                            tbActivity.AppendText("Client \"" + challengedName + "\" is no longer connected to server, refreshing Lobby List.", Color.Black);

                                                            clickedList = true; //Programatically "click" Lobby List button to refresh
                                                            requestedChallenge = false;
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Enabled = true;
                                                            });
                                                        }

                                                        else if (newmessage.Substring(0, 2) == "CH")
                                                        {
                                                            //Recieved challenge, but had already requested challenge
                                                            tbActivity.AppendText("Recieved challenge, but had already requested challenge.", Color.Black);
                                                            //Decline challenge
                                                            string challengerName = newmessage.Substring(2);
                                                            try
                                                            {
                                                                bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DE" + challengerName + "\0");
                                                                networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                                                tbActivity.AppendText("Automatically declined challenge.", Color.Black);
                                                            }
                                                            catch
                                                            {
                                                                tbActivity.AppendText("Failed to automatically decline challenge.", Color.Red);
                                                                terminating = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    if (requestedList == true && newmessage.Substring(0, 2) == "LS")
                                                    {
                                                        //Recieved the list; split it into names, refresh Lobby List with new names
                                                        string theList = newmessage.Substring(2, newmessage.Length - 2);
                                                        char[] delimiterChars = { '\n' };
                                                        string[] splitList = theList.Split(delimiterChars);

                                                        listClients.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            listClients.Items.Clear();
                                                        });

                                                        listClients_selectedIndex = -1; //Reset selected index to match deselection in UI

                                                        foreach (string s in splitList)
                                                        {
                                                            listClients.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                listClients.Items.Add(s);
                                                            });
                                                        }

                                                        requestedList = false;

                                                        btRequest.BeginInvoke((MethodInvoker)delegate ()
                                                        {
                                                            btRequest.Enabled = true;
                                                        });
                                                        if (requestedChallenge == false)
                                                        {
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Enabled = true;
                                                            });
                                                        }
                                                    }
                                                    if (playingGame == true && newmessage.Substring(0, 2) == "GM")
                                                    {
                                                        if (newmessage == "GMed")
                                                        {
                                                            //Opponent surrendered
                                                            tbActivity.AppendText("Opponent \"" + playingAgainst + "\" has surrendered, you have won the game!", Color.Black);

                                                            playingGame = false;
                                                            gameform.Close();
                                                            challenge.Enabled = true;
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Text = "Challenge";
                                                            });
                                                        }
                                                        else if (newmessage == "GMdc")
                                                        {
                                                            //Opponent disconnected during game
                                                            tbActivity.AppendText("Opponent \"" + playingAgainst + "\" disconnected, you have won the game!.", Color.Black);

                                                            playingGame = false;
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Text = "Challenge";
                                                            });
                                                        }
                                                        else if (newmessage == "GMch")
                                                        {
                                                            //Opponent disconnected after sending challenge
                                                            tbActivity.AppendText("Opponent \"" + playingAgainst + "\" disconnected after sending the challenge.", Color.Black);

                                                            playingGame = false;
                                                            challenge.BeginInvoke((MethodInvoker)delegate ()
                                                            {
                                                                challenge.Text = "Challenge";
                                                            });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            tbActivity.AppendText("Lost connection to server.", Color.Red);
                                            terminating = true;
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
            challenge.BeginInvoke((MethodInvoker)delegate ()
            {
                challenge.Text = "Challenge";
                challenge.Enabled = false;
            });
            btRequest.BeginInvoke((MethodInvoker)delegate ()
            {
                btRequest.Enabled = false;
            });

            playingGame = false;
            clickedList = false;
            requestedList = false;
            clickedChallenge = false;
            requestedChallenge = false;

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
            btRequest.Enabled = false;

            clickedList = true; //Set variable to true so that the "if(clickedList)" code block executes
        }

        private void challenge_Click(object sender, EventArgs e)
        {
            challenge.Enabled = false;

            clickedChallenge = true; //Set variable to true so that the "if(clickedChallenge)" code block executes
        }

        private void listClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            listClients_selectedIndex = listClients.SelectedIndex;
        }
        public void enableChallenge()
        {
            challenge.Enabled = true;
        }
    }
}