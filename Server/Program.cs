using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server
{
    internal class Program
    {
        static int lastMove = -1;
        static bool legalMove;
        private static readonly ManualResetEventSlim moveReceived = new ManualResetEventSlim(false);
        static TcpListener? server;
        static void Main(string[] args)
        {
            // Wait for 2 clients to connect
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), 3360);
            server.Start();
            Console.WriteLine("Server is running!");
            Game(server);
        }

        static void Game(TcpListener server)
        {
            TcpClient client1 = server.AcceptTcpClient();
            Console.WriteLine("Client 1 connected!");
            TcpClient client2 = server.AcceptTcpClient();
            Console.WriteLine("Client 2 connected!");
            while (true)
            {
                Console.WriteLine("The Game Begins!");
                // Randomly choose which player starts
                // P1 = ✕, P2 = ◯
                Random rand = new Random();
                int currentPlayer = rand.Next(1, 3);
                Console.WriteLine($"Player {currentPlayer} begins the game.");
                SendData(currentPlayer == 1 ? client1 : client2, "FIRST");
                SendData(currentPlayer == 2 ? client1 : client2, "SECOND");
                int[] board = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 1 = ✕, 2 = ◯
                bool gameResolved = false;
                bool isDraw = false;
                Thread receiveT;
                while (!gameResolved)
                {
                    if (currentPlayer == 1)
                    {
                        SendData(client1, "rdy");
                        SendData(client2, "wait");
                        receiveT = new Thread(() => ReceiveData(client1));
                        receiveT.Start();
                        moveReceived.Wait();
                        moveReceived.Reset();
                    }
                    else
                    {
                        SendData(client1, "wait");
                        SendData(client2, "rdy");
                        receiveT = new Thread(() => ReceiveData(client2));
                        receiveT.Start();
                        moveReceived.Wait();
                        moveReceived.Reset();
                    }
                    while (!legalMove)
                    {
                        if (lastMove != -1 && board[lastMove] == 0) { legalMove = true; }
                    }
                    board[lastMove] = currentPlayer;
                    string readBoard = "";
                    foreach (int i in board) { readBoard += i + " "; }
                    Console.WriteLine($"Player {currentPlayer} checked the {lastMove} square");
                    Console.WriteLine($"Board: {readBoard}");
                    SendData(client1, "b" + readBoard);
                    SendData(client2, "b" + readBoard);
                    // Check if player won the game.
                    if (board[0] == currentPlayer && board[1] == currentPlayer && board[2] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[3] == currentPlayer && board[4] == currentPlayer && board[5] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[6] == currentPlayer && board[7] == currentPlayer && board[8] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[0] == currentPlayer && board[3] == currentPlayer && board[6] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[1] == currentPlayer && board[4] == currentPlayer && board[7] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[2] == currentPlayer && board[5] == currentPlayer && board[8] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[0] == currentPlayer && board[4] == currentPlayer && board[8] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    else if (board[2] == currentPlayer && board[4] == currentPlayer && board[6] == currentPlayer) { gameResolved = true; SendData(currentPlayer == 1 ? client1 : client2, "WIN"); SendData(currentPlayer == 1 ? client2 : client1, "LOSE"); }
                    // Check if a draw occured.
                    isDraw = true;
                    foreach (int i in board) { if (i == 0) { isDraw = false; break; } }
                    if (isDraw) { gameResolved = true; SendData(client1, "DRAW"); SendData(client2, "DRAW"); Console.WriteLine("The game ends in a draw."); }
                    // If no one won the game or the game didn't draw, game moves one.
                    if (!gameResolved)
                    {
                        currentPlayer = currentPlayer == 1 ? 2 : 1;
                        legalMove = false;
                        receiveT = null;
                    }
                }
                if (!isDraw) { Console.WriteLine($"Player {currentPlayer}, won the game!"); }
                Console.WriteLine("Game Over!");
                Console.WriteLine("Restarting game.");
            }
        }

            static void ReceiveData(TcpClient client)
            {
                try
                {
                    NetworkStream networkStream = client.GetStream();
                    byte[] receiveBuffer = new byte[256];
                    while (true)
                    {
                        int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                        string receivedMessage = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                        lastMove = Convert.ToInt32(receivedMessage);
                        Console.WriteLine("Client: " + receivedMessage);
                        moveReceived.Set();

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving data: {ex.Message}");
                }
            }

            static void SendData(TcpClient client, string message)
            {
                try
                {
                    NetworkStream networkStream = client.GetStream();
                    //Pobiera strumień danych z obiektu TcpClient, który będzie używany do wysyłania danych.

                    byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
                    //Konwertuje wiadomość tekstową na tablicę bajtów w kodowaniu ASCII.

                    networkStream.Write(sendBuffer, 0, sendBuffer.Length);
                    //Wysyła dane z bufora przez networkStream do drugiego klienta.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data: {ex.Message}");
                }
            }
        }
    }
