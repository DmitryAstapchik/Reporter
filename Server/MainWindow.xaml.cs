using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener server;
        TcpClient client;
        NetworkStream stream;
        DispatcherTimer timer;
        int screenshotsCount;
        MailMessage message;
        SmtpClient smtp;
        public MainWindow()
        {
            InitializeComponent();
            Grid.Background = System.Windows.Media.Brushes.Green;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                timer = new DispatcherTimer(TimeSpan.FromSeconds(int.Parse(TextBoxInterval.Text)), DispatcherPriority.Normal, new EventHandler(Timer_Tick), Dispatcher)
                {
                    IsEnabled = false
                };

                smtp = new SmtpClient(TextBoxSmtp.Text, int.Parse(TextBoxSmtpPort.Text))
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(TextBoxEmail.Text, PasswordBox.Password)
                };

                message = new MailMessage(new MailAddress(TextBoxEmail.Text), new MailAddress(TextBoxEmail.Text));

                server = new TcpListener(IPAddress.Parse(TextBoxIp.Text), int.Parse(TextBoxIpPort.Text));
                server.Start();
                server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), null);

                LabelCount.Content = screenshotsCount = 0;
                SetStartedVisualState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Start error");
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult result)
        {
            try
            {
                client = server.EndAcceptTcpClient(result);
                stream = client.GetStream();

                timer.Start();
            }
            catch (ObjectDisposedException)
            {
                // occurs after server is stopped
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                stream.Write(new byte[1], 0, 1); // forces the client to send a screenshot

                var img = new byte[10 * 1024 * 1024];
                var size = stream.Read(img, 0, img.Length);
                Array.Resize(ref img, size);
                var bitmap = new ImageConverter().ConvertFrom(img) as Bitmap;

                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                    memory.Position = 0;

                    message.Attachments.Add(new Attachment(memory, string.Format("Screenshot #{0}.jpg", ++screenshotsCount)));
                    smtp.Send(message);
                    LabelCount.Content = screenshotsCount;
                    message.Attachments.Clear();
                }
            }
            catch (Exception ex)
            {
                StopServer();
                SetStoppedVisualState();
                MessageBox.Show(ex.Message, "Screenshot sending error");
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
            SetStoppedVisualState();
        }

        private void SetStartedVisualState()
        {
            ButtonStart.IsEnabled = false;
            ButtonStop.IsEnabled = true;
            foreach (var item in Grid.Children.OfType<TextBox>())
            {
                item.IsEnabled = false;
            }
            PasswordBox.IsEnabled = false;
            Grid.Background = System.Windows.Media.Brushes.LightGreen;
        }

        private void SetStoppedVisualState()
        {
            ButtonStop.IsEnabled = false;
            ButtonStart.IsEnabled = true;
            foreach (var item in Grid.Children.OfType<TextBox>())
            {
                item.IsEnabled = true;
            }
            PasswordBox.IsEnabled = true;
            Grid.Background = System.Windows.Media.Brushes.Green;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            if (stream != null)
            {
                try
                {
                    stream.Write(new byte[2], 0, 2); // informs the client that the server is off
                }
                catch (IOException)
                {
                    // means that the client is off
                }
                timer.Stop();
                stream.Close();
                stream = null;
                client.Close();
            }
            server.Stop();
        }
    }
}