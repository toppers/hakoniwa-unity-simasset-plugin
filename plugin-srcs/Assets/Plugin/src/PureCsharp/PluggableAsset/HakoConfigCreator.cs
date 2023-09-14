using System;
using System.Collections.Generic;
using System.IO;
using Hakoniwa.PluggableAsset;
using Newtonsoft.Json;
using UnityEngine;

namespace Hakoniwa.PluggableAsset
{
    [System.Serializable]
    public class HakoRoboPduConfig
    {
        public string type;
        public string org_name;
        public string name;
        public string class_name;
        public string conv_class_name;
        public int channel_id;
        public int pdu_size;
        public int write_cycle;
        public string method_type;
    }
    [System.Serializable]
    public class HakoRobotConfig
    {
        public string name;
        public HakoRoboPduConfig[] rpc_pdu_readers;
        public HakoRoboPduConfig[] rpc_pdu_writers;
        public HakoRoboPduConfig[] shm_pdu_readers;
        public HakoRoboPduConfig[] shm_pdu_writers;
    }
    [System.Serializable]
    public class HakoRobotConfigContainer
    {
        public HakoRobotConfig[] robots;
    }
    public class HakoConfigCreator
    {
        public static HakoRobotConfigContainer GetHakoRobotConfig(string json)
        {
            return JsonConvert.DeserializeObject<HakoRobotConfigContainer>(json);
        }
        public static void CreateCoreConfig(CoreConfig config)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(config, settings);
            System.IO.File.WriteAllText("./Assets/Resources/core_config.json", json);
        }
        private static void AddRosMessageType(Dictionary<string, string> map, string typename, string parent_path)
        {
            if (map.ContainsKey(typename))
            {
                return;
            }
            map.Add(typename, parent_path + typename + ".json");
            var len = typename.Split('/').Length;
            var last_elment = typename.Split('/')[len -1];
            map.Add(last_elment, parent_path + typename + ".json");
        }
        public static void CreatePduConfig(RosTopicMessageConfigContainer ros_configs, CoreConfig core_config, string parent_path)
        {
            core_config.pdu_configs_parent_path = parent_path;
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach(var entry in ros_configs.fields)
            {
                AddRosMessageType(map, entry.topic_type_name, parent_path);
            }
            var container = new PduDataConfig[map.Count + 2];
            int i = 0;
            foreach (var entry in map)
            {
                container[i] = new PduDataConfig();
                container[i].pdu_type_name = entry.Key;
                container[i].pdu_data_field_path = entry.Value;
                i++;
            }
            container[i] = new PduDataConfig();
            container[i].pdu_type_name = "time";
            container[i].pdu_data_field_path = parent_path + "builtin_interfaces/Time.json";
            i++;
            container[i] = new PduDataConfig();
            container[i].pdu_type_name = "HakoniwaSimTime";
            container[i].pdu_data_field_path = "./HakoniwaSimTime.json";
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/pdu_configs.json", json);
            core_config.pdu_configs_path = "./pdu_configs.json";

        }
        private static string GetPduReaderName(HakoRoboPduConfig cfg)
        {
            return cfg.name + "Pdu";
        }
        private static string GetPduWriterName(HakoRoboPduConfig cfg)
        {
            return cfg.name + "Pdu";
        }
        private static HakoRoboPduConfig[] GetPduReaders(HakoRobotConfig robo)
        {
            if (robo.rpc_pdu_readers == null)
            {
                robo.rpc_pdu_readers = new HakoRoboPduConfig[0];
            }
            if (robo.shm_pdu_readers == null)
            {
                robo.shm_pdu_readers = new HakoRoboPduConfig[0];
            }
            var reader_inputs = new HakoRoboPduConfig[robo.rpc_pdu_readers.Length + robo.shm_pdu_readers.Length];
            Array.Copy(robo.rpc_pdu_readers, reader_inputs, robo.rpc_pdu_readers.Length);
            Array.Copy(robo.shm_pdu_readers, 0, reader_inputs, robo.rpc_pdu_readers.Length, robo.shm_pdu_readers.Length);
            return reader_inputs;
        }
        private static HakoRoboPduConfig[] GetPduWriters(HakoRobotConfig robo)
        {
            if (robo.rpc_pdu_writers == null)
            {
                robo.rpc_pdu_writers = new HakoRoboPduConfig[0];
            }
            if (robo.shm_pdu_writers == null)
            {
                robo.shm_pdu_writers = new HakoRoboPduConfig[0];
            }
            var writer_inputs = new HakoRoboPduConfig[robo.rpc_pdu_writers.Length + robo.shm_pdu_writers.Length];
            Array.Copy(robo.rpc_pdu_writers, writer_inputs, robo.rpc_pdu_writers.Length);
            Array.Copy(robo.shm_pdu_writers, 0, writer_inputs, robo.rpc_pdu_writers.Length, robo.shm_pdu_writers.Length);
            return writer_inputs;
        }
        public static void CreateInsideAsset(HakoRobotConfigContainer robo_config, CoreConfig core_config)
        {
            InsideAssetConfig[] container = new InsideAssetConfig[robo_config.robots.Length];
            for (int robo_inx = 0; robo_inx < robo_config.robots.Length; robo_inx++)
            {
                var robo = robo_config.robots[robo_inx];
                container[robo_inx] = new InsideAssetConfig();
                container[robo_inx].name = robo.name;

                var reader_inputs = GetPduReaders(robo);
                container[robo_inx].pdu_reader_names = new string[reader_inputs.Length];
                for (var i = 0; i < reader_inputs.Length; i++)
                {
                    container[robo_inx].pdu_reader_names[i] = GetPduReaderName(reader_inputs[i]);
                }
                var writer_inputs = GetPduWriters(robo);
                container[robo_inx].pdu_writer_names = new string[writer_inputs.Length];
                for (var i = 0; i < writer_inputs.Length; i++)
                {
                    container[robo_inx].pdu_writer_names[i] = GetPduWriterName(writer_inputs[i]);
                }
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/inside_assets.json", json);
            core_config.inside_assets_path = "./inside_assets.json";

        }

        public static void CreatePduReaderWriter(HakoRobotConfigContainer robo_config, CoreConfig core_config)
        {
            PduReaderConfig[] reader_container = new PduReaderConfig[0];
            PduWriterConfig[] writer_container = new PduWriterConfig[0];
            int i = 0;
            int j = 0;
            foreach (var robo in robo_config.robots)
            {
                var reader_inputs = GetPduReaders(robo);
                var writer_inputs = GetPduWriters(robo);
                foreach (var entry in reader_inputs)
                {
                    Array.Resize<PduReaderConfig>(ref reader_container, reader_container.Length + 1);
                    var tmp = new PduReaderConfig();
                    tmp.class_name = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduReader";
                    tmp.conv_class_name = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduReaderConverter";
                    tmp.name = GetPduReaderName(entry);
                    tmp.pdu_config_name = entry.type;
                    reader_container[i] = tmp;
                    i++;
                }
                foreach (var entry in writer_inputs)
                {
                    Array.Resize<PduWriterConfig>(ref writer_container, writer_container.Length + 1);
                    var tmp = new PduWriterConfig();
                    tmp.class_name = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduWriter";
                    tmp.conv_class_name = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduWriterConverter";
                    tmp.name = GetPduWriterName(entry);
                    tmp.pdu_config_name = entry.type;
                    writer_container[j] = tmp;
                    j++;
                }
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string reader_json = JsonConvert.SerializeObject(reader_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/pdu_readers.json", reader_json);
            core_config.pdu_readers_path = "./pdu_readers.json";
            string writer_json = JsonConvert.SerializeObject(writer_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/pdu_writers.json", writer_json);
            core_config.pdu_writers_path = "./pdu_writers.json";
        }


        public static string GetRpcMethodName(HakoRobotConfig robo, HakoRoboPduConfig entry, bool isRead)
        {
            if (isRead)
            {
                return robo.name + "RpcReader" + entry.channel_id;
            }
            else
            {
                return robo.name + "RpcWriter" + entry.channel_id;
            }
        }
        public static string GetShmMethodName(HakoRobotConfig robo, HakoRoboPduConfig entry, bool isRead)
        {
            if (isRead)
            {
                return robo.name + "ShmReader" + entry.channel_id;
            }
            else
            {
                return robo.name + "ShmWriter" + entry.channel_id;
            }
        }

        public static void CreateRpcMethod(HakoRobotConfigContainer robo_config, CoreConfig core_config)
        {
           RpcMethodConfig[] method_container = new RpcMethodConfig[0];
            foreach (var robo in robo_config.robots)
            {
                foreach (var entry in robo.rpc_pdu_readers)
                {
                    int i = method_container.Length;
                    Array.Resize<RpcMethodConfig>(ref method_container, method_container.Length + 1);
                    var tmp = new RpcMethodConfig();
                    tmp.asset_name = robo.name;
                    tmp.method_name = GetRpcMethodName(robo, entry, true);
                    tmp.method_type = entry.method_type;
                    tmp.channel_id = entry.channel_id;
                    tmp.is_read = true;
                    tmp.pdu_size = entry.pdu_size;
                    method_container[i] = tmp;
                }
                foreach (var entry in robo.rpc_pdu_writers)
                {
                    int i = method_container.Length;
                    Array.Resize<RpcMethodConfig>(ref method_container, method_container.Length + 1);
                    var tmp = new RpcMethodConfig();
                    tmp.asset_name = robo.name;
                    tmp.method_name = GetRpcMethodName(robo, entry, false);
                    tmp.method_type = entry.method_type;
                    tmp.channel_id = entry.channel_id;
                    tmp.is_read = false;
                    tmp.pdu_size = entry.pdu_size;
                    tmp.write_cycle = entry.write_cycle;
                    method_container[i] = tmp;
                }
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(method_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/rpc_methods.json", json);
            core_config.rpc_methods_path = "./rpc_methods.json";

        }
        public static void CreateShmMethod(HakoRobotConfigContainer robo_config, CoreConfig core_config)
        {
            ShmMethodConfig[] method_container = new ShmMethodConfig[0];
            foreach (var robo in robo_config.robots)
            {
                foreach (var entry in robo.shm_pdu_readers)
                {
                    int i = method_container.Length;
                    Array.Resize<ShmMethodConfig>(ref method_container, method_container.Length + 1);
                    var tmp = new ShmMethodConfig();
                    tmp.asset_name = robo.name;
                    tmp.method_name = GetShmMethodName(robo, entry, true);
                    tmp.channel_id = entry.channel_id;
                    tmp.is_read = true;
                    tmp.iosize = entry.pdu_size;
                    method_container[i] = tmp;
                }
                foreach (var entry in robo.shm_pdu_writers)
                {
                    int i = method_container.Length;
                    Array.Resize<ShmMethodConfig>(ref method_container, method_container.Length + 1);
                    var tmp = new ShmMethodConfig();
                    tmp.asset_name = robo.name;
                    tmp.method_name = GetShmMethodName(robo, entry, false);
                    tmp.channel_id = entry.channel_id;
                    tmp.is_read = false;
                    tmp.iosize = entry.pdu_size;
                    method_container[i] = tmp;
                }
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string json = JsonConvert.SerializeObject(method_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/shm_methods.json", json);
            core_config.shm_methods_path = "./shm_methods.json";

        }

        private static void AddPduChannelCoonector(ref PduChannelConnectorConfig[] container, string name, bool is_reader)
        {
            Array.Resize<PduChannelConnectorConfig>(ref container, container.Length + 1);
            var tmp = new PduChannelConnectorConfig();
            tmp.outside_asset_name = "None";
            if (is_reader)
            {
                tmp.reader_connector_name = name;
            }
            else
            {
                tmp.writer_connector_name = name;
            }
            container[container.Length - 1] = tmp;
        }

        public static void CreateConnector(HakoRobotConfigContainer robo_config, CoreConfig core_config, bool isDebug)
        {
            PduChannelConnectorConfig[] pdu_channe_container = new PduChannelConnectorConfig[0];
            ReaderConnectorConfig[] reader_container = new ReaderConnectorConfig[0];
            WriterConnectorConfig[] writer_container = new WriterConnectorConfig[0];
            int i = 0;
            int j = 0;
            foreach (var robo in robo_config.robots)
            {
                var reader_inputs = GetPduReaders(robo);
                var writer_inputs = GetPduWriters(robo);
                foreach (var entry in reader_inputs)
                {
                    Array.Resize<ReaderConnectorConfig>(ref reader_container, reader_container.Length + 1);
                    var tmp = new ReaderConnectorConfig();
                    tmp.name = "custom_reader_connector_" + i;
                    tmp.pdu_name = GetPduReaderName(entry);
                    if (isDebug == false)
                    {
                        //reader_inputsには、rpcとshm両方あるので、どちらのエントリかをチェックする必要がある
                        bool is_exist = Array.Exists<HakoRoboPduConfig>(robo.rpc_pdu_readers, e => e == entry);
                        if (is_exist)
                        {
                            tmp.method_name = GetRpcMethodName(robo, entry, true);
                        }
                        else
                        {
                            tmp.method_name = GetShmMethodName(robo, entry, true);
                        }
                    }
                    reader_container[i] = tmp;
                    //PDU CHANNEL CONNECTOR
                    AddPduChannelCoonector(ref pdu_channe_container, tmp.name, true);
                    i++;
                }
                foreach (var entry in writer_inputs)
                {
                    Array.Resize<WriterConnectorConfig>(ref writer_container, writer_container.Length + 1);
                    var tmp = new WriterConnectorConfig();
                    tmp.name = "custom_writer_connector_" + j;
                    tmp.pdu_name = GetPduWriterName(entry);
                    if (isDebug == false)
                    {
                        //writer_inputsには、rpcとshm両方あるので、どちらのエントリかをチェックする必要がある
                        bool is_exist = Array.Exists<HakoRoboPduConfig>(robo.rpc_pdu_writers, e => e == entry);
                        if (is_exist)
                        {
                            tmp.method_name = GetRpcMethodName(robo, entry, false);
                        }
                        else
                        {
                            tmp.method_name = GetShmMethodName(robo, entry, false);
                        }
                    }
                    writer_container[j] = tmp;
                    //PDU CHANNEL CONNECTOR
                    AddPduChannelCoonector(ref pdu_channe_container, tmp.name, false);
                    j++;
                }
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            string reader_json = JsonConvert.SerializeObject(reader_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/reader_connector.json", reader_json);
            core_config.reader_connectors_path = "./reader_connector.json";
            string writer_json = JsonConvert.SerializeObject(writer_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/writer_connector.json", writer_json);
            core_config.writer_connectors_path = "./writer_connector.json";
            string connector_json = JsonConvert.SerializeObject(pdu_channe_container, settings);
            System.IO.File.WriteAllText("./Assets/Resources/pdu_channel_connector.json", connector_json);
            core_config.pdu_channel_connectors_path = "./pdu_channel_connector.json";
        }
    }

}

