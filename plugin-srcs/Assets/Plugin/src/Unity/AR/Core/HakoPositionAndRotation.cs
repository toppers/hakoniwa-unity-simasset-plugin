using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hakoniwa.AR.Core
{
    public class HakoPositionAndRotation
    {
        public string name;
        public int state;
        public Vector3 position;
        public Vector3 rotation;

        public byte[] Encode(float scale)
        {
            byte[] dataBytes;
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.name.Length);
            writer.Write(this.name);
            writer.Write(this.state);
            writer.Write(this.position.x / scale);
            writer.Write(this.position.y / scale);
            writer.Write(this.position.z / scale);
            writer.Write(this.rotation.x);
            writer.Write(this.rotation.y);
            writer.Write(this.rotation.z);
            dataBytes = stream.ToArray();
            return dataBytes;
        }
        public int Decode(byte[] dataBytes, int off, float scale)
        {
            MemoryStream stream = new MemoryStream(dataBytes, off, dataBytes.Length - off);
            BinaryReader reader = new BinaryReader(stream);
            int nameLength = reader.ReadInt32();
            this.name = reader.ReadString();
            this.state = reader.ReadInt32();
            this.position.x = reader.ReadSingle() * scale;
            this.position.y = reader.ReadSingle() * scale;
            this.position.z = reader.ReadSingle() * scale;
            this.rotation.x = reader.ReadSingle();
            this.rotation.y = reader.ReadSingle();
            this.rotation.z = reader.ReadSingle();
            //Debug.Log("Stream.pos=" + stream.Position);
            return (int)stream.Position;
        }
    }

}
