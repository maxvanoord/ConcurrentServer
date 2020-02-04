/*** Fill these lines with your complete information.
 * Note: Incomplete information may result in FAIL.
 * Mameber 1: Max van Oord
 * Mameber 2: Joeri Huigsloot
 * Std Number 1: 0966232
 * Std Number 2: 
 * Class: INF2E
 ***/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
/* Note: If you are using .net core 2.1, install System.Text.Json (use NuGet). */
using System.Text.Json; 
using System.Threading;

namespace SocketServer
{
    public class ClientInfo
    {
        public string studentnr { get; set; }
        public string classname { get; set; }
        public int clientid { get; set; }
        public string teamname { get; set; }
        public string ip { get; set; }
        public string secret { get; set; }
        public string status { get; set; }
    }

    public class Message
    {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class SequentialServer
    {
        public Socket listener; // listener is our server side Socket object where we listen on for client connections
        public IPEndPoint localEndPoint; // localEndPoint is a network binding between IP and PORT where our server listens on
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // we define our IP address to be local 
        public readonly int portNumber = 11111; // we define a port number 

        public String results = "";
        public LinkedList<ClientInfo> clients = new LinkedList<ClientInfo>(); // we define a list which holds all info about connected clients

        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 5;

        public void prepareServer()
        {
            byte[] bytes = new Byte[1024]; // we use bytes as our max size of incomming receive data
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool stop;

            try
            {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);
                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");
                    // Suspend while waiting for incoming connection 
                    Socket connection = listener.Accept();              // at this point the program is stuck on waiting 
                    this.sendReply(connection, Message.welcome);

                    stop = false;
                    while (!stop)
                    {
                        numByte = connection.Receive(bytes); // numByte is the encoded received message
                        data = Encoding.ASCII.GetString(bytes, 0, numByte); // data holds the decoded received message 
                        replyMsg = processMessage(data); 
                        if (replyMsg.Equals(Message.stopCommunication))
                        {
                            stop = true;
                            break;
                        }
                        else
                            this.sendReply(connection, replyMsg);
                    }

                }

            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }
        public void handleClient(Socket con)
        {
        }

        public string processMessage(String msg)
        {
            Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.stopCommunication:
                        replyMsg = Message.stopCommunication;
                        break;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        clients.AddLast(c);
                        if (c.clientid == -1)
                        {
                            stopCond = true;
                            exportResults();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }
        public void sendReply(Socket connection, string msg)
        {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            connection.Send(encodedMsg);
        }
        public void exportResults()
        {
            if (stopCond)
            {
                this.printClients();
            }
        }
        public void printClients()
        {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients)
            {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }
    }


    public class ConcurrentServer
    {
        public Socket listener; // listener is our server side Socket object where we listen on for client connections
        public IPEndPoint localEndPoint; // localEndPoint is a network binding between IP and PORT where our server listens on
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); 
        public readonly int portNumber = 11111; 

        public List<Thread> threadList = new List<Thread>(); // a collection of all running threads in our program
        public LinkedList<ClientInfo> clients = new LinkedList<ClientInfo>(); // a list which holds all info about connected clients

        public Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 250;

        public void prepareServer() {

            try
            {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);

                while (true)                                                    // main thread is only accepting connections and making threads 
                {   
                    Socket connection = listener.Accept();                      // main thread accepting a client's connection

                    threadList.Add(new Thread(() => handleClient(connection))); // adding a new thread to our program which is going to handle the connection

                    if (threadList.Count() > 0){
                        threadList.Last().Start();                              // starting the added thread 
                    }

                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

        public void handleClient(Socket connection)
        // this method is running for each connected client on a different thread
        {
            byte[] bytes = new Byte[1024]; 
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool run = true;

            this.sendReply(connection, Message.welcome);            // first we need to send the client a welcome message

            while (run)                                             // then we need to start checking for messages in a loop
            {
                numByte = connection.Receive(bytes);                // receiving a message from client's connection
                data = Encoding.ASCII.GetString(bytes, 0, numByte); 
                replyMsg = processMessage(data);                    // we generate a reply message with the method processMessage

                if (replyMsg.Equals(Message.stopCommunication))     // if the client returns with a 'stop' message we close the connection
                {
                    run = false;
                    break;
                }
                else 
                {
                    this.sendReply(connection, replyMsg);           // otherwise we reply to the client with a secret + status
                }   
            }
        }

        public string processMessage(String msg)
        // argument:    incomming message from a client
        // return:      message to sent back to client
        {
            //Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.stopCommunication:                 // if the message is a "COMC-STOP" we close the connection in HandleClient()
                        replyMsg = Message.stopCommunication;
                        break;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString()); // converting client's message to a ClientInfo object
                        clients.AddLast(c);
                        if (c.clientid == -1)
                        {
                            printClients();                         // when the last client connected we print info about all connected clients
                        }

                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }

        public void sendReply(Socket connection, string msg)
        {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            connection.Send(encodedMsg);
        }

        public void printClients()
        {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients)
            {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);
        }
    }


    public class ServerSimulator
    {
        public static void sequentialRun()
        {
            Console.Out.WriteLine("[Server] A sample server, sequential version ...");
            SequentialServer server = new SequentialServer();
            server.prepareServer();
        }
        public static void concurrentRun()
        {
            Console.Out.WriteLine("[Server] A sample server, concurrent version ...");
            ConcurrentServer server = new ConcurrentServer();
            server.prepareServer();
        }
    }
    class Program
    {
        // Main Method 
        static void Main(string[] args)
        {
            Console.Clear();
            //ServerSimulator.sequentialRun();
            // todo: uncomment this when the solution is ready.
            ServerSimulator.concurrentRun();
        }

    }
}
