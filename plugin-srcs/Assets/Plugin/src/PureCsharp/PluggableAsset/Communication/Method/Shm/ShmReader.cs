using Hakoniwa.Core;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Communication.Method.Shm
{
    class ShmReader: IIoReader, IDisposable
    {
        public string Name { get; internal set; }
        private ShmConfig shm_config;
        private byte[] arg_buffer = null;
        private IntPtr buffer = IntPtr.Zero;
        private string asset_name;
        public string GetName()
        {
            return Name;
        }

        public void Initialize(IIoReaderConfig config)
        {
            this.shm_config = config as ShmConfig;
            this.asset_name = AssetConfigLoader.core_config.cpp_asset_name;
            this.arg_buffer = new byte[shm_config.io_size];
            this.buffer = Marshal.AllocHGlobal(shm_config.io_size);
        }

        public IPduCommData Recv(string io_key)
        {
            bool ret = HakoCppWrapper.asset_read_pdu(this.asset_name, this.shm_config.asset_name, shm_config.channel_id, buffer, (uint)shm_config.io_size);
            if (ret == false)
            {
                throw new ArgumentException("Can not read pdul!! " + this.shm_config.asset_name + " channel_id=" + this.shm_config.channel_id);
            }
            Marshal.Copy(buffer, arg_buffer, 0, arg_buffer.Length);
            return new PduCommBinaryData(arg_buffer);
        }

        public void Dispose()
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~ShmReader()
        {
            Dispose();
        }
    }
}
