/*
 *
 * Data Comm Project I
 * FTP Client
 * Jesse Martinez (jem4687)
 *
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;

/// <summary>
/// This class contains all methods to create a connection to an FTP server
/// </summary>
public class FTP
{
	/////////////////////////////////////////////////////////////
	// 	Class Variables
	/////////////////////////////////////////////////////////////

	private const string prompt = ">";
	
	private int port;
	private int dataPort;
	private bool debug = false;
	private bool pasvMode = true;
	private string transferMode = "ASCII";
	private string hostName;
	
	private IPAddress hostIP;
	private IPAddress dataIP;
	private TcpClient messageClient;
	private TcpClient dataSocket;
	private TcpListener dataSocketListener;
	private StreamReader reader;
	private StreamWriter writer;
	
	private Dictionary<string, Command> commands;

	/// <summary>
	/// Non-Default Constructor
	///
	/// Constructor attempts to create a connection to the given hostName
	/// on the given port (21 if no port given)
	/// </summary>
	///
	/// <param name="hostName"> a string equal to the host you are trying 
	///                         to connect to
	/// </param>
	/// <param name="port"> an integer for the port you want to connect to
	///  			(21 if no port is sent)
	/// </param>
	public FTP(string hostName, int port = 21)
	{
		dataSocket = null;
		dataSocketListener = null;
		this.hostName = hostName;
		this.port = port;
		hostIP = Dns.GetHostEntry(hostName).AddressList[0];
		dataSocket = null;
		fillCommands();
		attemptConnection();
	}

	/// <summary>
	/// FillCommands - fills the commands Dictionary with all implemented 
	///		   commands
	/// </summary>
	private void fillCommands()
	{
		commands = new Dictionary<string, Command>();
		commands.Add("ASCII", new Command("ASCII", "TYPE A", "ASCII command sends the command 'TYPE A' - used to change download mode of server to ASCII", 0));
		commands.Add("BINARY", new Command("BINARY", "TYPE I","BINARY commands sends the command 'TYPE I' - used to change download mode of server to BINARY", 0));
		commands.Add("CD", new Command("CD", "CWD", "CD sends the command CWD <directory> - changes directory to given directory", 1));
		commands.Add("CDUP", new Command("CDUP", "CDUP", "CDUP sends the command CDUP - moves to parent directory", 0));
		commands.Add("DEBUG", new Command("DEBUG", "DEBUG", "DEBUG toggles on and off client debug mode", 0));
		commands.Add("DIR", new Command("DIR", "LIST", "DIR sends  the command 'LIST' - prints all files and folders in current directory", 0));
		commands.Add("GET", new Command("GET", "RETR", "GET sends the command 'RETR' <filename> - sends a command to start downloading the given file name", 1));
		commands.Add("HELP", new Command("HELP", "HELP", "HELP sends the 'HELP' command - use to retrieve server help", 0));
		commands.Add("-HELP", new Command("-HELP", "-HELP", "-HELP used to print commands client accepts, -HELP <command> prints the commands help info", 1));
		commands.Add("PASSIVE", new Command("PASSIVE", "PASV", "PASV toggles active and passive client data connection", 0));
		commands.Add("PWD", new Command("PWD", "PWD", "PWD sends the 'PWD' command - print current directory", 0));
		commands.Add("QUIT", new Command("QUIT", "QUIT", "QUIT disconnects the client from the server", 0));
		commands.Add("USER", new Command("USER", "USER", "USER sends the 'USER' <username> command", 1));
		commands.Add("PASS", new Command("PASS", "PASS", "PASS sends the 'PASS' <password> command", 1));
	}

	/// <summary>
	/// Connect - connects to the given hostname on the given port 
	///           (21 if not specified)
	/// </summary>
	/// <param name="hostName"> string - name of host to connect to
	/// </param>
	/// <param name="port"> int - port to connect to (21 if not specified)
	/// </param>
	public void connect(string hostName, int port = 21)
	{
		this.hostName = hostName;
		this.port = port;
		hostIP = Dns.GetHostEntry(hostName).AddressList[0];
		attemptConnection();
	}

	/// <summary>
	/// attemptConnection - attempt to connect to an FTP server
	/// </summary>
	private void attemptConnection()
	{
		Console.WriteLine("Trying " + hostIP);
		try{
			messageClient = new TcpClient(hostName, port);
			reader = new StreamReader(messageClient.GetStream());
			writer = new StreamWriter(messageClient.GetStream());
			Console.WriteLine("Successfully Connected to " + hostName);
			Console.WriteLine("Begin with USER <username> and PASS <password> commands");
		}
		catch(SocketException e1)
		{
			Console.WriteLine("Connection Failed");
			if(debug)
				Console.WriteLine(e1.ToString());
			closeConnection();
			System.Environment.Exit(1);
		}
	}

	/// <summary>
	/// sendCMD - attempts to send a command to an FTP server, catchs an
	///	      exception and attempts to reconnect to server if 
	///	      connection errors for some reason
	/// </summary>
	/// <param name="CMD">string - command to send to server</param>
	/// <return>true - if command was sent succesfully, false if connection
	///                was lost
	/// </return>
	private bool sendCMD(string CMD)
	{
		try
		{
			writer.WriteLine(CMD);
			writer.Flush();
			if(debug)
				Console.WriteLine(CMD);
			return true;
		}
		catch(Exception e)
		{
			Console.WriteLine("Connection lost....reconnecting to " + hostName);
			attemptConnection();
			if(debug)
				Console.WriteLine(e.ToString());
			return false;
		}
	}

	private void printHelp(string[] userInput)
	{
		if(userInput.Length == 1)
		{
			Console.WriteLine("Commands recognized by this client");
			foreach (KeyValuePair<string, Command> pair in commands)
				Console.Write(pair.Key + " ");
			Console.WriteLine();
		}
		else
		{	
			for(int i = 1; i < userInput.Length; i++)
			{
				if(commands.ContainsKey(userInput[i].ToUpper()))
					Console.WriteLine(userInput[i].ToUpper() + " - " + commands[userInput[i].ToUpper()].getHelp());
				else
					Console.WriteLine("Command " + userInput[i] + " not recognized");
			}
		}
	}

	/// <summary>
	/// generateIPString - returns a string representation of the IP 
	///		       address your listening on, replacing all 
	///                    '.' with ','
	/// </summary>
	/// <return>string representation of IP address</return>
	private string generateIPString()
	{
		string host = Dns.GetHostName();
		IPAddress[] ips = Dns.GetHostAddresses(host);
		foreach(IPAddress ip in ips)
			if(ip.AddressFamily == AddressFamily.InterNetwork)
			{
				dataIP = ip;
				return dataIP.ToString().Replace('.', ',');
			}
		return null;
	}

	/// <summary>
	/// generatePortString - returns a string representation of the port 
	///			 your listening on split into p1,p2 for the 
	///			 port command
	/// </summary>
	/// <return>string representation of port</return>
	private string generatePortString()
	{
		dataPort = ((IPEndPoint)dataSocketListener.LocalEndpoint).Port;

		int p1 = dataPort / 256;
		int p2 = dataPort % 256;

		return "," + p1 + "," + p2;
	}

	/// <summary>
	/// toggleDebug - toggles on and off debug
	/// </summary>
	private void toggleDebug()
	{
		debug = !debug;
		if(debug)
			Console.WriteLine("Debug Toggled On");
		else
			Console.WriteLine("Debug Toggled Off");
	}

	/// <summary>
	/// setTrasnferMode - set the the given transfer mode (Binary or Ascii)
	/// </summary>
	/// <param name="mode">string - mode to switch to (Binary or Ascii)
	/// </param>
	private void setTransferMode(string mode)
	{
		transferMode = mode;
	}

	/// <summary>
	/// togglePassive - switchs between passive and active data connections
	/// </summary>
	private void togglePassive()
	{
		pasvMode = !pasvMode;
		if(pasvMode)
			Console.WriteLine("Switched to Passive Connection");
		else
			Console.WriteLine("Switched to Active Connection");
	}

	/// <summary>
	/// createDataConnection - call the proper function to create a 
	///			   data connection 
	///			   to the FTP server in either passive or 
	///			   active mode
	/// </summary>
	private void createDataConnection(string[] input)
	{
		
		if(pasvMode)
		{
			passiveConnection();
			if(debug)
				Console.WriteLine("Passive Connection Established");
		}
		else
		{
			activeConnection();
			sendCMD(string.Join(" ",input));
			dataSocket = dataSocketListener.AcceptTcpClient();
			dataSocketListener.Stop();
			dataSocketListener = null;
			if(input[0].ToUpper().Equals("LIST"))
				listing();
			else if(input[0].ToUpper().Equals("GET"))
				retrieveFile(input[1]);
			if(debug)
				Console.WriteLine("Active Connection Established");
		}
	}

	/// <summary>
	/// activeConnection - creates an active data connection to an 
	///		       FTP server using a random open IP and port
	/// </summary>
	private void activeConnection()
	{
		dataSocketListener = new TcpListener(IPAddress.Any, 0);
		dataSocketListener.Start();
		sendCMD("PORT " + generateIPString() + generatePortString());
		if(debug)
			Console.WriteLine("PORT " + generateIPString() + generatePortString());
	}

	/// <summary>
	/// passiveConnection - creates a passive data connection to an 
	///			FTP server, connecting on the IP and port 
	///			recieved by the server
	/// </summary>
	private void passiveConnection()
	{
		if(dataSocket != null)
			dataSocket.Close();
		
		if(sendCMD("PASV"))
		{
			string printIpPort = reader.ReadLine();
			if(debug)
				Console.WriteLine(printIpPort);
			string[] ipPort = printIpPort.Trim().Split(' ');
			ipPort = ipPort[4].Split(',');
			string ip = ipPort[0].Substring(1) + "." + ipPort[1] + "." + ipPort[2] + "." + ipPort[3];
			int port = (Convert.ToInt32(ipPort[4]) * 256) + Convert.ToInt32(ipPort[5].Substring(0,(ipPort[5].Length-1)));
			dataSocket = new TcpClient(ip,port);
		}
	}

	/// <summary>
	/// listing - prints all files in the current directory
	///	      (used with dir command)
	/// </summary>
	private void listing()
	{
		StreamReader dataReader = new StreamReader(dataSocket.GetStream());
		string item = "";
		do
		{
			item = dataReader.ReadLine();
			if(item != null)
				Console.WriteLine(item);
		}
		while(item != null);
		Console.WriteLine(reader.ReadLine());
		dataSocket.Close();
		dataSocket = null;
	}

	/// <summary>
	/// progressBar - prints out a cool progress bar for your current 
	///		  active download
	/// </summary>
	/// <param name="filesize">int - total file size</param>
	/// <param name="amountDownloaded">int - amount downloaded</param>
	private void progressBar(int fileSize, int amountDownloaded)
	{
		int saveCursorSize  = Console.CursorSize;
		Console.CursorSize = 0;
		int left = Console.CursorLeft;
		int bufferWidth = Console.BufferWidth - 5;
		decimal complete = (decimal)amountDownloaded/(decimal)fileSize;
		int numberOfChars = (int)Math.Floor(complete*bufferWidth);
		string progressBar = "";
		for(int i = 0; i < numberOfChars; i++)
			progressBar += "#";
		for(int i = 0; i < (bufferWidth - numberOfChars); i++)
			progressBar += " ";
		progressBar +=" " +  (int)(complete * 100) + "%";
		Console.Write(progressBar);
		if(complete * 100 == 100)
		{
			Console.WriteLine("");
			Console.CursorSize = saveCursorSize;
		}
		else
			Console.CursorLeft = left;
	}

	/// <summary>
	/// retrieveFile - downloads the file with received file name
	/// </summary>
	/// <param name="fileName">string - name of file to download</param>
	/// <return> true - if file downloaded succesfully, false - if file 
	///	     download was unsuccesful
	/// </return>
	private bool retrieveFile(string fileName)
	{
		string reply = reader.ReadLine().Trim();
		string[] initialMessage = reply.Split(' ');
		Console.WriteLine(reply);
		if(!reply.Substring(0,3).Equals("550") && dataSocket != null)
		{
			retrieveFileBinary(fileName, Convert.ToInt32(initialMessage[initialMessage.Length - 2].Substring(1)));
			return true;
		}
		
		return false;
	}

	/// <summary>
	/// retrieveFileBinary - downloads the given file
	/// </summary>
	/// <param name="fileName">string - name of file to download</param>
	/// <param name="fileLength">int - size of file to download</param>
	private void retrieveFileBinary(string fileName, int fileLength)
	{
		if(debug)
			Console.WriteLine("Downloading In Binary Mode");
		Stream dataReader = dataSocket.GetStream();
		FileStream fileStream = new FileStream(Directory.GetCurrentDirectory() + "/" + fileName, FileMode.Create, FileAccess.ReadWrite);
		Stream fileWriter = fileStream;
		Byte[] bytes = new Byte[fileLength];
		int length;
		int bytesRead = 0;
		while((length = dataReader.Read(bytes, 0, bytes.Length)) != 0)
		{
			bytesRead += length;
			fileWriter.Write(bytes, 0, length);
			progressBar(fileLength, bytesRead);
		}
		fileStream.Close();
		fileWriter.Close();

		Console.WriteLine("File Successfully Downloaded");
		dataSocket.Close();
		dataSocket = null;
	}

	/// <summary>
	/// closeConnection - closes all connections to the FTP server
	/// </summary>
	public void closeConnection()
	{
		messageClient.Close();
		if(dataSocket != null)
			dataSocket.Close();

		Console.WriteLine("Connection Closed Successfully");
	}

	/// <summary>
	/// runFtp - runs the loop that begins commmunication between server and
	///	     client
	/// </summary>
	public void runFtp()
	{
		string reply;
		bool validCommand;
		bool moreInput;
		bool quitCommand = false;
		bool isLoggedIn = false;

		if(messageClient == null)
			Console.WriteLine("Please Connect to FTP Server");
		else
		{
			do
			{
				string initialReply = "";
				bool closedReply = false;
				do
				{	try
					{
						reply = reader.ReadLine().Trim();
						if((reply.Length == 3 && initialReply.Equals(reply)) || (reply.Length > 3 && reply[3] != '-' && Char.IsNumber(reply[0])))
						{
							closedReply = true;
						}
						if(initialReply.Equals(""))
						{
							if(reply.Length == 3 || 
								(reply.Length > 3 && reply[3] == ' '))
								closedReply = true;
							else
								initialReply = reply.Substring(0,3);
						}
						Console.WriteLine(reply);
						if(reply.Substring(0,3).Equals("150"))
							listing();
						if(reply.Substring(0,3).Equals("230"))
							isLoggedIn = true;
					}
					catch(Exception e)
					{
						if(debug)
							Console.WriteLine(e.ToString());
						Console.WriteLine("Connection to server lost");
						attemptConnection();
					}
				}while(closedReply == false);
				do
				{
					validCommand = false;
					moreInput = false;
					Console.Write(prompt);
					string[] input = Console.ReadLine().Trim().Split(' ');
					string command = input[0].ToUpper();
					if(command.Equals("-HELP") || (commands.ContainsKey(command) && commands[command].getParamLength() == (input.Length - 1)))
					{
						input[0] = commands[command].getSubstituteCommand();
						if(command.Equals("ASCII") || command.Equals("BINARY"))
							setTransferMode(command);		
						if((command.Equals("GET") || command.Equals("DIR")) && dataSocket == null && isLoggedIn )
						{	
							createDataConnection(input);
							if(!pasvMode)
								moreInput = true;
						}
						if(command.Equals("PASSIVE"))
						{
							togglePassive();
							moreInput = true;
						}
						else if(command.Equals("DEBUG"))
						{
							toggleDebug();
							moreInput = true;
						}
						else if(command.Equals("-HELP"))
						{
							printHelp(input);
							moreInput = true;
						}
						else
						{
							if(!sendCMD(String.Join(" ", input)))
								moreInput = true;
						}

						if(command.Equals("GET") && isLoggedIn)
							if(pasvMode && !retrieveFile(input[1]))
								moreInput = true;
						if(command.Equals("QUIT"))
						{
							quitCommand = true;
							break;
						}
						validCommand = true;
					}
					else
					{
						if(!commands.ContainsKey(command))
							Console.WriteLine(command + " is not a valid command");
						else
							Console.WriteLine("Wrong number of arguments for command " + command);
					}
				}while(validCommand == false || moreInput);
			}while(!quitCommand);

			closeConnection();
		}
	}

	////////////////////////////////////////////////////
	///		GETTERS & SETTERS
	////////////////////////////////////////////////////

	public int getPort(){return port;}
	public int getDataPort(){return dataPort;}
	public bool isDebug(){return debug;}
	public bool isPasvMode(){return pasvMode;}
	public string getTransferMode(){return transferMode;}
	public string getHostName(){return hostName;}
	public string getHostIP(){return hostIP.ToString();}
	public string getDataIP(){return dataIP.ToString();}
	
	////////////////////////////////////////////////////
	///		MAIN
	////////////////////////////////////////////////////
	static public void Main(string[] args)
	{
		if(args.Length == 0)
			Console.Error.WriteLine("usage - mono FTP.exe <ftp server>");
		else
		{
			FTP ftpClient = new FTP(args[0]);
			ftpClient.runFtp();
		}
	}
}
