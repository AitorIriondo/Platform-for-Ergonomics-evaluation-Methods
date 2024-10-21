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

namespace Platform_for_Ergonomics_evaluation_Methods.Services
{
    public class TcpServerService
    {
        private TcpListener _tcpListener;
        private readonly JsonDeserializer _jsonDeserializer;
        private readonly MessageStorageService _messageStorageService;

        public TcpServerService(JsonDeserializer jsonDeserializer, MessageStorageService messageStorageService)
        {
            _tcpListener = new TcpListener(System.Net.IPAddress.Any, 5000); // Port 5000
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
            Console.WriteLine("TCP Server is running...");

            while (true)
            {
                TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected");
                HandleClient(client);
            }
        }

        private async void HandleClient(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string jsonMessage = await reader.ReadToEndAsync();
                    Console.WriteLine("Received JSON message: " + jsonMessage);

                    // Store the latest message for the web UI
                    _messageStorageService.StoreMessage(jsonMessage);

                    // Use the JsonDeserializer to convert the JSON message into a Manikin object
                    Manikin manikin = _jsonDeserializer.DeserializeManikin(jsonMessage);

                    // Delegate the processing of the Manikin object to the JsonDeserializer
                    _jsonDeserializer.ProcessManikin(manikin);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
