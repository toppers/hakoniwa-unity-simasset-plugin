using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Hakoniwa.AR.Core
{
    public class HakoUdpServer
    {
        private static Thread thread;
        private static bool isUdpActive = false;

        public HakoUdpServer(string ipaddr, int port, int timeout_sec)
        {
            this.ipaddr = ipaddr;
            this.port = port;
            this.timeout = timeout_sec;
        }

        private async Task ReceiveWithTimeout(UdpClient udpClient, TimeSpan timeout)
        {
            Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
            Task timeoutTask = Task.Delay(timeout);
            Task completedTask = await Task.WhenAny(receiveTask, timeoutTask);

            if (completedTask == receiveTask)
            {
                UdpReceiveResult result = receiveTask.Result;
                lock (this.lockObj)
                {
                    this.buffer = result.Buffer;
                }
            }
            else
            {
                //nothing to do
            }
        }


        private async Task ThreadMethodAsync()
        {
            Debug.Log("UdpServer Start");
            UdpClient udpClient = null;
            try
            {
                TimeSpan timeout = TimeSpan.FromSeconds(this.timeout);
                IPAddress localAddress = IPAddress.Parse(this.ipaddr);
                IPEndPoint localEP = new IPEndPoint(localAddress, this.port);
                udpClient = new System.Net.Sockets.UdpClient(localEP);
                Debug.Log("UDP server ipaddr=" + this.ipaddr + " port=" + this.port);

                while (isUdpActive)
                {
                    try
                    {
                        await ReceiveWithTimeout(udpClient, timeout);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("ERROR: " + ex.Message);
                    }
                }
                Debug.Log("UdpServer Finished");
            }
            catch (Exception ex)
            {
                Debug.LogError("ERROR: " + ex.Message);
            }
            finally
            {
                if (udpClient != null)
                {
                    udpClient.Close();
                }
            }
        }

        public void StopServer()
        {
            HakoUdpServer.isUdpActive = false;
        }

        private string ipaddr = "192.168.11.37";
        private int port = 50001;
        private int timeout = 1;
        protected byte[] buffer = null;
        protected System.Object lockObj = new System.Object();

        public async Task StartServer()
        {
            if (isUdpActive)
            {
                return;
            }
            isUdpActive = true;
            await Task.Run(() => ThreadMethodAsync());
        }
        public byte[] RecvData()
        {
            lock (this.lockObj)
            {
                var data = this.buffer;
                this.buffer = null;
                return data;
            }
        }

    }

}
