using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class TcpServer {

	private const int SERVER_PORT = 13000;
	private const int CLIENT_TIMEOUT = 5000;
	private const int SERVICE_TIME = 2000;

	//
	// Take the specified processor time.
	//
	
	private static void TakeTime(int time, bool spinning) {
		if (spinning) {
			int until = Environment.TickCount + time;
			do {
				Thread.SpinWait(100);
			} while (Environment.TickCount < until);
		} else {
			Thread.Sleep(time);
		}
	}
	
	//
	// Processes a client's connection.
	//
	
	static void ProcessConnection(TcpClient client) {
	
		Console.WriteLine("-->thread #{0}", Thread.CurrentThread.ManagedThreadId);

		//
		// Get the in and out streams for read and write from/to the client socket.
		//
				
		StreamReader input = new StreamReader(client.GetStream());
		StreamWriter output = new StreamWriter(client.GetStream());

		//
		// Set the receive timeout.
		//

		client.ReceiveTimeout = CLIENT_TIMEOUT;
		
		//
		// Loop to receive all the data sent by the client that is delimited
		// by an empty line.
		// If timeout expires, a IOException is thrown.
		//
		
		string request;
		try {
			while ((request = input.ReadLine()) != null && request != String.Empty) {
		
				Console.WriteLine("Received: {0}", request);

				//
				// Process the data sent by the client, taking some time.
				//
					
				request = request.ToUpper();
				TakeTime(SERVICE_TIME, false);
				
				//
				// Send back the response.
				//
					
				output.WriteLine(request);
				output.Flush();
				Console.WriteLine("Sent: {0}", request);            
			}
		} catch (IOException) {
			Console.WriteLine("TIMEOUT!");
		} finally {
		
			//
			// Close the client's socket.
			//
		
			client.Close();
		}
		Console.WriteLine("<--thread #{0}", Thread.CurrentThread.ManagedThreadId);
	}
	
	//
	// Single threaded server.
	//
	
	static void SingleThreadedServer() {
	
		TcpListener server = null;   
		try	{

			//
			// Create a listen socket bound to the server port.
			//
			
			server = new TcpListener(IPAddress.Loopback, SERVER_PORT);
			
			//
			// Start listening for client requests.
			//
	
			server.Start();
						
			//
			// Enter the listening loop.
			//
			
			do {
			
				Console.WriteLine("+++waiting for new connection... ");
				
				//
				// Perform a blocking call to accept requests.
				//
				
				TcpClient client = server.AcceptTcpClient();

				//	
				// To process all connections on the current thread,
				// uncomment the following line.
				//
				 
				//ProcessConnection(client);
				
				//
				// To create a new thread for each connection, uncomment
				// the following line.
				//
				 
				new Thread((c) => ProcessConnection((TcpClient)c)).Start(client);
				
				//
				// To use worker thread pool's threads to process each
				// connection, uncomment the follwing lines.
				//
				 
				//ThreadPool.QueueUserWorkItem((c) => ProcessConnection((TcpClient)c), client);
			} while (true);
		} catch(SocketException e) {
			Console.WriteLine("***SocketException: {0}", e);
		} finally {
		
			//
			// Stop listening for new clients.
			//

			if (server != null) {
				server.Stop();
			}
		}
	}
	
	//
	// The multithreaded TCP server.
	//
	
	public static void MultiThreadedServer() {
	
		TcpListener server = null;   
		try	{
		
			//
			// Create a listen socket bound to the server port.
			//
			
			server = new TcpListener(IPAddress.Loopback, SERVER_PORT);
			
			//
			// Start the first AcceptTcpClient
			//
			
			AsyncCallback OnAccept = null;
			
			OnAccept = delegate (IAsyncResult ar) {
				TcpClient client = null;
				try {
		
					//
					// Get the socket that will be used to communicate with the connected client.
					//
			
					client = server.EndAcceptTcpClient(ar);
				} catch (ObjectDisposedException) {
		
					//
					// Accept callback was called because the server socket was closed, so return.
					//
					
					return;
				}

				//
				// The server is ready to accepts a new connection.
				//

				server.BeginAcceptTcpClient(OnAccept, null);
		
				//
				// Process the current connection.
				//
		
				ProcessConnection(client);			
			};
			
			//
			// Start listening for client requests.
			//
			
			server.Start();
			server.BeginAcceptTcpClient(OnAccept, null);
			
			//
			// This thread is free to do anything we need.
			//
			
			Console.WriteLine("Hit enter to terminate the server...");			
			Console.ReadLine();

			//
			// Close the listener socket.
			//
			
			server.Stop();
		} catch(SocketException e) {
			Console.WriteLine("SocketException: {0}", e);
		}
	}

	//
	// The entry point.
	//
	
	static void Main() {
		//SingleThreadedServer();
		MultiThreadedServer();
		Thread.Sleep(200);
	}
}
