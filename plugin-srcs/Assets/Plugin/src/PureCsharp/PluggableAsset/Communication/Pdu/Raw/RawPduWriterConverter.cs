using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hakoniwa.Core.Utils.Logger;
using Hakoniwa.PluggableAsset.Assets.Robot.Parts;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Communication.Pdu.Raw
{
    public class DynamicAllocator
    {
        private List<byte> data;

        public DynamicAllocator()
        {
            data = new List<byte>();
        }

        public void Add(byte[] bytes)
        {
            data.AddRange(bytes);
        }

        public void Add(byte[] bytes, int expectedOffset, int count)
        {
            int currentSize = data.Count;
            if (currentSize < expectedOffset)
            {
                data.AddRange(new byte[expectedOffset - currentSize]);
            }
            data.AddRange(new ArraySegment<byte>(bytes, 0, count));
        }

        public byte[] ToArray()
        {
            return data.ToArray();
        }

        public int Size => data.Count;
    }



    class RawPduWriterConverter : IPduWriterConverter
    {
        private static void SetMetaDataToBuffer(byte[] buffer, HakoPduMetaDataType meta)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(meta.magicno), 0, buffer, 0, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(meta.version), 0, buffer, 4, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(meta.base_off), 0, buffer, 8, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(meta.heap_off), 0, buffer, 12, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(meta.total_size), 0, buffer, 16, sizeof(uint));
        }
        private static DynamicAllocator heap_allocator = null;

        public static IPduCommData ConvertToIoData(IPduReadOperation src)
        {
            string type_name = src.Ref(null).GetName();
            var off_info = PduOffset.Get(type_name);
            if (off_info == null)
            {
                throw new InvalidOperationException("Error: Can not found offset: type=" + type_name);
            }
            DynamicAllocator base_allocator = new DynamicAllocator();
            heap_allocator = new DynamicAllocator();
            // メタデータを設定
            HakoPduMetaDataType meta = new HakoPduMetaDataType
            {
                magicno = ConstantValues.PduMetaDataMagicNo,
                version = ConstantValues.PduMetaDataVersion,
                base_off = (uint)ConstantValues.PduMetaDataSize,
                heap_off = (uint)ConstantValues.PduMetaDataSize + (uint)off_info.size,
                total_size = (uint)0
            };

            // データを動的アロケータに追加
            ConvertFromStruct(0, off_info, base_allocator, src);

            // 全体サイズを計算し、バッファを確保
            int totalSize = off_info.size + heap_allocator.Size + ConstantValues.PduMetaDataSize;
            byte[] buffer = new byte[totalSize];
            meta.total_size = (uint)totalSize;
            //SimpleLogger.Get().Log(Level.INFO, "name: " + type_name);
            //SimpleLogger.Get().Log(Level.INFO, "total_size: " + totalSize + " base_allocator size: " + base_allocator.Size + " heap_allocator size: " + heap_allocator.Size);

            // 基本データをバッファにコピー
            byte[] baseData = base_allocator.ToArray();
            //SimpleLogger.Get().Log(Level.INFO, "base writer: off: " + meta.base_off + "src.len:" + baseData.Length + "dst.len: " + buffer.Length);
            Array.Copy(baseData, 0, buffer, ConstantValues.PduMetaDataSize, baseData.Length);

            if (heap_allocator.Size > 0)
            {
                // ヒープデータをバッファにコピー
                byte[] heapData = heap_allocator.ToArray();
                //SimpleLogger.Get().Log(Level.INFO, "heap writer: heap_off=" + meta.heap_off + " heapData.len:" + heapData.Length + " dst.len: " + buffer.Length);
                Array.Copy(heapData, 0, buffer, (int)meta.heap_off, heapData.Length);
            }
            // メタデータをバッファに設定
            SetMetaDataToBuffer(buffer, meta);

            // PduCommBinaryDataオブジェクトを作成して返す
            var obj = new PduCommBinaryData(buffer);
#if false
            if (type_name == "hako_msgs/HakoStatusMagnetHolder")
            {
                // メモリダンプを追加
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Memory Dump (hex):");
                for (int i = 0; i < buffer.Length; i += 16)
                {
                    sb.AppendFormat("{0:X4}  ", i); // Offset
                    for (int j = 0; j < 16; j++)
                    {
                        if (i + j < buffer.Length)
                            sb.AppendFormat("{0:X2} ", buffer[i + j]);
                        else
                            sb.Append("   ");
                    }
                    sb.Append(" ");
                    for (int j = 0; j < 16; j++)
                    {
                        if (i + j < buffer.Length)
                        {
                            char c = (char)buffer[i + j];
                            sb.Append(Char.IsControl(c) ? '.' : c);
                        }
                    }
                    sb.AppendLine();
                }
                Debug.Log(sb.ToString());
            }
#endif
            return obj;
        }

        private static void ConvertFromStruct(int parent_off, PduBinOffsetInfo off_info, DynamicAllocator allocator, IPduReadOperation src)
        {
            //SimpleLogger.Get().Log(Level.INFO, "TO BIN:Start Convert: package=" + off_info.package_name + " type=" + off_info.type_name);
            foreach (var elm in off_info.elms)
            {
                //SimpleLogger.Get().Log(Level.INFO, "elm: " + elm.field_name + " type: " + elm.type_name + " is_array:" + elm.is_array + " is_varray: " + elm.is_varray);
                if (elm.is_primitive)
                {
                    //primitive
                    if (elm.is_array)
                    {
                        ConvertFromPrimtiveArray(parent_off, elm, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromPrimtiveArray(0, elm, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes, parent_off + elm.offset, array_size_bytes.Length);
                        allocator.Add(offset_from_heap_bytes, parent_off + elm.offset + array_size_bytes.Length, offset_from_heap_bytes.Length);
                    }
                    else
                    {
                        ConvertFromPrimtive(parent_off, elm, allocator, src);
                    }
                }
                else
                {
                    //struct
                    if (elm.is_array)
                    {
                        ConvertFromStructArray(parent_off + elm.offset, elm, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromStructArray(0, elm, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes, parent_off + elm.offset, array_size_bytes.Length);
                        allocator.Add(offset_from_heap_bytes, parent_off + elm.offset + array_size_bytes.Length, offset_from_heap_bytes.Length);
                    }
                    else
                    {
                        //SimpleLogger.Get().Log(Level.INFO, "name: " + elm.field_name + " parent off: " + elm.offset);
                        PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
                        ConvertFromStruct(parent_off + elm.offset, struct_off_info, allocator, src.Ref(elm.field_name).GetPduReadOps());
                    }
                }
            }
        }

        private static int ConvertFromStructArray(int parent_off, PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
        {
            PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
            int array_size = src.Refs(elm.field_name).Length;
            for (int i = 0; i < array_size; i++)
            {
                Pdu src_data = src.Refs(elm.field_name)[i];
                ConvertFromStruct(parent_off + (i * elm.elm_size), struct_off_info, allocator, src_data.GetPduReadOps());
            }
            return array_size;
        }

        private static int ConvertFromPrimtiveArray(int parent_off, PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
        {
            int array_size = 0;
            int element_size = elm.elm_size;
            byte[] tmp_bytes = null;

            switch (elm.type_name)
            {
                case "int8":
                    sbyte[] int8Array = src.GetDataInt8Array(elm.field_name);
                    array_size = int8Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(int8Array, 0, tmp_bytes, 0, array_size);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, array_size * element_size);
                    return array_size;
                case "int16":
                    short[] int16Array = src.GetDataInt16Array(elm.field_name);
                    array_size = int16Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(int16Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "int32":
                    int[] int32Array = src.GetDataInt32Array(elm.field_name);
                    array_size = int32Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(int32Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "int64":
                    long[] int64Array = src.GetDataInt64Array(elm.field_name);
                    array_size = int64Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(int64Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "uint8":
                    byte[] uint8Array = src.GetDataUInt8Array(elm.field_name);
                    array_size = uint8Array.Length;
                    allocator.Add(uint8Array, parent_off + elm.offset, array_size * element_size);
                    return array_size;
                case "uint16":
                    ushort[] uint16Array = src.GetDataUInt16Array(elm.field_name);
                    array_size = uint16Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(uint16Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "uint32":
                    uint[] uint32Array = src.GetDataUInt32Array(elm.field_name);
                    array_size = uint32Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(uint32Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "uint64":
                    ulong[] uint64Array = src.GetDataUInt64Array(elm.field_name);
                    array_size = uint64Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(uint64Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "float32":
                    float[] float32Array = src.GetDataFloat32Array(elm.field_name);
                    array_size = float32Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(float32Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "float64":
                    double[] float64Array = src.GetDataFloat64Array(elm.field_name);
                    array_size = float64Array.Length;
                    tmp_bytes = new byte[array_size * element_size];
                    Buffer.BlockCopy(float64Array, 0, tmp_bytes, 0, tmp_bytes.Length);
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "bool":
                    bool[] boolArray = src.GetDataBoolArray(elm.field_name);
                    array_size = boolArray.Length;
                    tmp_bytes = new byte[array_size * 4]; // 4バイト長のbool型データ用
                    for (int i = 0; i < array_size; i++)
                    {
                        byte[] boolBytes = new byte[4];
                        boolBytes[0] = boolArray[i] ? (byte)1 : (byte)0;
                        Buffer.BlockCopy(boolBytes, 0, tmp_bytes, i * 4, 4);
                    }
                    allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
                    return array_size;
                case "string":
                    string[] stringArray = src.GetDataStringArray(elm.field_name);
                    array_size = stringArray.Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        byte[] stringBytes = Encoding.ASCII.GetBytes(stringArray[i]);
                        byte[] paddedStringBytes = new byte[elm.elm_size];
                        Buffer.BlockCopy(stringBytes, 0, paddedStringBytes, 0, stringBytes.Length);
                        allocator.Add(paddedStringBytes, parent_off + elm.offset + i * element_size, paddedStringBytes.Length);
                    }
                    return array_size;
                default:
                    throw new InvalidCastException("Error: Cannot find ptype: " + elm.type_name);
            }
        }


        private static void ConvertFromPrimtive(int parent_off, PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
        {
            byte[] tmp_bytes = null;
            switch (elm.type_name)
            {
                case "int8":
                    sbyte sint8v = src.GetDataInt8(elm.field_name);
                    tmp_bytes = new byte[] { (byte)sint8v };
                    break;
                case "int16":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt16(elm.field_name));
                    break;
                case "int32":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt32(elm.field_name));
                    break;
                case "int64":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt64(elm.field_name));
                    break;
                case "uint8":
                    var uint8v = src.GetDataUInt8(elm.field_name);
                    tmp_bytes = new byte[] { uint8v };
                    break;
                case "uint16":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt16(elm.field_name));
                    break;
                case "uint32":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt32(elm.field_name));
                    break;
                case "uint64":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt64(elm.field_name));
                    break;
                case "float32":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataFloat32(elm.field_name));
                    break;
                case "float64":
                    //SimpleLogger.Get().Log(Level.INFO, "name: " + elm.field_name + " = " + src.GetDataFloat64(elm.field_name) + "off: " + elm.offset);
                    tmp_bytes = BitConverter.GetBytes(src.GetDataFloat64(elm.field_name));
                    break;
                case "bool":
                    //SimpleLogger.Get().Log(Level.INFO, "elm: " + elm.field_name + " value: " + src.GetDataBool(elm.field_name) + " off: " + elm.offset);
                    // bool型を4バイトにパディングする
                    tmp_bytes = new byte[4];
                    tmp_bytes[0] = src.GetDataBool(elm.field_name) ? (byte)1 : (byte)0;
                    break;
                case "string":
                    tmp_bytes = new byte[elm.elm_size];
                    var str_bytes = System.Text.Encoding.ASCII.GetBytes(src.GetDataString(elm.field_name));
                    Array.Copy(str_bytes, tmp_bytes, str_bytes.Length);
                    break;
                default:
                    throw new InvalidCastException("Error: Can not found ptype: " + elm.type_name);
            }
            allocator.Add(tmp_bytes, parent_off + elm.offset, tmp_bytes.Length);
        }

        public IPduCommData ConvertToIoData(IPduWriter src)
        {
            return ConvertToIoData(src.GetReadOps());
        }
    }
}
