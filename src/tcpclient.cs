using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client {
	private const int SERVER_PORT = 13000;

	//
	// Processes a server's request.
	//
	
	static void ProcessRequest(string message) {
		TcpClient client = new TcpClient();
		
		try  {

			//
			// Connect to the server on localhost:SERVER_PORT.
			//
			
			client.Connect(IPAddress.Loopback, SERVER_PORT);

			//
			// Get an output stream reader to send data to the server.
			//
			
			StreamWriter output = new StreamWriter(client.GetStream());			
			output.WriteLine(message);
			output.WriteLine();
			Console.WriteLine("Sent: {0}", message); 
			output.Flush();
			
			//
			// Read one line of the server response.
			//
			
			StreamReader input = new StreamReader(client.GetStream());
			string response = input.ReadLine();
			Console.WriteLine("Received: {0}", response);
		} catch (Exception exn) {
			Console.WriteLine("EXCEPTION: {0}", exn);
		} finally {
		
			//
			// Close the client socket.
			//
			
			if (client != null) {
				client.Close();
			}
		}
	}
	

	static void Main2(string[] args) {
		ProcessRequest((args.Length != 0) ? args[0] : "1a2b3c4d5e6f7g8h9i");
	}

	//
	// Concurrent requests.
	//
	
	const int CONCURRENT_REQUESTS = 50;
	
	static void Main(string[] args) {
		string message = (args.Length != 0) ? args[0] : "1a2b3c4d5e6f7g8h9i";
		Thread[] clients = new Thread[CONCURRENT_REQUESTS];
		do {
			for (int i = 0; i < CONCURRENT_REQUESTS; i++) {
				clients[i] = new Thread((n) => ProcessRequest("[" + n.ToString() +"]" + message));
				clients[i].Start(i);
			}
			for (int i = 0; i < CONCURRENT_REQUESTS; i++) {
				clients[i].Join();
			}
			if (Console.KeyAvailable) {
				break;
			}
		} while (true);
	}
}
