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
        private OrderedDictionary clientDatabase = new OrderedDictionary(); //Stores pairs of Client names and their corresponding TcpClients; ordered so first Client added is at the top
        private List<TcpClient> clientList = new List<TcpClient>();
        static private Object clientLock = new Object(); //Lock to ensure only one thread accesses shared structures at any given time
        private HashSet<string> unavailableClients = new HashSet<string>();
        private Dictionary<string, string> gameClientPairs = new Dictionary<string, string>(); //Stores pairs of 2 Client names for every ongoing game
        private Dictionary<string, int> clientGuessPairs = new Dictionary<string, int>(); //Stores pairs of Client names with the guess that they made
        private Dictionary<string, int> clientGlobalScore = new Dictionary<string, int>(); //Stores pairs of Client names with how many games they have won in total
        private Dictionary<string, int> clientRoundScore = new Dictionary<string, int>(); //Stores pairs of Client names with how many rounds they have won in their current game
        private Dictionary<string, string> recievedChallenge = new Dictionary<string, string>(); //Stores clients who have recieved a challenge but not yet answered, notify challenger if they disconnect
        private Random randGen = new Random();

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
                    tbActivity.AppendText("Cannot create a server with the specified IP/port number, check their validity and try again.", Color.Red);
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
                clientDatabase.Clear();

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
                byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("ID");
                networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
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
                        byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("FM"); //Incorrect format code
                        networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
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
                        if (clientDatabase.Contains(name_substring)) //Check if name is already in use
                        {
                            uniqueName = false;
                        }
                    }

                    if (!uniqueName)
                    {
                        tbActivity.AppendText("Client name \"" + name_substring + "\" not unique, connection refused.", Color.Purple);

                        try
                        {
                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("UN"); //Non-unique name code
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
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
                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("OK");
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                            tbActivity.AppendText("Successfully responded to unique identification.", Color.Black);

                            clientName = name_substring;

                            //Name is unique, add to list
                            lock (clientLock)
                            {
                                clientDatabase.Add(clientName, networkStream);
                                clientGlobalScore.Add(clientName, 0);

                                listClients.BeginInvoke((MethodInvoker)delegate ()
                                {
                                    listClients.Items.Add(clientName + "-0");
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
            
            string opponentName = "";
            bool playRound = false;

            //If name is unique, establish connection
            while (!terminating)
            {
                try
                {
                    byte[] bytesToRead = new byte[clientTcp.ReceiveBufferSize];
                    int byteCount = networkStream.Read(bytesToRead, 0, clientTcp.ReceiveBufferSize);

                    if (byteCount <= 0)
                    {
                        throw new SocketException();
                    }

                    string newmessage = Encoding.ASCII.GetString(bytesToRead);
                    newmessage = newmessage.Substring(0, newmessage.IndexOf("\0"));
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

                                textList = "LS" + clientNames[0] + "-" + clientGlobalScore[clientNames[0]].ToString();

                                int listSize = clientNames.Length;
                                for (int ctr = 1; ctr < listSize; ctr++)
                                {
                                    textList = textList + "\n" + clientNames[ctr] + "-" + clientGlobalScore[clientNames[ctr]].ToString(); //Seperate names using newline as it cannot be a character in a client's name
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
                    else if (newmessage.Length > 2)
                    {
                        if (newmessage.Substring(0, 2) == "CH")
                        {
                            unavailableClients.Add(clientName);

                            string challenged_Name = newmessage.Substring(2);

                            if (unavailableClients.Contains(challenged_Name)) //Challenged client is unavailable/busy
                            {
                                byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DE" + challenged_Name + "\0");
                                try
                                {
                                    networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                    tbActivity.AppendText($"Client \"{challenged_Name}\" currently unavailable, automatically declined \"{clientName}\"'s challenge.", Color.Black);

                                    unavailableClients.Remove(clientName);
                                }
                                catch
                                {
                                    tbActivity.AppendText("Client \"" + clientName + "\" disconnected before challenge could be automatically declined.", Color.Purple);
                                    terminating = true;
                                }
                            }
                            else if (!clientDatabase.Contains(challenged_Name)) //Challenged client is no longer connected to server
                            {
                                byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DX" + challenged_Name + "\0");
                                try
                                {
                                    networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                    tbActivity.AppendText($"Client \"{challenged_Name}\" is no longer connected, notified \"{clientName}\".", Color.Black);

                                    unavailableClients.Remove(clientName);
                                }
                                catch
                                {
                                    tbActivity.AppendText($"Client \"{challenged_Name}\" is no longer connected, failed to notify \"{clientName}\".", Color.Purple);
                                    terminating = true;
                                }
                            }
                            else //Challenged client is available
                            {
                                unavailableClients.Add(challenged_Name);

                                byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("CH" + clientName + "\0");
                                NetworkStream challenged_networkStream = (NetworkStream)clientDatabase[challenged_Name];
                                try
                                {
                                    challenged_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                    tbActivity.AppendText($"Sent challenge from \"{clientName}\" to \"{challenged_Name}\".", Color.Black);

                                    recievedChallenge.Add(challenged_Name, clientName);
                                }
                                catch
                                {
                                    tbActivity.AppendText($"Failed to send challenge from \"{clientName}\" to \"{challenged_Name}\".", Color.Purple);

                                    bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DE" + challenged_Name + "\0");
                                    try
                                    {
                                        networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                        tbActivity.AppendText($"Client \"{challenged_Name}\" currently unavailable, automatically declined {clientName}'s challenge.", Color.Black);
                                    }
                                    catch
                                    {
                                        tbActivity.AppendText("Client \"" + clientName + "\" disconnected before challenge could be automatically declined.", Color.Purple);
                                        terminating = true;
                                    }

                                    unavailableClients.Remove(clientName);
                                    unavailableClients.Remove(challenged_Name);
                                }
                            }
                        }
                        else if (newmessage.Substring(0, 2) == "AC") //Client accepted challenge
                        {
                            string accepted_Name = newmessage.Substring(2);
                            recievedChallenge.Remove(clientName);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("AC" + clientName + "\0");
                            NetworkStream accepted_networkStream = (NetworkStream)clientDatabase[accepted_Name];
                            try
                            {
                                accepted_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                tbActivity.AppendText($"Accepted challenge from \"{accepted_Name}\" to \"{clientName}\".", Color.Black);
                                
                                gameClientPairs.Add(accepted_Name, clientName);
                            }
                            catch
                            {
                                tbActivity.AppendText($"Failed to accept challenge from \"{accepted_Name}\" to \"{clientName}\".", Color.Purple);

                                bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMch\0");
                                try
                                {
                                    networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                    tbActivity.AppendText($"Client \"{accepted_Name}\" disconnected after sending invite, sent notice to \"{clientName}\".", Color.Black);
                                }
                                catch
                                {
                                    tbActivity.AppendText($"Client \"{accepted_Name}\" disconnected after sending invite, failed to send notice to \"{clientName}\".", Color.Purple);
                                    terminating = true;
                                }

                                unavailableClients.Remove(clientName);
                                unavailableClients.Remove(accepted_Name);
                            }
                        }
                        else if (newmessage.Substring(0, 2) == "DE") //Client declined challenge
                        {
                            unavailableClients.Remove(clientName);

                            string declined_Name = newmessage.Substring(2);
                            recievedChallenge.Remove(clientName);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DE" + clientName + "\0");
                            NetworkStream declined_networkStream = (NetworkStream)clientDatabase[declined_Name];
                            try
                            {
                                declined_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                tbActivity.AppendText($"Declined challenge from \"{declined_Name}\" to \"{clientName}\".", Color.Black);
                            }
                            catch
                            {
                                tbActivity.AppendText($"Failed to decline challenge from \"{declined_Name}\" to \"{clientName}\".", Color.Purple);
                            }

                            unavailableClients.Remove(declined_Name);
                        }
                        else if (newmessage.Substring(0, 2) == "GM")
                        {
                            if (newmessage.Length > 4 && newmessage.Substring(2, 2) == "ed") //Client ended game (surrendered)
                            {
                                string winner_Name = newmessage.Substring(4);

                                byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMed\0");
                                NetworkStream winner_networkStream = (NetworkStream)clientDatabase[winner_Name];
                                try
                                {
                                    winner_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                                    tbActivity.AppendText($"\"{clientName}\" has surrendered, \"{winner_Name}\" is the winner!", Color.Black);
                                }
                                catch
                                {
                                    tbActivity.AppendText($"\"{clientName}\" has surrendered, but failed to notify \"{winner_Name}\" that they won.", Color.Purple);
                                }
                                
                                //Round score and guess clean-up, and global score increment
                                if (clientGuessPairs.ContainsKey(clientName))
                                {
                                    clientGuessPairs.Remove(clientName);
                                }
                                if (clientGuessPairs.ContainsKey(winner_Name))
                                {
                                    clientGuessPairs.Remove(winner_Name);
                                }
                                if (clientRoundScore.ContainsKey(clientName))
                                {
                                    clientRoundScore.Remove(clientName);
                                }
                                if (clientRoundScore.ContainsKey(winner_Name))
                                {
                                    clientRoundScore.Remove(winner_Name);
                                }

                                lock (clientLock)
                                {
                                    int itemIndex = listClients.FindStringExact(winner_Name + "-" + clientGlobalScore[winner_Name]);
                                    clientGlobalScore[winner_Name] += 1;
                                    listClients.BeginInvoke((MethodInvoker)delegate ()
                                    {
                                        listClients.Items[itemIndex] = winner_Name + "-" + clientGlobalScore[winner_Name];
                                    });
                                }

                                removeFrom_clientListPairs(clientName);
                                unavailableClients.Remove(clientName);
                                unavailableClients.Remove(winner_Name);
                                
                                opponentName = "";
                            }

                            if (newmessage.Length > 4 && newmessage.Substring(2, 2) == "gu") //Client sent guess
                            {
                                int clientGuess = Convert.ToInt32(newmessage.Substring(4));

                                tbActivity.AppendText("Client \"" + clientName + "\" has guessed " + clientGuess + ".", Color.Blue);
                                
                                opponentName = getOpponentName(clientName);
                                if (clientGuessPairs.ContainsKey(opponentName))
                                {
                                    playRound = true;
                                }

                                clientGuessPairs.Add(clientName, clientGuess);
                            }
                        }
                    }
                }
                catch
                {
                    tbActivity.AppendText("Client \"" + clientName + "\" has disconnected.", Color.Purple);
                    terminating = true;
                }

                if (playRound == true)
                {
                    int clientGuess = clientGuessPairs[clientName];
                    int opponentGuess = clientGuessPairs[opponentName];
                    
                    int randomNumber = randGen.Next(1, 101); //Generate a number between 1-100
                    int clientDifference = Math.Abs(clientGuess - randomNumber);
                    int opponentDifference = Math.Abs(opponentGuess - randomNumber);

                    if (clientDifference < opponentDifference)
                    {
                        //Client wins the round
                        if (clientRoundScore.ContainsKey(clientName) == true)
                        {
                            //2nd round client won, client wins the game
                            tbActivity.AppendText($"The random number was {randomNumber}, \"{clientName}\" has won the game!", Color.Black);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMwg" + randomNumber + "-" + opponentGuess + "\0");
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                            
                            bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMlg" + randomNumber + "-" + clientGuess + "\0");
                            NetworkStream opponent_networkStream = (NetworkStream)clientDatabase[opponentName];
                            opponent_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                            clientGuessPairs.Remove(clientName);
                            clientGuessPairs.Remove(opponentName);

                            clientRoundScore.Remove(clientName);
                            if (clientRoundScore.ContainsKey(opponentName))
                            {
                                clientRoundScore.Remove(opponentName);
                            }

                            lock (clientLock)
                            {
                                int itemIndex = listClients.FindStringExact(clientName + "-" + clientGlobalScore[clientName]);
                                clientGlobalScore[clientName] += 1;
                                listClients.BeginInvoke((MethodInvoker)delegate ()
                                {
                                    listClients.Items[itemIndex] = clientName + "-" + clientGlobalScore[clientName];
                                });
                            }

                            removeFrom_clientListPairs(clientName);
                            unavailableClients.Remove(clientName);
                            unavailableClients.Remove(opponentName);

                            opponentName = "";
                        }
                        else
                        {
                            //1st round client won
                            tbActivity.AppendText($"The random number was {randomNumber}, \"{clientName}\" has won their first round.", Color.Black);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMwr" + randomNumber + "-" + opponentGuess + "\0");
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                            bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMlr" + randomNumber + "-" + clientGuess + "\0");
                            NetworkStream opponent_networkStream = (NetworkStream)clientDatabase[opponentName];
                            opponent_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                            
                            clientGuessPairs.Remove(clientName);
                            clientGuessPairs.Remove(opponentName);

                            clientRoundScore.Add(clientName, 1);
                        }
                    }
                    else if (clientDifference > opponentDifference)
                    {
                        //Opponent wins the round
                        if (clientRoundScore.ContainsKey(opponentName) == true)
                        {
                            //2nd round opponent won, opponent wins the game
                            string oppName = opponentName;
                            tbActivity.AppendText($"The random number was {randomNumber}, \"{oppName}\" has won the game!", Color.Black);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMlg" + randomNumber + "-" + opponentGuess + "\0");
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                            bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMwg" + randomNumber + "-" + clientGuess + "\0");
                            NetworkStream opponent_networkStream = (NetworkStream)clientDatabase[opponentName];
                            opponent_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                            clientGuessPairs.Remove(clientName);
                            clientGuessPairs.Remove(opponentName);

                            clientRoundScore.Remove(opponentName);
                            if (clientRoundScore.ContainsKey(clientName))
                            {
                                clientRoundScore.Remove(clientName);
                            }

                            lock (clientLock)
                            {
                                int itemIndex = listClients.FindStringExact(opponentName + "-" + clientGlobalScore[opponentName]);
                                clientGlobalScore[opponentName] += 1;
                                listClients.BeginInvoke((MethodInvoker)delegate ()
                                {
                                    listClients.Items[itemIndex] = oppName + "-" + clientGlobalScore[oppName];
                                });
                            }

                            removeFrom_clientListPairs(clientName);
                            unavailableClients.Remove(clientName);
                            unavailableClients.Remove(opponentName);

                            opponentName = "";
                        }
                        else
                        {
                            //1st round opponent won
                            string oppName = opponentName;
                            tbActivity.AppendText($"The random number was {randomNumber}, \"{oppName}\" has won their first round.", Color.Black);

                            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMlr" + randomNumber + "-" + opponentGuess + "\0");
                            networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                            bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMwr" + randomNumber + "-" + clientGuess + "\0");
                            NetworkStream opponent_networkStream = (NetworkStream)clientDatabase[opponentName];
                            opponent_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                            
                            clientGuessPairs.Remove(clientName);
                            clientGuessPairs.Remove(opponentName);

                            clientRoundScore.Add(opponentName, 1);
                        }
                    }
                    else
                    {
                        //Tie
                        tbActivity.AppendText($"The random number was {randomNumber}, the round is a tie.", Color.Black);
                        byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMtr" + randomNumber + "-" + opponentGuess + "\0");
                        networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                        bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMtr" + randomNumber + "-" + clientGuess + "\0");
                        NetworkStream opponent_networkStream = (NetworkStream)clientDatabase[opponentName];
                        opponent_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);

                        clientGuessPairs.Remove(clientName);
                        clientGuessPairs.Remove(opponentName);
                    }

                    playRound = false;
                }
            }
            //Post-disconnect cleanup

            //Round score and guess clean-up, and global score increment
            if (gameClientPairs.ContainsKey(clientName) || gameClientPairs.ContainsValue(clientName))
            {
                opponentName = getOpponentName(clientName);

                if (clientGuessPairs.ContainsKey(clientName))
                {
                    clientGuessPairs.Remove(clientName);
                }
                if (clientGuessPairs.ContainsKey(opponentName))
                {
                    clientGuessPairs.Remove(opponentName);
                }
                if (clientRoundScore.ContainsKey(clientName))
                {
                    clientRoundScore.Remove(clientName);
                }
                if (clientRoundScore.ContainsKey(opponentName))
                {
                    clientRoundScore.Remove(opponentName);
                }
            }
            
            lock (clientLock)
            {
                clientDatabase.Remove(clientName);

                if (clientGlobalScore.ContainsKey(clientName))
                {
                    string nameText = clientName + "-" + clientGlobalScore[clientName].ToString();

                    listClients.BeginInvoke((MethodInvoker)delegate ()
                    {
                        listClients.Items.Remove(nameText);
                    });
                }

                clientGlobalScore.Remove(clientName);
                removeFrom_clientListPairs_DC(clientName);
            }

            if (recievedChallenge.ContainsKey(clientName))
            {
                removeFrom_recievedChallenge_DC(clientName);
            }
            
            unavailableClients.Remove(clientName);

            clientList.Remove(clientTcp);
            
            clientTcp.Close();
        }

        //Check values and keys in gameClientPairs; remove entry with clientName as a value
        private void removeFrom_clientListPairs(string clientName)
        {
            if (gameClientPairs.ContainsKey(clientName))
            {
                gameClientPairs.Remove(clientName);
            }
            else if (gameClientPairs.ContainsValue(clientName))
            {
                foreach (var clientItem in gameClientPairs.Where(kvp => kvp.Value == clientName).Take(1).ToList())
                {
                    gameClientPairs.Remove(clientItem.Key);
                }
            }
        }

        //Check values and keys in gameClientPairs; remove entry with clientName as a value, then notify the other client that clientName disconnected
        private void removeFrom_clientListPairs_DC(string clientName)
        {
            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("GMdc\0");
            
            if (gameClientPairs.ContainsKey(clientName))
            {
                string winner_Name = gameClientPairs[clientName];
                NetworkStream winner_networkStream = (NetworkStream)clientDatabase[winner_Name];

                try
                {
                    winner_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                    tbActivity.AppendText($"\"{clientName}\" has disconnected, \"{winner_Name}\" is the winner!", Color.Black);

                    //Increment global score of opponent if they are still connected
                    lock (clientLock)
                    {
                        if (clientGlobalScore.ContainsKey(winner_Name))
                        {
                            int score = clientGlobalScore[winner_Name];
                            clientGlobalScore[winner_Name] += 1;

                            string scoreString = score.ToString();

                            listClients.BeginInvoke((MethodInvoker)delegate ()
                            {
                                if (listClients.FindStringExact(winner_Name + "-" + scoreString) != ListBox.NoMatches)
                                {
                                    int itemIndex = listClients.FindStringExact(winner_Name + "-" + scoreString);
                                    if (itemIndex != -1)
                                    {
                                        listClients.Items[itemIndex] = winner_Name + "-" + (score+1).ToString();
                                    }
                                }
                            });
                        }
                    }
                }
                catch
                {
                    tbActivity.AppendText($"\"{clientName}\" has disconnected, but failed to notify \"{winner_Name}\" that they won.", Color.Purple);
                }

                gameClientPairs.Remove(clientName);
                unavailableClients.Remove(winner_Name);
            }
            else if (gameClientPairs.ContainsValue(clientName))
            {
                foreach (var clientItem in gameClientPairs.Where(kvp => kvp.Value == clientName).Take(1).ToList())
                {
                    string winner_Name = clientItem.Key;
                    NetworkStream winner_networkStream = (NetworkStream)clientDatabase[winner_Name];
                    try
                    {
                        winner_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                        tbActivity.AppendText($"\"{clientName}\" has disconnected, \"{winner_Name}\" is the winner!", Color.Black);

                        //Increment global score of opponent if they are still connected
                        lock (clientLock)
                        {
                            if (clientGlobalScore.ContainsKey(winner_Name))
                            {
                                int score = clientGlobalScore[winner_Name];
                                clientGlobalScore[winner_Name] += 1;

                                string scoreString = score.ToString();

                                listClients.BeginInvoke((MethodInvoker)delegate ()
                                {
                                    if (listClients.FindStringExact(winner_Name + "-" + scoreString) != ListBox.NoMatches)
                                    {
                                        int itemIndex = listClients.FindStringExact(winner_Name + "-" + scoreString);
                                        if (itemIndex != -1)
                                        {
                                            listClients.Items[itemIndex] = winner_Name + "-" + (score + 1).ToString();
                                        }
                                    }
                                });
                            }
                        }
                    }
                    catch
                    {
                        tbActivity.AppendText($"\"{clientName}\" has disconnected, but failed to notify \"{winner_Name}\" that they won.", Color.Purple);
                    }

                    gameClientPairs.Remove(winner_Name);
                    unavailableClients.Remove(winner_Name);
                }
            }
        }

        //Remove clientName from the recievedChallenge list, then notify the challenger that clientName disconnected
        private void removeFrom_recievedChallenge_DC(string clientName)
        {
            byte[] bytesToWrite = ASCIIEncoding.ASCII.GetBytes("DX" + clientName + "\0");

            string challenger_Name = recievedChallenge[clientName];
            NetworkStream challenger_networkStream = (NetworkStream)clientDatabase[challenger_Name];

            try
            {
                challenger_networkStream.Write(bytesToWrite, 0, bytesToWrite.Length);
                tbActivity.AppendText($"Client \"{clientName}\" is no longer connected, notified \"{challenger_Name}\".", Color.Black);
            }
            catch
            {
                tbActivity.AppendText($"Client \"{clientName}\" is no longer connected, failed to notify \"{challenger_Name}\".", Color.Purple);
            }

            unavailableClients.Remove(challenger_Name);

            recievedChallenge.Remove(clientName);
        }

        //Returns opponent name for the given client name, blank if no opponent exists
        private string getOpponentName(string clientName)
        {
            string opponentName;

            if (!gameClientPairs.ContainsKey(clientName))
            {
                foreach (var clientItem in gameClientPairs.Where(kvp => kvp.Value == clientName).Take(1).ToList())
                {
                    opponentName = clientItem.Key;
                    return opponentName;
                }
            }
            else if (gameClientPairs.ContainsKey(clientName))
            {
                opponentName = gameClientPairs[clientName];
                return opponentName;
            }

            return "";
        }
    }
}
