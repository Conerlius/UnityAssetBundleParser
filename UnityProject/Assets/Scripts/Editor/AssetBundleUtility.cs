using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using K4os.Compression.LZ4;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor
{
    public class AssetBundleUtility
    {
        /// <summary>
        /// 获取简单的ab信息
        /// </summary>
        /// <param name="assetBundlePath">ab文件路径</param>
        /// <returns>信息</returns>
        public static SimpleInfo GetSimpleInfo(string assetBundlePath)
        {
            SimpleInfo simpleInfo = new SimpleInfo();
            
            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            simpleInfo.AssetBundleName = assetBundle.name;
            simpleInfo.MemoryBudgetKB = AssetBundle.memoryBudgetKB;
            simpleInfo.InstanceId = assetBundle.GetInstanceID();
            // 所有的assets
            var allAssetNames = assetBundle.GetAllAssetNames();
            foreach (var assetName in allAssetNames)
            {
                // simpleInfo.Assets.Add(new SimpleInfo.SimpleAsset(assetBundle.LoadAsset<Object>(assetName)));
                simpleInfo.Assets.Add(new SimpleInfo.SimpleAsset(assetName));
            }
            // 所有的Scene
            var allScenePaths = assetBundle.GetAllScenePaths();
            assetBundle.Unload(true);
            assetBundle = null;
            return simpleInfo;
        }

        /// <summary>
        /// 获取完整的ab信息
        /// </summary>
        /// <param name="assetBundlePath">ab文件路径</param>
        /// <returns>信息</returns>
        public static FullInfo GetFullInfo(string assetBundlePath)
        {
            FullInfo fullInfo = new FullInfo();
            // 这里就不走stream了，只是为了说明格式而已！
            var allBytes = File.ReadAllBytes(assetBundlePath);
            var assetBundleFactory = new AssetBundleHeader();
            assetBundleFactory.ReadStructure(allBytes);
            return fullInfo;
        }

        #region 解释相关

        public static string ReadStringToNull(byte[] content, ref int position, int max = 32767)
        {
            var bytes = new List<byte>();
            while (content.Length > position && bytes.Count < max)
            {
                if (content[position] == 0)
                {
                    position++;
                    break;
                }
                bytes.Add(content[position]);
                position++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static bool ReadBoolean(byte[] content, ref int position)
        {
            byte value = content[position++];
            return (int)value != 0;
        }

        public static UInt16 ReadUInt16(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public static Int16 ReadInt16(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public static UInt32 ReadUInt32(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            buffer[2] = content[position++];
            buffer[3] = content[position++];
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }
        public static Int32 ReadInt32(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            buffer[2] = content[position++];
            buffer[3] = content[position++];
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }
        public static Int64 ReadInt64(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            buffer[2] = content[position++];
            buffer[3] = content[position++];
            buffer[4] = content[position++];
            buffer[5] = content[position++];
            buffer[6] = content[position++];
            buffer[7] = content[position++];
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        }
        public static ulong ReadUInt64(byte[] content, ref int position)
        {
            byte[] buffer = new byte[8];
            buffer[0] = content[position++];
            buffer[1] = content[position++];
            buffer[2] = content[position++];
            buffer[3] = content[position++];
            buffer[4] = content[position++];
            buffer[5] = content[position++];
            buffer[6] = content[position++];
            buffer[7] = content[position++];
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        public static byte[] ReadBytes(byte[] content, int length, ref int position)
        {
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = content[position++];
            }

            return buffer;
        }
        public static byte ReadByte(byte[] content, ref int position)
        {
            byte buffer = content[position++];
            return buffer;
        }

        public delegate T ReadArrayFunction<out T>(byte[] content, ref int position);
        public static int[] ReadInt32Array(byte[] content, ref int position)
        {
            return ReadArray(ReadInt32, ReadInt32(content, ref position), content, ref position);
        }

        public static T[] ReadArray<T>(ReadArrayFunction<T> del, int length, byte[] content, ref int position)
        {
            var array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = del(content, ref position);
            }
            return array;
        }

        #endregion

        #region 压缩相关

        public static byte[] DecodeCompress(uint flag, byte[] compressBuffer, int unCompressLength)
        {
            List<byte> UncompressBlockInfoBuffer = new List<byte>();
            switch (flag & 0x3F)
            {
                default: //None
                {
                    // 没有压缩就直接复制
                    UncompressBlockInfoBuffer.AddRange(compressBuffer);
                    break;
                }
                case 1: //LZMA（7Z）
                {
                    byte[] uncompress = new byte[unCompressLength];
                    int compressLength = compressBuffer.Length;
                    Compress.DefaultLzmaUncompress(compressBuffer, ref compressLength, uncompress,
                        ref unCompressLength);
                    UncompressBlockInfoBuffer.AddRange(uncompress);
                    break;
                }
                case 2: //LZ4
                case 3: //LZ4HC
                {
                    var uncompressedBytes = new byte[unCompressLength];
                    var numWrite = LZ4Codec.Decode(compressBuffer, uncompressedBytes);
                    if (numWrite != unCompressLength)
                    {
                        // 异常
                        Debug.LogError("解释异常！");
                    }
                    UncompressBlockInfoBuffer.AddRange(uncompressedBytes);
                    break;
                }
            }

            return UncompressBlockInfoBuffer.ToArray();
        }

        #endregion
    }
    /// <summary>
    /// 简单信息
    /// </summary>
    public class SimpleInfo
    {
        public class SimpleAsset
        {
            public string Name { get; private set; }

            public SimpleAsset(string name)
            {
                Name = name;
            }
        }
        /// <summary>
        /// 内存大小
        /// </summary>
        public uint MemoryBudgetKB;

        /// <summary>
        /// ab名称
        /// </summary>
        public string AssetBundleName { get; set; }
        /// <summary>
        /// instance id
        /// </summary>
        public int InstanceId { get; set; }
        /// <summary>
        /// 所有的Asset
        /// </summary>
        public List<SimpleAsset> Assets = new List<SimpleAsset>();
        /// <summary>
        /// 所有的Scene
        /// </summary>
        // public List<SimpleScene> Assets = new List<SimpleScene>();
    }

    public class FullInfo
    {
        
    }
}