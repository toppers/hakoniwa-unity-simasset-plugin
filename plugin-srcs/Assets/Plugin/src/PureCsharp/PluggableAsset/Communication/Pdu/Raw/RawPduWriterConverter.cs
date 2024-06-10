using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Hakoniwa.Core.Utils.Logger;
using Hakoniwa.PluggableAsset.Assets.Robot.Parts;

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

        public void Add(byte[] bytes, int offset, int count)
        {
            data.AddRange(new ArraySegment<byte>(bytes, offset, count));
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
            ConvertFromStruct(off_info, 0, base_allocator, src);

            // 全体サイズを計算し、バッファを確保
            int totalSize = base_allocator.Size + heap_allocator.Size;
            byte[] buffer = new byte[totalSize];
            meta.total_size = (uint)totalSize;

            // メタデータをバッファに設定
            SetMetaDataToBuffer(buffer, meta);

            // 基本データをバッファにコピー
            byte[] baseData = base_allocator.ToArray();
            Array.Copy(baseData, 0, buffer, (int)meta.base_off, baseData.Length);

            // ヒープデータをバッファにコピー
            byte[] heapData = heap_allocator.ToArray();
            Array.Copy(heapData, 0, buffer, (int)meta.heap_off, heapData.Length);

            // PduCommBinaryDataオブジェクトを作成して返す
            var obj = new PduCommBinaryData(buffer);
            return obj;
        }

        private static void ConvertFromStruct(PduBinOffsetInfo off_info, int base_off, DynamicAllocator allocator, IPduReadOperation src)
        {
            //SimpleLogger.Get().Log(Level.INFO, "TO BIN:Start Convert: package=" + off_info.package_name + " type=" + off_info.type_name);
            foreach (var elm in off_info.elms)
            {
                if (elm.is_primitive)
                {
                    //primitive
                    if (elm.is_array)
                    {
                        ConvertFromPrimtiveArray(elm, base_off, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromPrimtiveArray(elm, offset_from_heap, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes);
                        allocator.Add(offset_from_heap_bytes);
                    }
                    else
                    {
                        ConvertFromPrimtive(elm, base_off, allocator, src);
                    }
                }
                else
                {
                    //struct
                    if (elm.is_array)
                    {
                        ConvertFromStructArray(elm, base_off + elm.offset, allocator, src);
                    }
                    else if (elm.is_varray)
                    {
                        int offset_from_heap = heap_allocator.Size;
                        int array_size = ConvertFromStructArray(elm, offset_from_heap, heap_allocator, src);
                        var offset_from_heap_bytes = BitConverter.GetBytes(offset_from_heap);
                        var array_size_bytes = BitConverter.GetBytes(array_size);
                        allocator.Add(array_size_bytes);
                        allocator.Add(offset_from_heap_bytes);
                    }
                    else
                    {
                        PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
                        ConvertFromStruct(struct_off_info, base_off + elm.offset, allocator, src.Ref(elm.field_name).GetPduReadOps());
                    }
                }
            }
        }

        private static int ConvertFromStructArray(PduBinOffsetElmInfo elm, int base_off, DynamicAllocator allocator, IPduReadOperation src)
        {
            PduBinOffsetInfo struct_off_info = PduOffset.Get(elm.type_name);
            int array_size = src.Refs(elm.field_name).Length;
            for (int i = 0; i < array_size; i++)
            {
                Pdu src_data = src.Refs(elm.field_name)[i];
                ConvertFromStruct(struct_off_info, base_off + (i * elm.elm_size), allocator, src_data.GetPduReadOps());
            }
            return array_size;
        }

        private static int ConvertFromPrimtiveArray(PduBinOffsetElmInfo elm, int base_off, DynamicAllocator allocator, IPduReadOperation src)
        {
            byte[] tmp_bytes = null;
            int array_size = 0;
            switch (elm.type_name)
            {
                case "int8":
                    array_size = src.GetDataInt8Array(elm.field_name).Length;
                    tmp_bytes = new byte[array_size * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataInt8Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "int16":
                    array_size = src.GetDataInt16Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataInt16Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataInt16Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "int32":
                    array_size = src.GetDataInt32Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataInt32Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataInt32Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "int64":
                    array_size = src.GetDataInt64Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataInt64Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataInt64Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "uint8":
                    array_size = src.GetDataUInt8Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataUInt8Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataUInt8Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "uint16":
                    array_size = src.GetDataUInt16Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataUInt16Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataUInt16Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "uint32":
                    array_size = src.GetDataUInt32Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataUInt32Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataUInt32Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "uint64":
                    array_size = src.GetDataUInt64Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataUInt64Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataUInt64Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "float32":
                    array_size = src.GetDataFloat32Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataFloat32Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataFloat32Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "float64":
                    array_size = src.GetDataFloat64Array(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataFloat64Array(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataFloat64Array(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    break;
                case "bool":
                    array_size = src.GetDataBoolArray(elm.field_name).Length;
                    tmp_bytes = new byte[src.GetDataBoolArray(elm.field_name).Length * elm.elm_size];
                    Buffer.BlockCopy(src.GetDataBoolArray(elm.field_name), 0, tmp_bytes, 0, array_size * elm.elm_size);
                    allocator.Add(tmp_bytes);
                    return array_size;
                case "string":
                    array_size = src.GetDataStringArray(elm.field_name).Length;
                    for (int i = 0; i < array_size; i++)
                    {
                        byte[] stringBytes = Encoding.ASCII.GetBytes(src.GetDataStringArray(elm.field_name)[i]);
                        byte[] paddedStringBytes = new byte[elm.elm_size];
                        Buffer.BlockCopy(stringBytes, 0, paddedStringBytes, 0, stringBytes.Length);
                        allocator.Add(paddedStringBytes);
                    }
                    return array_size;
                default:
                    throw new InvalidCastException("Error: Cannot find ptype: " + elm.type_name);
            }
            return 0;
        }


        private static void ConvertFromPrimtive(PduBinOffsetElmInfo elm, int off, DynamicAllocator allocator, IPduReadOperation src)
        {
            byte[] tmp_bytes = null;
            switch (elm.type_name)
            {
                case "int8":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataInt8(elm.field_name));
                    //tmp_bytes = BitConverter.GetBytes(src.GetDataInt8(elm.field_name));
                    sbyte sint8v = src.GetDataInt8(elm.field_name);
                    tmp_bytes = new byte[] { (byte)sint8v };
                    break;
                case "int16":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataInt16(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt16(elm.field_name));
                    break;
                case "int32":
                    //SimpleLogger.Get().Log(Level.INFO, off + ":" + elm.field_name + " = " + src.GetDataInt32(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt32(elm.field_name));
                    break;
                case "int64":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataInt64(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataInt64(elm.field_name));
                    break;
                case "uint8":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataUInt8(elm.field_name));
                    //tmp_bytes = BitConverter.GetBytes(src.GetDataUInt8(elm.field_name));
                    var uint8v = src.GetDataUInt8(elm.field_name);
                    tmp_bytes = new byte[] { uint8v };
                    break;
                case "uint16":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataUInt16(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt16(elm.field_name));
                    break;
                case "uint32":
                    //SimpleLogger.Get().Log(Level.INFO, off + ":" + elm.field_name + " = " + src.GetDataUInt32(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt32(elm.field_name));
                    break;
                case "uint64":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataUInt64(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataUInt64(elm.field_name));
                    break;
                case "float32":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataFloat32(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataFloat32(elm.field_name));
                    break;
                case "float64":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataFloat64(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataFloat64(elm.field_name));
                    break;
                case "bool":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + src.GetDataBool(elm.field_name));
                    tmp_bytes = BitConverter.GetBytes(src.GetDataBool(elm.field_name));
                    break;
                case "string":
                    //SimpleLogger.Get().Log(Level.INFO, elm.field_name + " = " + System.Text.Encoding.ASCII.GetBytes(src.GetDataString(elm.field_name)));
                    tmp_bytes = System.Text.Encoding.ASCII.GetBytes(src.GetDataString(elm.field_name));
                    break;
                default:
                    throw new InvalidCastException("Error: Can not found ptype: " + elm.type_name);
            }
            //SimpleLogger.Get().Log(Level.INFO, elm.type_name + " : " + elm.field_name + " : " + woff);
            //SimpleLogger.Get().Log(Level.INFO, "dst.len=" + dst_buffer.Length);
            //SimpleLogger.Get().Log(Level.INFO, "src.len=" + tmp_bytes.Length);
            allocator.Add(tmp_bytes);
        }

        public IPduCommData ConvertToIoData(IPduWriter src)
        {
            return ConvertToIoData(src.GetReadOps());
        }
    }
}
