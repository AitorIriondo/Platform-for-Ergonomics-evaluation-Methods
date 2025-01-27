using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Platform_for_Ergonomics_evaluation_Methods.Utils;
using Platform_for_Ergonomics_evaluation_Methods.Models;
using Platform_for_Ergonomics_evaluation_Methods.Services;
using System.Diagnostics;

namespace Platform_for_Ergonomics_evaluation_Methods.Services
{
    public class TcpServerService
    {
        static void WriteLine(string line)
        {
            Console.WriteLine(line);
            Debug.WriteLine(line);

        }
        private TcpListener _tcpListener;
        private readonly JsonDeserializer _jsonDeserializer;
        private readonly MessageStorageService _messageStorageService;

        public TcpServerService(JsonDeserializer jsonDeserializer, MessageStorageService messageStorageService)
        {
            _tcpListener = new TcpListener(System.Net.IPAddress.Any, 5050); // Port 5050
            _jsonDeserializer = new JsonDeserializer();  // Initialize the deserializer
            _messageStorageService = messageStorageService;
        }

        public void StartServer()
        {
            Task.Run(() => ListenForClients());
        }

        private async Task ListenForClients()
        {
            _tcpListener.Start();
            WriteLine("TCP Server is running...");

            while (true)
            {
                WriteLine("Waiting for client..");
                TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                WriteLine("Client connected");
                HandleClient(client);
            }
        }

        private async void HandleClient(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    string receivedText = await reader.ReadToEndAsync();
                    string jsonMessage = receivedText;
                    // When using a browser or curl we need to get rid of the header
                    if (receivedText.StartsWith("POST"))
                    {
                        string[] splitters = new string[] { "\n\n", "\r\n\r\n" };
                        foreach (string splitter in splitters)
                        {
                            int splitPos = receivedText.IndexOf(splitter);
                            if (splitPos > 0)
                            {
                                jsonMessage = receivedText.Substring(splitPos + splitter.Length);
                                ManikinManager.ParseMessage(jsonMessage);
                            }

                        }
                    }
                    Debug.WriteLine("Received JSON message: " + jsonMessage);

                    // Store the latest message for the web UI
                    _messageStorageService.StoreMessage(jsonMessage);

                    // Use the JsonDeserializer to convert the JSON message into a Manikin object
                    Manikin manikin = _jsonDeserializer.DeserializeManikin(jsonMessage);

                    // Delegate the processing of the Manikin object to the JsonDeserializer
                    _jsonDeserializer.ProcessManikin(manikin);
                    string responseMessage = "HTTP/1.1 200 OK\r\n" +
                                                         "Content-Type: application/json\r\n" +
                                                         "Content-Length: 13\r\n" +
                                                         "\r\n" +
                                                         "{\"status\":\"ok\"}";

                    await writer.WriteAsync(responseMessage);
                    await writer.FlushAsync();
                    WriteLine(responseMessage);
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
            WriteLine("Handle Client Done");

        }
    }
}
