using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TicTacToeTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 45555);
            Console.WriteLine("Подключение к серверу...");
            socket.Connect(endPoint);
            Console.WriteLine("Подключено к серверу!");
            Console.ReadKey();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            Console.WriteLine("Отключено от сервера");
            Console.ReadKey();
        }
    }
}
