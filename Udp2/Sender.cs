using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Udp2
{
    public partial class Sender : Form
    {
        public Sender()
        {
            InitializeComponent();
        }

        int portNumber = 15000;
        IPEndPoint connectedEndPnt;
        string ip;

        string filePath;
        string fileName;
        byte[] fileBytes;
        List<byte[]> splittedBytes;

        private void setupConnection() // before file transmission this sends information about file and number of pieces to receiver
        {
            try
            {
                UdpClient client = new UdpClient();

                byte[] dataFile = ASCIIEncoding.ASCII.GetBytes(fileName);
                byte[] dataPieces = ASCIIEncoding.ASCII.GetBytes(splittedBytes.Count.ToString() + " parts will be trasferred");
                byte[] dataEnd = ASCIIEncoding.ASCII.GetBytes("finish first transaction");
                byte[] recData = null;
                IPEndPoint recEndPnt = new IPEndPoint(IPAddress.Any, portNumber);
                connectedEndPnt = new IPEndPoint(IPAddress.Broadcast, portNumber);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(recEndPnt);


                client.Send(dataFile, dataFile.Length, connectedEndPnt);
                client.Send(dataPieces, dataPieces.Length, connectedEndPnt);
                client.Send(dataEnd, dataEnd.Length, connectedEndPnt);
                while (true)
                {
                    recEndPnt = new IPEndPoint(IPAddress.Any, portNumber);
                    recData = client.Receive(ref recEndPnt); // if receiver accept the file , transmission starts
                    string returndata = Encoding.ASCII.GetString(recData);
                    if (returndata == "sendMe")
                    {
                        client.Close();
                        ip = Convert.ToString(recEndPnt.Address);
                        connectedEndPnt = new IPEndPoint(IPAddress.Parse(ip), portNumber);

                        break;
                    }

                }

            }
            catch (Exception ex) { Console.WriteLine(ex); }
            packetSender();

        }
        private void packetSender() // sending file pieces
        {
            try
            {
                int i = 0;
                UdpClient client = new UdpClient();
                IPEndPoint recEndPnt = new IPEndPoint(IPAddress.Parse(ip), portNumber);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(recEndPnt);

                byte[] header;
                byte[] receiveData = null;

                //client.Client.ReceiveTimeout = 100;
                while (i < splittedBytes.Count)
                {
                    header = BitConverter.GetBytes(i);
                    byte[] buffer = splittedBytes[i];
                    byte[] packet = new byte[10 + buffer.Length];
                    System.Buffer.BlockCopy(header, 0, packet, 0, header.Length);
                    System.Buffer.BlockCopy(buffer, 0, packet, 10, buffer.Length); // 

                    client.Send(packet, packet.Length, connectedEndPnt); // sending datagram with header
                    receiveData = client.Receive(ref recEndPnt); // receiveing ack 

                    // check header 
                    if (BitConverter.ToInt32(receiveData, 0) != i)
                        i = Convert.ToInt32(receiveData);
                    else if (BitConverter.ToInt32(receiveData, 0) == i)
                        i++;
                    if (BitConverter.ToInt32(receiveData, 0) == splittedBytes.Count)
                        break;

                }
                client.Close();
                MessageBox.Show("file has been sent");
            }
            catch( Exception ex) { Console.Write(ex); }
        }


        private void button1_Click(object sender, EventArgs e) // file choosing here
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {

                filePath = openFileDialog1.FileName;
                fileName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                label1.Text = fileName;
                fileToByte(filePath); // calling method to convert file to byte array
            }

        }



        private void fileToByte(string fileSource)
        {
            FileStream stream = File.OpenRead(fileSource);
            fileBytes = new byte[stream.Length]; // converting byte array

            stream.Read(fileBytes, 0, fileBytes.Length);
            stream.Close();
            splittedBytes = new List<byte[]>();

            for (int i = 0; i < fileBytes.Length; i += 1000)     // this splits byte array to 1000 bytes pieces
            {
                byte[] buffer = new byte[1000];                 // each splittedBytes element has 1000 bytes or less
                if (fileBytes.Length - (splittedBytes.Count * 1000) < 1000)
                {
                    buffer = new byte[fileBytes.Length - splittedBytes.Count * 1000];
                    Buffer.BlockCopy(fileBytes, i, buffer, 0, fileBytes.Length - splittedBytes.Count * 1000);
                }
                else
                    Buffer.BlockCopy(fileBytes, i, buffer, 0, 1000);
                splittedBytes.Add(buffer);
            }
            button2.Enabled = true;
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            setupConnection();
        }

        private void Sender_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
