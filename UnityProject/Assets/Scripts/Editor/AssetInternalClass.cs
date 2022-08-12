using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using K4os.Compression.LZ4;
using UnityEngine;

namespace Editor
{
    public class AssetBundleHeader
    {
        public string Signature;
        public UInt32 Version;
        public string UnityVersion;
        public string unityRevision;
        public Int64 Size;
        public UInt32 CompressedBlocksInfoSize;
        public UInt32 UncompressedBlocksInfoSize;
        public UInt32 Flag;
        List<byte> CompressBlockInfoBuffer = new List<byte>();
        public AssetBundleDirectoryInfo[] allAssetBundleDirectoryInfo;

        public BlockInfo[] allBlockInfo;

        // 解释整体结构
        public void ReadStructure(byte[] bytes)
        {
            int BytesPosition = 0;
            Signature = AssetBundleUtility.ReadStringToNull(bytes, ref BytesPosition);
            Version = AssetBundleUtility.ReadUInt32(bytes, ref BytesPosition);
            UnityVersion = AssetBundleUtility.ReadStringToNull(bytes, ref BytesPosition);
            unityRevision = AssetBundleUtility.ReadStringToNull(bytes, ref BytesPosition);
            Size = AssetBundleUtility.ReadInt64(bytes, ref BytesPosition);
            CompressedBlocksInfoSize = AssetBundleUtility.ReadUInt32(bytes, ref BytesPosition);
            UncompressedBlocksInfoSize = AssetBundleUtility.ReadUInt32(bytes, ref BytesPosition);
            Flag = AssetBundleUtility.ReadUInt32(bytes, ref BytesPosition);
            // 对齐
            while (BytesPosition % 16 != 0)
            {
                BytesPosition++;
            }

            // CompressBlockInfoBuffer
            CompressBlockInfoBuffer.Clear();
            for (int index = 0; index < CompressedBlocksInfoSize; index++, BytesPosition++)
            {
                CompressBlockInfoBuffer.Add(bytes[BytesPosition]);
            }

            // 解释CompressBlockInfoBuffer
            AssetBundleInternalClassUtility.DecodeCompressBlocksInfo(CompressBlockInfoBuffer.ToArray(), this,
                out allAssetBundleDirectoryInfo, out allBlockInfo);

            // List<Class CompressBlockContent>
            List<byte> allBlockBuffer = new List<byte>();
            foreach (var blockInfo in allBlockInfo)
            {
                var compressBuffer1 =
                    AssetBundleUtility.ReadBytes(bytes, (int)blockInfo.CompressedSize, ref BytesPosition);
                var unCompressBuffer1 = AssetBundleUtility.DecodeCompress(blockInfo.Flags, compressBuffer1,
                    (int)blockInfo.UncompressedSize);
                allBlockBuffer.AddRange(unCompressBuffer1);
            }

            // 解释List<Class CompressBlockContent>
            AssetBundleInternalClassUtility.DecodeDirectoryBuffer(allBlockBuffer, ref allAssetBundleDirectoryInfo);

            #region 内容

            // for test 
            // foreach (var directoryInfo in allAssetBundleDirectoryInfo)
            // {
            //     Debug.Log($"========================= {directoryInfo.Path}");
            //     StringBuilder sb = new StringBuilder();
            //     int index = 0;
            //     foreach (var b in directoryInfo.Buffer)
            //     {
            //         index++;
            //         sb.Append($"{((int)b):X}");
            //         if (index % 16 == 0)
            //         {
            //             sb.Append("\n");
            //         }
            //         else
            //         {
            //             sb.Append(" ");
            //         }
            //     }
            //     Debug.Log(sb.ToString());
            //     sb.Clear();
            //     Debug.Log("=========================");
            // }

            #endregion

            // ReadAssetsBuffer();
        }

