using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Hakoniwa.AR.Core
{
    public class HakoUdpClient
    {
        private UdpClient udpClient;
        private string serverIp;
        private int serverPort;

        public HakoUdpClient(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            udpClient = new UdpClient();
        }

        public void SendData(byte[] data)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
                udpClient.Send(data, data.Length, endPoint);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error sending data: " + ex.Message);
            }
        }

        public void Close()
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }
    }
}

