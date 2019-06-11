using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Client
{
    class Program
    {
        static int screenshotsCount = 0;
        static void Main(string[] args)
        {
            while (true)
            {
                TcpClient client = InitializeTcpClient();
                Console.WriteLine("Connected. Processing...");
                ProcessConnection(client);
            }
        }

        private static void ProcessConnection(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            while (true)
            {
                var bytes = stream.Read(new byte[2], 0, 2);
                if (bytes == 1) // server sends 1 byte when waiting for a screenshot
                {
                    SendScreenshot(stream);
                }
                else if (bytes == 2) // server sends 2 bytes before stopping
                {
                    stream.Close();
                    client.Close();
                    Console.WriteLine("Server is off. Disconnected." + Environment.NewLine);
                    break;
                }
            }
        }

        private static void SendScreenshot(NetworkStream stream)
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                byte[] img = null;
                img = new ImageConverter().ConvertTo(bitmap, typeof(byte[])) as byte[];
                stream.Write(img, 0, img.Length);
            }
            Console.WriteLine("Screenshot #{0} is sent to the server.", ++screenshotsCount);
        }

        private static TcpClient InitializeTcpClient()
        {
            TcpClient client;
            while (true)
            {
                IPAddress ip = EnterIP();
                int port = EnterPort();

                try
                {
                    client = new TcpClient(ip.ToString(), port);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + Environment.NewLine);
                }
            }

            return client;
        }

        private static int EnterPort()
        {
            int port;
            while (true)
            {
                Console.WriteLine("Enter port (1000):");
                try
                {
                    port = int.Parse(Console.ReadLine());
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return port;
        }

        private static IPAddress EnterIP()
        {
            IPAddress ip;
            while (true)
            {
                Console.WriteLine("Enter IP (127.0.0.1):");
                try
                {
                    ip = IPAddress.Parse(Console.ReadLine());
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return ip;
        }
    }
}