        public void ReadAssetsBuffer()
        {
            foreach (var directoryInfo in allAssetBundleDirectoryInfo)
            {
                // 想办法从文件头里判定一下类型

                #region Asset类型解释 begin

                int _position = 0;
                {
                    var m_MetadataSize = AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                    long m_FileSize = AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                    var m_Version = AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                    long m_DataOffset = AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                    var m_Endianess = AssetBundleUtility.ReadByte(directoryInfo.buffer, ref _position);
                    var m_Reserved = AssetBundleUtility.ReadBytes(directoryInfo.buffer, 3, ref _position);
                    if (m_Version >= 22)
                    {
                        m_MetadataSize = AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                        m_FileSize = AssetBundleUtility.ReadInt64(directoryInfo.buffer, ref _position);
                        m_DataOffset = AssetBundleUtility.ReadInt64(directoryInfo.buffer, ref _position);
                        AssetBundleUtility.ReadInt64(directoryInfo.buffer, ref _position); // unknown
                    }
                }
                var unityVersion = AssetBundleUtility.ReadStringToNull(directoryInfo.buffer, ref _position);
                _position = 0;

                #endregion

                try
                {
                    AssetBundleUtility.ReadUInt32(directoryInfo.buffer, ref _position);
                }
                catch (Exception e)
                {
                }
            }
        }
    }

    public class BlockInfo
    {
        public uint UncompressedSize;
        public uint CompressedSize;
        public ushort Flags;
    }

    public class AssetBundleDirectoryInfo
    {
        public long Offset;
        public long Size;
        public uint Flags;
        public string Path;
        public byte[] buffer;

        public uint MetadataSize;
        public long FileSize;
        public uint Version;
        public long DataOffset;

        /// <summary>
        /// 大小端
        /// </summary>
        public byte Endianess;

        public string unityVersion;
        public bool EnableTypeTree;
        private List<TypeRoot> TypeList = new List<TypeRoot>();

        private int _position;

        public void ReadFiles()
        {
            _position = 0;
            MetadataSize = AssetBundleUtility.ReadUInt32(buffer, ref _position);
            FileSize = AssetBundleUtility.ReadUInt32(buffer, ref _position);
            Version = AssetBundleUtility.ReadUInt32(buffer, ref _position);
            DataOffset = AssetBundleUtility.ReadUInt32(buffer, ref _position);
            Endianess = AssetBundleUtility.ReadByte(buffer, ref _position);
            var m_Reserved = AssetBundleUtility.ReadBytes(buffer, 3, ref _position);
            if (Version >= 22)
            {
                MetadataSize = AssetBundleUtility.ReadUInt32(buffer, ref _position);
                FileSize = AssetBundleUtility.ReadInt64(buffer, ref _position);
                DataOffset = AssetBundleUtility.ReadInt64(buffer, ref _position);
                var aaa = AssetBundleUtility.ReadInt64(buffer, ref _position); // 未知
            }

            // 各个版本的特有的一些属性
            unityVersion = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
            var m_TargetPlatform = AssetBundleUtility.ReadInt32(buffer, ref _position);
            EnableTypeTree = AssetBundleUtility.ReadBoolean(buffer, ref _position);

            int typeCount = AssetBundleUtility.ReadInt32(buffer, ref _position);
            TypeList = new List<TypeRoot>();
            for (int i = 0; i < typeCount; i++)
            {
                TypeList.Add(new TypeRoot(buffer, EnableTypeTree, false, ref _position));
            }

            // read object
            int objectCount = AssetBundleUtility.ReadInt32(buffer, ref _position);
            for (int i = 0; i < objectCount; i++)
            {
                var m_PathID = AssetBundleUtility.ReadInt64(buffer, ref _position);
                var byteStart = AssetBundleUtility.ReadInt64(buffer, ref _position);
                byteStart += DataOffset;
                var byteSize = AssetBundleUtility.ReadUInt32(buffer, ref _position);
                var typeID = AssetBundleUtility.ReadInt32(buffer, ref _position);
                // 板顶树级关系
            }

            int scriptCount = AssetBundleUtility.ReadInt32(buffer, ref _position);
            for (int i = 0; i < scriptCount; i++)
            {
                var localSerializedFileIndex = AssetBundleUtility.ReadInt32(buffer, ref _position);
                // 4位对齐
                var localIdentifierInFile = AssetBundleUtility.ReadInt64(buffer, ref _position);
            }

            int externalsCount = AssetBundleUtility.ReadInt32(buffer, ref _position);
            for (int i = 0; i < externalsCount; i++)
            {
                byte[] guid = AssetBundleUtility.ReadBytes(buffer, 16, ref _position);
                var type = AssetBundleUtility.ReadInt32(buffer, ref _position);
                var pathName = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
            }

            int refTypesCount = AssetBundleUtility.ReadInt32(buffer, ref _position);
            for (int i = 0; i < refTypesCount; i++)
            {
                new TypeRoot(buffer, EnableTypeTree, true, ref _position);
            }

            var userInformation = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
        }
    }

