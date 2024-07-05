using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hakoniwa.AR.Core
{
    public class HakoObjectSynchronizerForApl : MonoBehaviour
    {
        public GameObject[] players;
        public GameObject[] avators;
        private string server_ipaddr;
        private int server_portno;
        public int timeout_sec;
        public float scale = 1;

        private string client_ipaddr;
        private int client_portno;

        private HakoUdpServer server;
        private HakoUdpClient client;


        private Dictionary<string, HakoAvatorObject> avatorMap = new Dictionary<string, HakoAvatorObject>();
        private Dictionary<string, HakoPlayerObject> playerMap = new Dictionary<string, HakoPlayerObject>();

        async void Start()
        {
            string ipport = PlayerPrefs.GetString("server_savedIP", "127.0.0.1:54002");
            server_ipaddr = ipport.Split(":")[0].Trim();
            server_portno = int.Parse(ipport.Split(":")[1].Trim());
            Debug.Log("server ip : " + server_ipaddr + " port:" + server_portno);

            ipport = PlayerPrefs.GetString("client_savedIP", "127.0.0.1:54002");
            client_ipaddr = ipport.Split(":")[0].Trim();
            client_portno = int.Parse(ipport.Split(":")[1].Trim());
            Debug.Log("client ip : " + client_ipaddr + " port:" + client_portno);

            this.server = new HakoUdpServer(server_ipaddr, server_portno, timeout_sec);
            this.client = new HakoUdpClient(client_ipaddr, client_portno);

            if (this.avators.Length > 0)
            {
                foreach (var obj in this.avators)
                {
                    var avator = obj.GetComponentInChildren<HakoAvatorObject>();
                    if (avator == null)
                    {
                        throw new System.Exception("Not Found avator");
                    }
                    this.avatorMap.Add(obj.name, avator);
                }
            }
            if (this.players.Length > 0)
            {
                foreach (var obj in this.players)
                {
                    var player = obj.GetComponentInChildren<HakoPlayerObject>();
                    if (player == null)
                    {
                        throw new System.Exception("Not Found player");
                    }
                    this.playerMap.Add(obj.name, player);
                }
            }
            await this.server.StartServer();

        }
        void OnApplicationQuit()
        {
            this.server.StopServer();
        }
        void FixedUpdate()
        {
            var data = this.server.RecvData();
            if (data != null)
            {
                //Debug.Log("recv data len=" + data.Length);
                for (int off = 0; off < data.Length;)
                {
                    HakoPositionAndRotation pr = new HakoPositionAndRotation();
                    int size = pr.Decode(data, off, this.scale);
                    //Debug.Log("off=" + off + " size=" + size);
                    off += size;
                    //Debug.Log("avator name=" + pr.name);
                    var obj = this.avatorMap[pr.name];
                    obj.SetPosAndRot(pr);
                }
            }
            List<byte[]> dataBytesList = new List<byte[]>();
            foreach (var name in this.playerMap.Keys)
            {
                var pr = this.playerMap[name].GetPosAndRot();
                {
                    var pr_data = pr.Encode(this.scale);
                    dataBytesList.Add(pr_data);
                }
                this.playerMap[name].SetPrevValue(pr);
            }
            byte[] combinedData = dataBytesList.SelectMany(bytes => bytes).ToArray();
            this.client.SendData(combinedData);
            //Debug.Log(" data send len=" + combinedData.Length);
        }
    }
}

