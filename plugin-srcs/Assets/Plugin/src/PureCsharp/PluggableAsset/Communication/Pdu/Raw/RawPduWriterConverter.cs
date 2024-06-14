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
            // メタデータのスペースを確保 (一時的なダミーデータ)
            base_allocator.Add(new byte[ConstantValues.PduMetaDataSize]);

            // データを動的アロケータに追加
            ConvertFromStruct(off_info, base_allocator, src);

            // 全体サイズを計算し、バッファを確保
            int totalSize = base_allocator.Size + heap_allocator.Size;
            byte[] buffer = new byte[totalSize];
            meta.total_size = (uint)totalSize;
            //SimpleLogger.Get().Log(Level.INFO, "name: " + type_name);
            //SimpleLogger.Get().Log(Level.INFO, "total_size: " + totalSize + " base_allocator size: " + base_allocator.Size + " heap_allocator size: " + heap_allocator.Size);

            // 基本データをバッファにコピー
            byte[] baseData = base_allocator.ToArray();
            //SimpleLogger.Get().Log(Level.INFO, "base writer: off: " + meta.base_off + "src.len:" + baseData.Length + "dst.len: " + buffer.Length);
            Array.Copy(baseData, 0, buffer, 0, baseData.Length);

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
            return obj;
        }

        private static void ConvertFromStruct(PduBinOffsetInfo off_info, DynamicAllocator allocator, IPduReadOperation src)
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
                        ConvertFromPrimtiveArray(elm, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromPrimtiveArray(elm, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes, elm.offset, array_size_bytes.Length);
                        allocator.Add(offset_from_heap_bytes, elm.offset + array_size_bytes.Length, offset_from_heap_bytes.Length);
                    }
                    else
                    {
                        ConvertFromPrimtive(elm, allocator, src);
                    }
                }
                else
                {
                    //struct
                    if (elm.is_array)
                    {
                        ConvertFromStructArray(elm, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromStructArray(elm, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes, elm.offset, array_size_bytes.Length);
                        allocator.Add(offset_from_heap_bytes, elm.offset + array_size_bytes.Length, offset_from_heap_bytes.Length);
                    }
                    else
                    {
                        PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
                        ConvertFromStruct(struct_off_info, allocator, src.Ref(elm.field_name).GetPduReadOps());
                    }
                }
            }
        }

        private static int ConvertFromStructArray(PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
        {
            PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
            int array_size = src.Refs(elm.field_name).Length;
            for (int i = 0; i < array_size; i++)
            {
                Pdu src_data = src.Refs(elm.field_name)[i];
                ConvertFromStruct(struct_off_info, allocator, src_data.GetPduReadOps());
            }
            return array_size;
        }

        private static int ConvertFromPrimtiveArray(PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
        {
            byte[] tmp_bytes = null;
            int array_size = 0;
            int element_size = elm.elm_size;
            switch (elm.type_name)
            {
                case "int8":
                    array_size = src.GetDataInt8Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = new byte[] { (byte)src.GetDataInt8Array(elm.field_name)[i] };
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "int16":
                    array_size = src.GetDataInt16Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataInt16Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "int32":
                    array_size = src.GetDataInt32Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataInt32Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "int64":
                    array_size = src.GetDataInt64Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataInt64Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "uint8":
                    array_size = src.GetDataUInt8Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = new byte[] { src.GetDataUInt8Array(elm.field_name)[i] };
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "uint16":
                    array_size = src.GetDataUInt16Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataUInt16Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "uint32":
                    array_size = src.GetDataUInt32Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataUInt32Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "uint64":
                    array_size = src.GetDataUInt64Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataUInt64Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "float32":
                    array_size = src.GetDataFloat32Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataFloat32Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "float64":
                    array_size = src.GetDataFloat64Array(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataFloat64Array(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "bool":
                    array_size = src.GetDataBoolArray(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        tmp_bytes = BitConverter.GetBytes(src.GetDataBoolArray(elm.field_name)[i]);
                        allocator.Add(tmp_bytes, elm.offset + i * element_size, tmp_bytes.Length);
                    }
                    return array_size;
                case "string":
                    array_size = src.GetDataStringArray(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        byte[] stringBytes = Encoding.ASCII.GetBytes(src.GetDataStringArray(elm.field_name)[i]);
                        byte[] paddedStringBytes = new byte[elm.elm_size];
                        Buffer.BlockCopy(stringBytes, 0, paddedStringBytes, 0, stringBytes.Length);
                        allocator.Add(paddedStringBytes, elm.offset + i * element_size, paddedStringBytes.Length);
                    }
                    return array_size;
                default:
                    throw new InvalidCastException("Error: Cannot find ptype: " + elm.type_name);
            }
        }


        private static void ConvertFromPrimtive(PduBinOffsetElmInfo elm, DynamicAllocator allocator, IPduReadOperation src)
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
                    tmp_bytes = BitConverter.GetBytes(src.GetDataFloat64(elm.field_name));
                    break;
                case "bool":
                    tmp_bytes = BitConverter.GetBytes(src.GetDataBool(elm.field_name));
                    break;
                case "string":
                    tmp_bytes = new byte[elm.elm_size];
                    var str_bytes = System.Text.Encoding.ASCII.GetBytes(src.GetDataString(elm.field_name));
                    Array.Copy(str_bytes, tmp_bytes, str_bytes.Length);
                    break;
                default:
                    throw new InvalidCastException("Error: Can not found ptype: " + elm.type_name);
            }
            allocator.Add(tmp_bytes, elm.offset, tmp_bytes.Length);
        }

        public IPduCommData ConvertToIoData(IPduWriter src)
        {
            return ConvertToIoData(src.GetReadOps());
        }
    }
}