    /// <summary>
    /// 资源文件
    /// </summary>
    public class AssetFile
    {
        public uint MetadataSize;
        public long FileSize;
        public uint Version;
        public long DataOffset;

        /// <summary>
        /// 大小端
        /// </summary>
        public byte Endianess;

        public string unityVersion;

        public AssetFile(byte[] buffer, ref int _position)
        {
        }
    }

    public class AssetBundleInternalClassUtility
    {
        public static void DecodeCompressBlocksInfo(byte[] CompressBlockInfoBuffer, AssetBundleHeader header,
            out AssetBundleDirectoryInfo[] allAssetBundleDirectoryInfo, out BlockInfo[] allBlockInfo)
        {
            int unCompressLength = (int)header.UncompressedBlocksInfoSize;
            // 解压 已经压缩的
            var unCompressBuffer =
                AssetBundleUtility.DecodeCompress(header.Flag, CompressBlockInfoBuffer, unCompressLength);
            int unCompressPosition = 0;
            // hash
            var DataHash = AssetBundleUtility.ReadBytes(unCompressBuffer, 16, ref unCompressPosition);
            var blocksInfoCount = AssetBundleUtility.ReadInt32(unCompressBuffer, ref unCompressPosition);
            allBlockInfo = new BlockInfo[blocksInfoCount];
            for (int i = 0; i < blocksInfoCount; i++)
            {
                allBlockInfo[i] = new BlockInfo
                {
                    UncompressedSize = AssetBundleUtility.ReadUInt32(unCompressBuffer, ref unCompressPosition),
                    CompressedSize = AssetBundleUtility.ReadUInt32(unCompressBuffer, ref unCompressPosition),
                    Flags = AssetBundleUtility.ReadUInt16(unCompressBuffer, ref unCompressPosition)
                };
            }

            var directoryCount = AssetBundleUtility.ReadInt32(unCompressBuffer, ref unCompressPosition);
            allAssetBundleDirectoryInfo = new AssetBundleDirectoryInfo[directoryCount];
            for (int i = 0; i < directoryCount; i++)
            {
                allAssetBundleDirectoryInfo[i] = new AssetBundleDirectoryInfo
                {
                    Offset = AssetBundleUtility.ReadInt64(unCompressBuffer, ref unCompressPosition),
                    Size = AssetBundleUtility.ReadInt64(unCompressBuffer, ref unCompressPosition),
                    Flags = AssetBundleUtility.ReadUInt32(unCompressBuffer, ref unCompressPosition),
                    Path = AssetBundleUtility.ReadStringToNull(unCompressBuffer, ref unCompressPosition),
                };
            }
        }

        public static void DecodeDirectoryBuffer(List<byte> buffer,
            ref AssetBundleDirectoryInfo[] allAssetBundleDirectoryInfo)
        {
            int DirectoryPosition = 0;
            foreach (var directoryInfo in allAssetBundleDirectoryInfo)
            {
                List<byte> _temp = new List<byte>();
                for (int k = 0; k < directoryInfo.Size; k++)
                {
                    _temp.Add(buffer[(int)directoryInfo.Offset + k]);
                }

                directoryInfo.buffer = _temp.ToArray();

                // 解释！
                directoryInfo.ReadFiles();
            }
        }
    }

