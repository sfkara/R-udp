using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Udp2
{
    public partial class Receiver : Form
    {
        int portNumber = 15000;
        byte[] data = new byte[65507];
        IPEndPoint connectedEndPnt;
        string ip;

        int count = 0;

        string filePath;
        string fileName;
        List<byte[]> splittedBytes = new List<byte[]>();

        public Receiver()
        {
            InitializeComponent();
        }

        private void connectToSender() // before file transmission this receives information about file and number of pieces 
        {
            try
            {
                UdpClient client = new UdpClient();
                byte[] recData = null;
                IPEndPoint recEndPnt = new IPEndPoint(IPAddress.Any, portNumber);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(recEndPnt);

                while (true)
                {
                    recData = client.Receive(ref recEndPnt);
                    string returndata = Encoding.ASCII.GetString(recData);
                    if (returndata == "finish first transaction")
                    {
                        break;
                    }
                    else if (returndata.Contains("parts will be trasferred") == true)
                    {
                        richTextBox1.AppendText(returndata + "\r\n");
                        count = Convert.ToInt32(returndata.Substring(0, returndata.IndexOf("p")));
                    }
                    else
                        richTextBox1.AppendText(returndata + "\r\n");

                }
                ip = Convert.ToString(recEndPnt.Address);
                connectedEndPnt = new IPEndPoint(IPAddress.Parse(ip), portNumber);
                client.Close();
                button2.Enabled = true;
            }
            catch { }
        }

        private void packetReceiver() // receiveing the pieces
        {
            try
            {
                int i = 0;
                UdpClient client = new UdpClient();
                IPEndPoint recEndPnt = new IPEndPoint(IPAddress.Parse(ip), portNumber);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(recEndPnt);


                byte[] header = new byte[10];
                byte[] buffer = new byte[1000];
                byte[] recData = null;
                byte[] ack;
                //client.Client.ReceiveTimeout = 1000;

                while (true)
                {
                    recData = client.Receive(ref connectedEndPnt);
                    System.Buffer.BlockCopy(recData, 0, header, 0, 10); // splits header and byte array 
                    System.Buffer.BlockCopy(recData, 10, buffer, 0, recData.Length - 10);
                    
                    // checking header if header is correct, send ack. if header is wrong send last correct packet ack
                    if (BitConverter.ToInt32(header, 0) == count - 1)
                    {
                        
                        splittedBytes.Add(buffer);
                        ack = header;
                        client.Send(ack, ack.Length, connectedEndPnt);
                        MessageBox.Show("file has been received");
                        break;
                    }
                    else if (BitConverter.ToInt32(header, 0) == i)
                    {
                        
                        splittedBytes.Add(buffer);
                        ack = header;
                        client.Send(ack, ack.Length, connectedEndPnt);
                        Debug.WriteLine(i.ToString());
                        i++;                        
                    }
                    else if (BitConverter.ToInt32(header, 0) != i)
                    {
                        
                        ack = BitConverter.GetBytes(i);
                        client.Send(ack, ack.Length, connectedEndPnt);
                    }


                }

                Thread.Sleep(100);
                client.Close();
                richTextBox1.AppendText("file has been received");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e) // waits for file from sender
        {
            connectToSender();
        }

        private void button2_Click(object sender, EventArgs e) // accepts file
        {
            try
            {

                IPEndPoint recEndPnt = new IPEndPoint(IPAddress.Broadcast, portNumber);
                UdpClient client = new UdpClient();
                byte[] dataEnd = ASCIIEncoding.ASCII.GetBytes("sendMe");
                client.Send(dataEnd, dataEnd.Length, recEndPnt);
                client.Close();
            }
            catch(Exception ex) { }
            packetReceiver();
        }

        private void Receiver_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
