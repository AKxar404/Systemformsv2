using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Systemformsv2
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private List<TcpClient> clients = new List<TcpClient>();
        private List<string> clientNames = new List<string>();
        private Thread serverThread;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serverThread = new Thread(StartServer);
            serverThread.Start();
            button1.Enabled = false;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 13000);
            server.Start();
            Invoke((Action)(() => listBox1.Items.Add("Servidor Iniciado")));
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                clients.Add(client);
                Invoke((Action)(() => listBox1.Items.Add(client.Client.RemoteEndPoint.ToString())));
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            byte[] bytes = new byte[256];
            string data = null;
            string clientName = null;

            using (NetworkStream stream = client.GetStream())
            {
                // Pide el nombre del cliente
                data = "Ingrese su nombre: ";
                byte[] msg = Encoding.ASCII.GetBytes(data);
                stream.Write(msg, 0, msg.Length);

                // Lee el nombre del cliente
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    clientName = data;
                    break;
                }

                lock (clientNames)
                {
                    clientNames.Add(clientName);
                }

                Console.WriteLine("Cliente conectado: {0}", clientName);

                // Loop para recibir todos los datos enviados por el cliente
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Traduce los datos recibidos a una cadena de texto
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("{0}: {1}", clientName, data);

                    // Reenvía el mensaje a todos los demás clientes
                    foreach (TcpClient c in clients)
                    {
                        if (c != client)
                        {
                            using (NetworkStream s = c.GetStream())
                            {
                                byte[] msg2 = Encoding.ASCII.GetBytes(clientName + ": " + data);
                                s.Write(msg2, 0, msg2.Length);
                            }
                        }
                    }
                }
            }

            // Cierra la conexión con el cliente
            lock (clients)
            {
                clients.Remove(client);
            }
            lock (clientNames)
            {
                clientNames.Remove(clientName);
            }
            client.Close();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (server != null)
            {
                server.Stop();
            }
        }
    }
}
