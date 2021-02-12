using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace TicTacToeServer
{
    class Program
    {

        private static List<Player> connectedPlayers = new List<Player>();
        private static object lockOn = new object();

        static void Main(string[] args)
        {

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 45555);
            listener.Bind(endPoint);
            listener.Listen();

            Thread thread = new Thread(ConnectedPlayersHandler);
            thread.IsBackground = true;
            thread.Start();

            Console.WriteLine("Ожидание подключений...\n");

            uint playerID = 0;

            while (true)
            {
                Socket client = listener.Accept();
                lock (lockOn) 
                {
                    connectedPlayers.Add(new Player(client, playerID));
                    Console.WriteLine("Игрок " + playerID + " присоединился.");
                } 
                playerID++;
            }
        }

        public static void ConnectedPlayersHandler()
        {

            while (true)
            {
                lock (lockOn)
                {
                    if (connectedPlayers.Count >= 2)
                    {
                        try
                        {
                            connectedPlayers[0].Socket.Send(new byte[1]);
                            connectedPlayers[0].Socket.Send(new byte[1]);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Игрок " + connectedPlayers[0].ID + " отсоединился.");
                            connectedPlayers.Remove(connectedPlayers[0]);
                            continue;
                        }

                        try
                        {
                            connectedPlayers[1].Socket.Send(new byte[1]);
                            connectedPlayers[1].Socket.Send(new byte[1]);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Игрок " + connectedPlayers[1].ID + " отсоединился.");
                            connectedPlayers.Remove(connectedPlayers[0]);
                            continue;
                        }

                        new GameRoom(connectedPlayers[0], connectedPlayers[1]);
                        Console.WriteLine("Игровая комната для игрока " + connectedPlayers[0].ID + " и " 
                            + "игрока " + connectedPlayers[1].ID + " создана.");
                        connectedPlayers.RemoveRange(0, 2);
                    }
                }

                Thread.Sleep(10);
            }
        }
    }

    class Player
    {

        public uint ID { get; }

        public Socket Socket { get; set; }
        
        public Player(Socket socket, uint id)
        {
            Socket = socket;
            ID = id;
        }
    }

    class GameRoom
    {
        private Player player1;
        private Player player2;

        private byte[,] gameGrid = new byte[3, 3];

        private PlayerTurn turn;
        private PlayerActions playerAction = PlayerActions.NOP;

        public GameRoom(Player player1, Player player2)
        {

            if (new Random().Next(1, 3) == 1)
            {
                turn = PlayerTurn.First;
                player1.Socket.Send(new byte[1] { 1 });
                player2.Socket.Send(new byte[1] { 0 });
            }
            else 
            {
                turn = PlayerTurn.Second;
                player1.Socket.Send(new byte[1] { 0 });
                player2.Socket.Send(new byte[1] { 1 });
            }

            this.player1 = player1;
            this.player2 = player2;

            Thread thread = new Thread(Update);
            thread.IsBackground = true;
            thread.Start();
        }

        private bool CheckForDraw()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (gameGrid[i, j] == 0) return false;
                }
            }

            return true;
        }

        private bool WinCheck(PlayerTurn playerTurn, out (byte a, byte b) cellNumbers)
        {
            byte turnNum = playerTurn == PlayerTurn.First ? 1 : 2;

            if (gameGrid[0, 0] == turnNum && gameGrid[0, 1] == turnNum && gameGrid[0, 2] == turnNum) 
            {
                cellNumbers = (1, 3);
                return true;
            } 
            if (gameGrid[1, 0] == turnNum && gameGrid[1, 1] == turnNum && gameGrid[1, 2] == turnNum)
            {
                cellNumbers = (4, 6);
                return true;
            }
            if (gameGrid[2, 0] == turnNum && gameGrid[2, 1] == turnNum && gameGrid[2, 2] == turnNum)
            {
                cellNumbers = (7, 9);
                return true;
            }

            if (gameGrid[0, 0] == turnNum && gameGrid[1, 0] == turnNum && gameGrid[2, 0] == turnNum)
            {
                cellNumbers = (1, 7);
                return true;
            }
            if (gameGrid[0, 1] == turnNum && gameGrid[1, 1] == turnNum && gameGrid[2, 1] == turnNum)
            {
                cellNumbers = (2, 8);
                return true;
            }
            if (gameGrid[0, 2] == turnNum && gameGrid[1, 2] == turnNum && gameGrid[2, 2] == turnNum)
            {
                cellNumbers = (3, 9);
                return true;
            }

            if (gameGrid[0, 0] == turnNum && gameGrid[1, 1] == turnNum && gameGrid[2, 2] == turnNum)
            {
                cellNumbers = (1, 9);
                return true;
            }
            if (gameGrid[2, 0] == turnNum && gameGrid[1, 1] == turnNum && gameGrid[0, 2] == turnNum)
            {
                cellNumbers = (3, 7);
                return true;
            }

            cellNumbers = (0, 0);
            return false;
        }

        private void Update()
        {

            byte[] buffer = new byte[4];
            int pauseTime = 4000;

            while (true)
            {

                if (turn == PlayerTurn.First)
                {
                    try
                    {
                        player2.Socket.Receive(buffer);
                    }
                    catch (Exception)
                    {

                        Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                        try
                        {
                            player1.Socket.Receive(buffer);
                            buffer[0] = 13;
                            player1.Socket.Send(buffer);
                        }
                        catch (Exception)
                        {
                        }
                        Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                        Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                        return;
                    }

                    if ((PlayerActionsRequests)buffer[0] == PlayerActionsRequests.LeftTheGame)
                    {

                        Console.WriteLine("Игрок " + player2.ID + " покинул игру.");
                        try
                        {
                            player1.Socket.Receive(buffer);
                            buffer[0] = 13;
                            player1.Socket.Send(buffer);
                        }
                        catch (Exception)
                        {
                        }
                        Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                        Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                        Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                        return;
                    }

                    try
                    {
                        player1.Socket.Receive(buffer);
                    }
                    catch (Exception)
                    {

                        buffer[0] = (byte)PlayerActionsRequests.LeftTheGame;
                    }

                    switch ((PlayerActionsRequests)buffer[0])
                    {
                        case PlayerActionsRequests.NOP:
                            playerAction = PlayerActions.NOP;
                            break;

                        case PlayerActionsRequests.PressLeftTop:
                            if (gameGrid[0, 0] == 0)
                            {
                                gameGrid[0, 0] = 1;

                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 1;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 1;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeftTop;
                            }
                            break;

                        case PlayerActionsRequests.PressTop:
                            if (gameGrid[0, 1] == 0)
                            {
                                gameGrid[0, 1] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 2;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 2;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressTop;
                            }
                            break;

                        case PlayerActionsRequests.PressRightTop:
                            if (gameGrid[0, 2] == 0)
                            {
                                gameGrid[0, 2] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 3;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 3;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRightTop;
                            }
                            break;

                        case PlayerActionsRequests.PressLeft:
                            if (gameGrid[1, 0] == 0)
                            {
                                gameGrid[1, 0] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 4;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 4;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeft;
                            }
                            break;

                        case PlayerActionsRequests.PressCenter:
                            if (gameGrid[1, 1] == 0)
                            {
                                gameGrid[1, 1] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 5;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 5;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressCenter;
                            }
                            break;

                        case PlayerActionsRequests.PressRight:
                            if (gameGrid[1, 2] == 0)
                            {
                                gameGrid[1, 2] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 6;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 6;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRight;
                            }
                            break;

                        case PlayerActionsRequests.PressLeftBottom:
                            if (gameGrid[2, 0] == 0)
                            {
                                gameGrid[2, 0] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 7;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 7;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeftBottom;
                            }
                            break;

                        case PlayerActionsRequests.PressBottom:
                            if (gameGrid[2, 1] == 0)
                            {
                                gameGrid[2, 1] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 8;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 8;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressBottom;
                            }
                            break;

                        case PlayerActionsRequests.PressRightBottom:
                            if (gameGrid[2, 2] == 0)
                            {
                                gameGrid[2, 2] = 1;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 9;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 9;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRightBottom;
                            }
                            break;

                        case PlayerActionsRequests.LeftTheGame:
                            playerAction = PlayerActions.LeftTheGame;
                            break;
                    }

                    switch (playerAction)
                    {
                        case PlayerActions.NOP:
                            buffer[0] = 0;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            break;

                        case PlayerActions.PressLeftTop:
                            buffer[0] = 1;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressTop:
                            buffer[0] = 2;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressRightTop:
                            buffer[0] = 3;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressLeft:
                            buffer[0] = 4;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressCenter:
                            buffer[0] = 5;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressRight:
                            buffer[0] = 6;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressLeftBottom:
                            buffer[0] = 7;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressBottom:
                            buffer[0] = 8;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.PressRightBottom:
                            buffer[0] = 9;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.Win:
                            buffer[0] = 10;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            buffer[0] = 11;
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            Thread.Sleep(pauseTime);
                            gameGrid = new byte[3, 3];
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            break;

                        case PlayerActions.Lose:
                            break;

                        case PlayerActions.Draw:
                            buffer[0] = 12;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            Thread.Sleep(pauseTime);
                            gameGrid = new byte[3, 3];
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            turn = PlayerTurn.Second;
                            break;

                        case PlayerActions.LeftTheGame:
                            buffer[0] = 13;
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {
                            }
                            Console.WriteLine("Игрок " + player1.ID + " покинул игру.");
                            Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                            Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                            Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                            return;
                    }

                }
                else
                {

                    try
                    {
                        player1.Socket.Receive(buffer);
                    }
                    catch (Exception)
                    {

                        Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                        try
                        {
                            player2.Socket.Receive(buffer);
                            buffer[0] = 13;
                            player2.Socket.Send(buffer);
                        }
                        catch (Exception)
                        {
                        }
                        Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                        Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                        return;
                    }

                    if ((PlayerActionsRequests)buffer[0] == PlayerActionsRequests.LeftTheGame)
                    {

                        Console.WriteLine("Игрок " + player1.ID + " покинул игру.");
                        try
                        {
                            player2.Socket.Receive(buffer);
                            buffer[0] = 13;
                            player2.Socket.Send(buffer);
                        }
                        catch (Exception)
                        {
                        }
                        Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                        Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                        Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                        return;
                    }

                    try
                    {
                        player2.Socket.Receive(buffer);
                    }
                    catch (Exception)
                    {

                        buffer[0] = (byte)PlayerActionsRequests.LeftTheGame;
                    }

                    switch ((PlayerActionsRequests)buffer[0])
                    {
                        case PlayerActionsRequests.NOP:
                            playerAction = PlayerActions.NOP;
                            break;

                        case PlayerActionsRequests.PressLeftTop:
                            if (gameGrid[0, 0] == 0)
                            {
                                gameGrid[0, 0] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 1;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 1;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeftTop;
                            }
                            break;

                        case PlayerActionsRequests.PressTop:
                            if (gameGrid[0, 1] == 0)
                            {
                                gameGrid[0, 1] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 2;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 2;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressTop;
                            }
                            break;

                        case PlayerActionsRequests.PressRightTop:
                            if (gameGrid[0, 2] == 0)
                            {
                                gameGrid[0, 2] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 3;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 3;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRightTop;
                            }
                            break;

                        case PlayerActionsRequests.PressLeft:
                            if (gameGrid[1, 0] == 0)
                            {
                                gameGrid[1, 0] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 4;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 4;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeft;
                            }
                            break;

                        case PlayerActionsRequests.PressCenter:
                            if (gameGrid[1, 1] == 0)
                            {
                                gameGrid[1, 1] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 5;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 5;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressCenter;
                            }
                            break;

                        case PlayerActionsRequests.PressRight:
                            if (gameGrid[1, 2] == 0)
                            {
                                gameGrid[1, 2] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 6;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 6;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRight;
                            }
                            break;

                        case PlayerActionsRequests.PressLeftBottom:
                            if (gameGrid[2, 0] == 0)
                            {
                                gameGrid[2, 0] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 7;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 7;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressLeftBottom;
                            }
                            break;

                        case PlayerActionsRequests.PressBottom:
                            if (gameGrid[2, 1] == 0)
                            {
                                gameGrid[2, 1] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 8;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 8;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressBottom;
                            }
                            break;

                        case PlayerActionsRequests.PressRightBottom:
                            if (gameGrid[2, 2] == 0)
                            {
                                gameGrid[2, 2] = 2;
                                if (WinCheck(turn, out (byte a, byte b) cellNumbers))
                                {
                                    buffer[1] = 9;
                                    buffer[2] = cellNumbers.a;
                                    buffer[3] = cellNumbers.b;
                                    playerAction = PlayerActions.Win;
                                }
                                else if (CheckForDraw())
                                {
                                    buffer[1] = 9;
                                    playerAction = PlayerActions.Draw;
                                }
                                else playerAction = PlayerActions.PressRightBottom;
                            }
                            break;

                        case PlayerActionsRequests.LeftTheGame:
                            playerAction = PlayerActions.LeftTheGame;
                            break;
                    }

                    switch (playerAction)
                    {
                        case PlayerActions.NOP:
                            buffer[0] = 0;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            break;

                        case PlayerActions.PressLeftTop:
                            buffer[0] = 1;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressTop:
                            buffer[0] = 2;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressRightTop:
                            buffer[0] = 3;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressLeft:
                            buffer[0] = 4;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressCenter:
                            buffer[0] = 5;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressRight:
                            buffer[0] = 6;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressLeftBottom:
                            buffer[0] = 7;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressBottom:
                            buffer[0] = 8;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.PressRightBottom:
                            buffer[0] = 9;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.Win:
                            buffer[0] = 10;
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            buffer[0] = 11;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            Thread.Sleep(pauseTime);
                            gameGrid = new byte[3, 3];
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            break;

                        case PlayerActions.Lose:
                            break;

                        case PlayerActions.Draw:
                            buffer[0] = 12;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            playerAction = PlayerActions.NOP;
                            Thread.Sleep(pauseTime);
                            gameGrid = new byte[3, 3];
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player1.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player2.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                                return;
                            }
                            try
                            {
                                player2.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("Игрок " + player2.ID + " отсоединился.");
                                try
                                {
                                    buffer[0] = 13;
                                    player1.Socket.Send(buffer);
                                }
                                catch (Exception)
                                {
                                }
                                Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                                Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                                return;
                            }
                            turn = PlayerTurn.First;
                            break;

                        case PlayerActions.LeftTheGame:
                            buffer[0] = 13;
                            try
                            {
                                player1.Socket.Send(buffer);
                            }
                            catch (Exception)
                            {
                            }
                            Console.WriteLine("Игрок " + player2.ID + " покинул игру.");
                            Console.WriteLine("Игровая комната для игрока " + player1.ID + " и " + "игрока " + player2.ID + " расформирована.");
                            Console.WriteLine("Игрок " + player1.ID + " отсоединен.");
                            Console.WriteLine("Игрок " + player2.ID + " отсоединен.");
                            return;
                    }
                }


                Thread.Sleep(100);
            }
        }

        enum PlayerTurn
        {
            First,
            Second
        }

        enum PlayerActionsRequests
        {
            NOP,
            PressLeftTop,
            PressTop,
            PressRightTop,
            PressLeft,
            PressCenter,
            PressRight,
            PressLeftBottom,
            PressBottom,
            PressRightBottom,
            LeftTheGame
        }

        enum PlayerActions
        {
            NOP,
            PressLeftTop,
            PressTop,
            PressRightTop,
            PressLeft,
            PressCenter,
            PressRight,
            PressLeftBottom,
            PressBottom,
            PressRightBottom,
            Win,
            Lose,
            Draw,
            LeftTheGame
        }
    }
}