    public class TypeRoot
    {
        public TypeRoot(byte[] buffer, bool EnableTypeTree, bool refType, ref int _position)
        {
            var classID = AssetBundleUtility.ReadInt32(buffer, ref _position);
            var m_IsStrippedType = AssetBundleUtility.ReadBoolean(buffer, ref _position);
            var m_ScriptTypeIndex = AssetBundleUtility.ReadInt16(buffer, ref _position);
            if (classID == 114)
            {
                var m_ScriptID = AssetBundleUtility.ReadBytes(buffer, 16, ref _position);
            }

            var m_OldTypeHash = AssetBundleUtility.ReadBytes(buffer, 16, ref _position);
            if (EnableTypeTree)
            {
                var typeTree = new TypeTree(buffer, ref _position);
                if (refType)
                {
                    var klassName = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
                    var nameSpace = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
                    var asmName = AssetBundleUtility.ReadStringToNull(buffer, ref _position);
                }
                else
                {
                    var m_TypeDependencies = AssetBundleUtility.ReadInt32Array(buffer, ref _position);
                }
            }
        }
    }

    public class TypeTree
    {
        private readonly List<TypeNode> m_Nodes;

        public TypeTree(byte[] buffer, ref int _position)
        {
            m_Nodes = new List<TypeNode>();
            int numberOfNodes = AssetBundleUtility.ReadInt32(buffer, ref _position);
            int stringBufferSize = AssetBundleUtility.ReadInt32(buffer, ref _position);
            for (int i = 0; i < numberOfNodes; i++)
            {
                var typeTreeNode = new TypeNode();
                m_Nodes.Add(typeTreeNode);
                var m_Version = AssetBundleUtility.ReadUInt16(buffer, ref _position);
                typeTreeNode.m_Level = AssetBundleUtility.ReadByte(buffer, ref _position);
                typeTreeNode.m_TypeFlags = AssetBundleUtility.ReadByte(buffer, ref _position);
                typeTreeNode.m_TypeStrOffset = AssetBundleUtility.ReadUInt32(buffer, ref _position);
                typeTreeNode.m_NameStrOffset = AssetBundleUtility.ReadUInt32(buffer, ref _position);
                typeTreeNode.m_ByteSize = AssetBundleUtility.ReadInt32(buffer, ref _position);
                typeTreeNode.m_Index = AssetBundleUtility.ReadInt32(buffer, ref _position);
                typeTreeNode.m_MetaFlag = AssetBundleUtility.ReadInt32(buffer, ref _position);
                // > 2019
                typeTreeNode.m_RefTypeHash = AssetBundleUtility.ReadUInt64(buffer, ref _position);
            }

            byte[] stringbuffer = AssetBundleUtility.ReadBytes(buffer, stringBufferSize, ref _position);
            // stringbuffer struct
            {
                for (int i = 0; i < numberOfNodes; i++)
                {
                    m_Nodes[i].m_Type = ReadString(stringbuffer, m_Nodes[i].m_TypeStrOffset);
                    m_Nodes[i].m_Name = ReadString(stringbuffer, m_Nodes[i].m_NameStrOffset);
                }
            }

            string ReadString(byte[] stringBufferReader, uint value)
            {
                var isOffset = (value & 0x80000000) == 0;
                if (isOffset)
                {
                    int Position = (int)value;
                    return AssetBundleUtility.ReadStringToNull(stringBufferReader, ref Position);
                }

                var offset = value & 0x7FFFFFFF;
                if (StringBuffer.TryGetValue(offset, out var str))
                {
                    return str;
                }

                return offset.ToString();
            }
        }

        public class TypeNode
        {
            public byte m_Level;
            public byte m_TypeFlags;
            public uint m_TypeStrOffset;
            public uint m_NameStrOffset;
            public int m_ByteSize;
            public int m_Index;
            public int m_MetaFlag;
            public ulong m_RefTypeHash;
            public string m_Type;
            public string m_Name;
        }

