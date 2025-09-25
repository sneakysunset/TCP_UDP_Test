using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

    // ==================== SERVEUR UDP ====================
public class UdpServer
{
    private UdpClient _udpServer = new UdpClient();
    private bool _isRunning;
    public async Task StartAsync(int port)
    {
        _udpServer = new UdpClient(port);
        _isRunning = true;
        Console.WriteLine($"Serveur UDP démarré sur le port {port}");
        while (_isRunning)
        {
            try
            {
                // Recevoir des données
                var result = await _udpServer.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);
                var clientEndpoint = result.RemoteEndPoint;
                Console.WriteLine($"Message reçu de {clientEndpoint} : {message}");
                // Envoyer une réponse
                var response = $"Echo: {message}";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await _udpServer.SendAsync(responseBytes, responseBytes.Length, clientEndpoint);
                Console.WriteLine($"Réponse envoyée à {clientEndpoint}");
            }
            catch (ObjectDisposedException)
            {
                // Le socket a été fermé
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur serveur UDP : {ex.Message}");
            }
        }
    }
    public void Stop()
    {
        _isRunning = false;
        _udpServer?.Close();
    }
}
// ==================== CLIENT UDP ====================
public class UdpClientExample
{
    public async Task SendMessagesAsync(string serverAddress, int port)
    {
        using var udpClient = new UdpClient();
        var serverEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
        try
        {
            // Envoyer plusieurs messages
            await SendMessageAsync(udpClient, serverEndpoint, "Bonjour serveur UDP!");
            await SendMessageAsync(udpClient, serverEndpoint, "Message numéro 2");
            await SendMessageAsync(udpClient, serverEndpoint, "Dernier message");
            // Recevoir les réponses
            for (int i = 0; i < 3; i++)
            {
                var result = await udpClient.ReceiveAsync();
                var response = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine($"Réponse reçue : {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur client UDP : {ex.Message}");
        }
    }
    private async Task SendMessageAsync(UdpClient client, IPEndPoint endpoint, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(bytes, bytes.Length, endpoint);
        Console.WriteLine($"Message envoyé : {message}");
    }
}
// ==================== VERSION UDP AVANCÉE AVEC TIMEOUT ====================
public class UdpClientWithTimeout
{
    private readonly int _timeoutMs;
    public UdpClientWithTimeout(int timeoutMs = 5000)
    {
        _timeoutMs = timeoutMs;
    }
    public async Task<string> SendWithResponseAsync(string serverAddress, int port, string message)
    {
        using var udpClient = new UdpClient();
        var serverEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
        try
        {
            // Envoyer le message
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await udpClient.SendAsync(messageBytes, messageBytes.Length, serverEndpoint);
            // Attendre la réponse avec timeout
            var receiveTask = udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(_timeoutMs);
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Timeout après {_timeoutMs}ms");
            }
            var result = await receiveTask;
            return Encoding.UTF8.GetString(result.Buffer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
            throw;
        }
    }
}
// ==================== SERVEUR UDP MULTICAST ====================
public class UdpMulticastServer
{
    private UdpClient _udpClient = new UdpClient();
    private IPEndPoint? _multicastEndpoint;
    private bool _isRunning;
    public async Task StartAsync(string multicastAddress, int port)
    {
        _multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
        _udpClient = new UdpClient();
        _udpClient.JoinMulticastGroup(IPAddress.Parse(multicastAddress));
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        _isRunning = true;
        Console.WriteLine($"Serveur multicast démarré sur {multicastAddress}:{port}");
        while (_isRunning)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine($"Message multicast reçu : {message}");
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }
    public async Task BroadcastAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _udpClient.SendAsync(bytes, bytes.Length, _multicastEndpoint);
    }
    public void Stop()
    {
        _isRunning = false;
        _udpClient?.Close();
    }
}

// ==================== EXEMPLE D'UTILISATION UDP ====================
public class UdpExample
{
    public static async Task RunExample()
    {
        var server = new UdpServer();
        // Démarrer le serveur
        var serverTask = server.StartAsync(8081);
        // Attendre un peu pour que le serveur démarre
        await Task.Delay(1000);
        // Connecter un client
        var client = new UdpClientExample();
        await client.SendMessagesAsync("127.0.0.1", 8081);
        // Test avec timeout
        var clientWithTimeout = new UdpClientWithTimeout(3000);
        try
        {
            var response = await clientWithTimeout.SendWithResponseAsync("127.0.0.1", 8081, "Test timeout");
            Console.WriteLine($"Réponse avec timeout : {response}");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"Timeout : {ex.Message}");
        }
        // Arrêter le serveur
        server.Stop();
    }
}
