using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace tic_tac_toe
{
    public partial class MainPage : ContentPage
    {
        static TcpClient? client;
        static bool canChange = false;
        static List<Button> buttons = new List<Button>();

        public static class ConsoleHelper
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool AllocConsole();
        }

        void OpenConsole(object sender, EventArgs e) 
        {
#if WINDOWS
            ConsoleHelper.AllocConsole();
#endif
            Console.WriteLine("This console is used for debugging purposes.");
        }

        void Connect()
        {
            while (true)
            {
                try
                {
                    client = new TcpClient("127.0.0.1", 3360);
                    Thread receiveThread = new Thread(() => ReceiveData(client));
                    receiveThread.Start();
                    return;
                }
                catch
                {
                    Dispatcher.Dispatch(async () =>
                    {
                        DisplayAlert("Connection Error", "Server not found, trying to connect again in 5 seconds.", "OK");
                    });
                    Task.Delay(5000).Wait();
                }
            }
        }

        void ReceiveData(TcpClient client)
        {
            try
            {
                NetworkStream networkStream = client.GetStream();
                byte[] receiveBuffer = new byte[256];

                while (true)
                {
                    int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                    string receivedMessage = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    Console.WriteLine(receivedMessage);
                    if (receivedMessage == "rdy") { canChange = true; }
                    else if (receivedMessage.Contains('b')) {updateBoard(receivedMessage);}
                    if(receivedMessage == "FIRST") 
                    {
                        Dispatcher.Dispatch(async () =>
                        {
                            DisplayAlert("Alert", "The game begins! You move first!", "OK");
                        });
                    }
                    else if(receivedMessage == "SECOND") 
                    {
                        Dispatcher.Dispatch(async () =>
                        {
                            DisplayAlert("Alert", "The game begins! Wait for your oppenent move!", "OK");
                        });
                    }
                    switch (receivedMessage)
                    {
                        case "WIN":
                            Dispatcher.Dispatch(async () =>
                            {
                                DisplayAlert("Alert", "YOU WON! Restarting game..", "OK");
                            });
                            updateBoard("b0 0 0 0 0 0 0 0 0 ");
                            break;

                        case "LOSE":
                            Dispatcher.Dispatch(async () =>
                            {
                                DisplayAlert("Alert", "YOU LOST! Restarting game..", "OK");
                            });
                            updateBoard("b0 0 0 0 0 0 0 0 0 ");
                            break;

                        case "DRAW":
                            Dispatcher.Dispatch(async () =>
                            {
                                DisplayAlert("Alert", "DRAW! Restarting game..", "OK");
                            });
                            updateBoard("b0 0 0 0 0 0 0 0 0 ");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
                Dispatcher.Dispatch(async () =>
                {
                    DisplayAlert("Connection Error", "Connection to the server was suddenly lost. Retrying to connect in 5 seconds.", "OK");
                });
                Task.Delay(5000).ContinueWith(_ => Connect());
                updateBoard("b0 0 0 0 0 0 0 0 0 ");
            }
        }

        static void SendData(TcpClient client, string message)
        {
            try
            {
                NetworkStream networkStream = client.GetStream();
                byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
                networkStream.Write(sendBuffer, 0, sendBuffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
            }
        }

        public MainPage()
        {
            InitializeComponent();
            for (int i = 0; i < 9; i++)
            {
                Button button = new Button
                {
                    Text = "",
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    FontSize = 64,
                    Margin = new Thickness(20),
                    BackgroundColor = Colors.AntiqueWhite,
                    TextColor = Colors.Black,
                    ClassId = i.ToString()
                };

                button.Clicked += changeSquare;

                int row = i / 3;
                int column = i % 3;

                gameBoard.Children.Add(button);
                gameBoard.SetRow(button, row);
                gameBoard.SetColumn(button, column);
                buttons.Add(button);
            }
            Task.Delay(1000).ContinueWith(_ => Connect());
        }

        void updateBoard(string board)
        {
            Dispatcher.Dispatch(async () =>
            {
            string[] boardFinal = board.Substring(1).Split(' ');
            for (int i = 0; i < 9; i++)
            {
                if (boardFinal[i] == "1")
                    buttons[i].Text = "✕";
                else if (boardFinal[i] == "2")
                    buttons[i].Text = "◯";
                else
                        buttons[i].Text = "";
                }
            });
        }

        void changeSquare(object sender, EventArgs e)
        {
            var button = (Button)sender;
            if(button.Text == "" && canChange)
            {
                SendData(client, button.ClassId);
            }
            canChange = false;
        }
    }



}