        public static readonly Dictionary<uint, string> StringBuffer = new Dictionary<uint, string>
        {
            { 0, "AABB" },
            { 5, "AnimationClip" },
            { 19, "AnimationCurve" },
            { 34, "AnimationState" },
            { 49, "Array" },
            { 55, "Base" },
            { 60, "BitField" },
            { 69, "bitset" },
            { 76, "bool" },
            { 81, "char" },
            { 86, "ColorRGBA" },
            { 96, "Component" },
            { 106, "data" },
            { 111, "deque" },
            { 117, "double" },
            { 124, "dynamic_array" },
            { 138, "FastPropertyName" },
            { 155, "first" },
            { 161, "float" },
            { 167, "Font" },
            { 172, "GameObject" },
            { 183, "Generic Mono" },
            { 196, "GradientNEW" },
            { 208, "GUID" },
            { 213, "GUIStyle" },
            { 222, "int" },
            { 226, "list" },
            { 231, "long long" },
            { 241, "map" },
            { 245, "Matrix4x4f" },
            { 256, "MdFour" },
            { 263, "MonoBehaviour" },
            { 277, "MonoScript" },
            { 288, "m_ByteSize" },
            { 299, "m_Curve" },
            { 307, "m_EditorClassIdentifier" },
            { 331, "m_EditorHideFlags" },
            { 349, "m_Enabled" },
            { 359, "m_ExtensionPtr" },
            { 374, "m_GameObject" },
            { 387, "m_Index" },
            { 395, "m_IsArray" },
            { 405, "m_IsStatic" },
            { 416, "m_MetaFlag" },
            { 427, "m_Name" },
            { 434, "m_ObjectHideFlags" },
            { 452, "m_PrefabInternal" },
            { 469, "m_PrefabParentObject" },
            { 490, "m_Script" },
            { 499, "m_StaticEditorFlags" },
            { 519, "m_Type" },
            { 526, "m_Version" },
            { 536, "Object" },
            { 543, "pair" },
            { 548, "PPtr<Component>" },
            { 564, "PPtr<GameObject>" },
            { 581, "PPtr<Material>" },
            { 596, "PPtr<MonoBehaviour>" },
            { 616, "PPtr<MonoScript>" },
            { 633, "PPtr<Object>" },
            { 646, "PPtr<Prefab>" },
            { 659, "PPtr<Sprite>" },
            { 672, "PPtr<TextAsset>" },
            { 688, "PPtr<Texture>" },
            { 702, "PPtr<Texture2D>" },
            { 718, "PPtr<Transform>" },
            { 734, "Prefab" },
            { 741, "Quaternionf" },
            { 753, "Rectf" },
            { 759, "RectInt" },
            { 767, "RectOffset" },
            { 778, "second" },
            { 785, "set" },
            { 789, "short" },
            { 795, "size" },
            { 800, "SInt16" },
            { 807, "SInt32" },
            { 814, "SInt64" },
            { 821, "SInt8" },
            { 827, "staticvector" },
            { 840, "string" },
            { 847, "TextAsset" },
            { 857, "TextMesh" },
            { 866, "Texture" },
            { 874, "Texture2D" },
            { 884, "Transform" },
            { 894, "TypelessData" },
            { 907, "UInt16" },
            { 914, "UInt32" },
            { 921, "UInt64" },
            { 928, "UInt8" },
            { 934, "unsigned int" },
            { 947, "unsigned long long" },
            { 966, "unsigned short" },
            { 981, "vector" },
            { 988, "Vector2f" },
            { 997, "Vector3f" },
            { 1006, "Vector4f" },
            { 1015, "m_ScriptingClassIdentifier" },
            { 1042, "Gradient" },
            { 1051, "Type*" },
            { 1057, "int2_storage" },
            { 1070, "int3_storage" },
            { 1083, "BoundsInt" },
            { 1093, "m_CorrespondingSourceObject" },
            { 1121, "m_PrefabInstance" },
            { 1138, "m_PrefabAsset" },
            { 1152, "FileSize" },
            { 1161, "Hash128" }
        };
    }
}