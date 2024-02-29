using AssetStudio;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudioGUI
{
    /// <summary>
    /// Performs 32-bit reversed cyclic redundancy checks.
    /// </summary>
    public class Crc32
    {
        #region Constants
        /// <summary>
        /// Generator polynomial (modulo 2) for the reversed CRC32 algorithm. 
        /// </summary>
        private const UInt32 s_generator = 0x04c11db7;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the Crc32 class.
        /// </summary>
        public Crc32()
        {
            byte v1; // r10
            uint v2; // eax
            byte v3; // r8
            int v4; // edx
            uint v5; // ecx
            int v6; // eax
            uint v7; // er9
            long result; // rax

            v1 = 0;
            m_checksumTable = new uint[256];
            do
            {
                v2 = 0;
                v3 = 0x80;
                do
                {
                    v4 = (int)(v2 ^ 0x80000000);
                    if ((v1 & v3) == 0)
                        v4 = (int)v2;
                    v2 = (uint)((2 * v4) ^ 0x4C11DB7);
                    if (v4 >= 0)
                        v2 = (uint)(2 * v4);
                    v3 >>= 1;
                }
                while (v3 > 0);
                v5 = ((2 * v2) ^ ((2 * v2) ^ (v2 >> 1)) & 0x55555555) >> 2;
                v6 = (int)(4 * ((2 * v2) ^ ((2 * v2) ^ (v2 >> 1)) & 0x55555555));
                v7 = (uint)((0x10 * (v6 ^ (v6 ^ v5) & 0x33333333)) ^ ((0x10 * (v6 ^ (v6 ^ v5) & 0x33333333)) ^ ((v6 ^ (v6 ^ v5) & 0x33333333) >> 4)) & 0xF0F0F0F);
                result = (0x10101 * (((0x8020 * v1) & 0x88440) | (0x802 * v1) & 0x22110u)) >> 0x10;

                var meow = (v7 << 8) ^ ((v7 << 8) ^ (v7 >> 8)) & 0xFF00FF;
                meow = (meow >> 0x10) | (meow << (32 - 0x10));
                m_checksumTable[(byte)((0x10101 * ((0x8020 * v1) & 0x88440 | (0x802 * v1) & 0x22110u)) >> 0x10)] = meow;
                ++v1;
            }
            while (v1 > 0);

            // Constructs the checksum lookup table. Used to optimize the checksum.
            //m_checksumTable = Enumerable.Range(0, 256).Select(i =>
            //{
            //    var tableEntry = (uint)i;
            //    for (var j = 0; j < 8; ++j)
            //    {
            //        tableEntry = ((tableEntry & 1) != 0)
            //            ? (s_generator ^ (tableEntry >> 1))
            //            : (tableEntry >> 1);
            //    }
            //    return tableEntry;
            //}).ToArray();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Calculates the checksum of the byte stream.
        /// </summary>
        /// <param name="byteStream">The byte stream to calculate the checksum for.</param>
        /// <returns>A 32-bit reversed checksum.</returns>
        public Int32 Get<T>(IEnumerable<T> byteStream)
        {
            try
            {
                UInt32 crc = 0xffffffff;
                var bytelength = byteStream.Count();
                var i = 0;
                while (bytelength-- != 0)
                {
                    crc = (uint)(m_checksumTable[(byte)crc ^ (Convert.ToByte(byteStream.ElementAt(i)))] ^ (crc >> 8));
                    ++i;
                }
                // return (~crc); also works
                return (int)(crc ^ 0xffffffff);
            }
            catch (FormatException e)
            {
                throw new Exception("Could not read the stream out as bytes.", e);
            }
            catch (InvalidCastException e)
            {
                throw new Exception("Could not read the stream out as bytes.", e);
            }
            catch (OverflowException e)
            {
                throw new Exception("Could not read the stream out as bytes.", e);
            }
        }
        #endregion

        #region Fields
        /// <summary>
        /// Contains a cache of calculated checksum chunks.
        /// </summary>
        private readonly UInt32[] m_checksumTable;

        #endregion
    }

    public static class Utils
    {
        public static IEnumerable<KeyValuePair<object, object>> ForLinq(this IDictionaryEnumerator iter)
        {
            using (iter as IDisposable)
            {
                while (iter.MoveNext()) yield return new KeyValuePair<object, object>(iter.Key, iter.Value);
            }
        }
    }

    internal static class Exporter
    {
        public static bool ExportTexture2D(AssetItem item, string name, string exportPath)
        {
            var m_Texture2D = (Texture2D)item.Asset;
            if (Properties.Settings.Default.convertTexture)
            {
                var type = Properties.Settings.Default.convertType;
                if (!TryExportFileRename(exportPath, name, item, "." + type.ToString().ToLower(), out var exportFullPath))
                    return false;
                var image = m_Texture2D.ConvertToImage(true);
                if (image == null)
                    return false;
                using (image)
                {
                    using (var file = File.OpenWrite(exportFullPath))
                    {
                        image.WriteToStream(file, type);
                    }
                    return true;
                }
            }
            else
            {
                if (!TryExportFileRename(exportPath, name, item, ".tex", out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_Texture2D.image_data.GetData());
                return true;
            }
        }
        //public static bool ExportTexture2D(AssetItem item, string name, string exportPath)
        //{
        //    var m_Texture2D = (Texture2D)item.Asset;
        //    if (Properties.Settings.Default.convertTexture)
        //    {
        //        var bitmap = m_Texture2D.ConvertToBitmap(true);
        //        if (bitmap == null)
        //            return false;
        //        ImageFormat format = null;
        //        var ext = Properties.Settings.Default.convertType;
        //        bool tga = false;
        //        switch (ext)
        //        {
        //            case "BMP":
        //                format = ImageFormat.Bmp;
        //                break;
        //            case "PNG":
        //                format = ImageFormat.Png;
        //                break;
        //            case "JPEG":
        //                format = ImageFormat.Jpeg;
        //                break;
        //            case "TGA":
        //                tga = true;
        //                break;
        //        }
        //        if (!TryExportFileRename(exportPath, name, item, "." + ext.ToLower(), out var exportFullPath))
        //            return false;
        //        if (tga)
        //        {
        //            var file = new TGA(bitmap);
        //            file.Save(exportFullPath);
        //        }
        //        else
        //        {
        //            bitmap.Save(exportFullPath, format);
        //        }
        //        bitmap.Dispose();
        //        return true;
        //    }
        //    else
        //    {
        //        if (!TryExportFileRename(exportPath, name, item, ".tex", out var exportFullPath))
        //            return false;
        //        File.WriteAllBytes(exportFullPath, m_Texture2D.image_data.GetData());
        //        return true;
        //    }
        //}
        public static bool ExportTexture2D(AssetItem item, string exportPath)
        {
            var m_Texture2D = (Texture2D)item.Asset;
            if (Properties.Settings.Default.convertTexture)
            {
                var type = Properties.Settings.Default.convertType;
                if (!TryExportFile(exportPath, item, "." + type.ToString().ToLower(), out var exportFullPath))
                    return false;
                var image = m_Texture2D.ConvertToImage(true);
                if (image == null)
                    return false;
                using (image)
                {
                    using (var file = File.OpenWrite(exportFullPath))
                    {
                        image.WriteToStream(file, type);
                    }
                    return true;
                }
            }
            else
            {
                if (!TryExportFile(exportPath, item, ".tex", out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_Texture2D.image_data.GetData());
                return true;
            }
        }

        public static bool ExportAudioClip(AssetItem item, string exportPath)
        {
            var m_AudioClip = (AudioClip)item.Asset;
            var m_AudioData = m_AudioClip.m_AudioData.GetData();
            if (m_AudioData == null || m_AudioData.Length == 0)
                return false;
            var converter = new AudioClipConverter(m_AudioClip);
            if (Properties.Settings.Default.convertAudio && converter.IsSupport)
            {
                if (!TryExportFile(exportPath, item, ".wav", out var exportFullPath))
                    return false;
                var buffer = converter.ConvertToWav();
                if (buffer == null)
                    return false;
                File.WriteAllBytes(exportFullPath, buffer);
            }
            else
            {
                if (!TryExportFile(exportPath, item, converter.GetExtensionName(), out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_AudioData);
            }
            return true;
        }

        public static bool ExportShader(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".shader", out var exportFullPath))
                return false;
            var m_Shader = (Shader)item.Asset;
            var str = m_Shader.Convert();
            File.WriteAllText(exportFullPath, str);
            return true;
        }

        public static bool ExportTextAsset(AssetItem item, string exportPath)
        {
            var m_TextAsset = (TextAsset)(item.Asset);
            var extension = ".txt";
            if (Properties.Settings.Default.restoreExtensionName)
            {
                if (!string.IsNullOrEmpty(item.Container))
                {
                    extension = Path.GetExtension(item.Container);
                }
            }
            if (!TryExportFile(exportPath, item, extension, out var exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, m_TextAsset.m_Script);
            return true;
        }

        public static bool ExportMonoBehaviour(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".json", out var exportFullPath))
                return false;
            var m_MonoBehaviour = (MonoBehaviour)item.Asset;
            var type = m_MonoBehaviour.ToType();
            if (type == null)
            {
                var m_Type = Studio.MonoBehaviourToTypeTree(m_MonoBehaviour);
                type = m_MonoBehaviour.ToType(m_Type);
            }
            var str = JsonConvert.SerializeObject(type, Formatting.Indented);
            File.WriteAllText(exportFullPath, str);
            return true;
        }

        public static bool ExportFont(AssetItem item, string exportPath)
        {
            var m_Font = (Font)item.Asset;
            if (m_Font.m_FontData != null)
            {
                var extension = ".ttf";
                if (m_Font.m_FontData[0] == 79 && m_Font.m_FontData[1] == 84 && m_Font.m_FontData[2] == 84 && m_Font.m_FontData[3] == 79)
                {
                    extension = ".otf";
                }
                if (!TryExportFile(exportPath, item, extension, out var exportFullPath))
                    return false;
                File.WriteAllBytes(exportFullPath, m_Font.m_FontData);
                return true;
            }
            return false;
        }

        public static bool ExportMesh(AssetItem item, string exportPath)
        {
            var m_Mesh = (Mesh)item.Asset;
            if (m_Mesh.m_VertexCount <= 0)
                return false;
            if (!TryExportFile(exportPath, item, ".obj", out var exportFullPath))
                return false;
            var sb = new StringBuilder();
            sb.AppendLine("g " + m_Mesh.m_Name);
            #region Vertices
            if (m_Mesh.m_Vertices == null || m_Mesh.m_Vertices.Length == 0)
            {
                return false;
            }
            int c = 3;
            if (m_Mesh.m_Vertices.Length == m_Mesh.m_VertexCount * 4)
            {
                c = 4;
            }
            for (int v = 0; v < m_Mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("v {0} {1} {2}\r\n", -m_Mesh.m_Vertices[v * c], m_Mesh.m_Vertices[v * c + 1], m_Mesh.m_Vertices[v * c + 2]);
            }
            #endregion

            #region UV
            if (m_Mesh.m_UV0?.Length > 0)
            {
                c = 4;
                if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 2)
                {
                    c = 2;
                }
                else if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vt {0} {1}\r\n", m_Mesh.m_UV0[v * c], m_Mesh.m_UV0[v * c + 1]);
                }
            }
            #endregion

            #region Normals
            if (m_Mesh.m_Normals?.Length > 0)
            {
                if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                else if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 4)
                {
                    c = 4;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vn {0} {1} {2}\r\n", -m_Mesh.m_Normals[v * c], m_Mesh.m_Normals[v * c + 1], m_Mesh.m_Normals[v * c + 2]);
                }
            }
            #endregion

            #region Face
            int sum = 0;
            for (var i = 0; i < m_Mesh.m_SubMeshes.Length; i++)
            {
                sb.AppendLine($"g {m_Mesh.m_Name}_{i}");
                int indexCount = (int)m_Mesh.m_SubMeshes[i].indexCount;
                var end = sum + indexCount / 3;
                for (int f = sum; f < end; f++)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n", m_Mesh.m_Indices[f * 3 + 2] + 1, m_Mesh.m_Indices[f * 3 + 1] + 1, m_Mesh.m_Indices[f * 3] + 1);
                }
                sum = end;
            }
            #endregion

            sb.Replace("NaN", "0");
            File.WriteAllText(exportFullPath, sb.ToString());
            return true;
        }

        public static bool ExportVideoClip(AssetItem item, string exportPath)
        {
            var m_VideoClip = (VideoClip)item.Asset;
            if (m_VideoClip.m_ExternalResources.m_Size > 0)
            {
                if (!TryExportFile(exportPath, item, Path.GetExtension(m_VideoClip.m_OriginalPath), out var exportFullPath))
                    return false;
                m_VideoClip.m_VideoData.WriteData(exportFullPath);
                return true;
            }
            return false;
        }

        public static bool ExportMovieTexture(AssetItem item, string exportPath)
        {
            var m_MovieTexture = (MovieTexture)item.Asset;
            if (!TryExportFile(exportPath, item, ".ogv", out var exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, m_MovieTexture.m_MovieData);
            return true;
        }

        public static bool ExportSprite(AssetItem item, string exportPath)
        {
            var type = Properties.Settings.Default.convertType;
            if (!TryExportFile(exportPath, item, "." + type.ToString().ToLower(), out var exportFullPath))
                return false;
            var image = ((Sprite)item.Asset).GetImage();
            if (image != null)
            {
                using (image)
                {
                    using (var file = File.OpenWrite(exportFullPath))
                    {
                        image.WriteToStream(file, type);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool ExportSpriteName(AssetItem item, string name, string exportPath)
        {
            var type = Properties.Settings.Default.convertType;
            if (!TryExportFileRename(exportPath, name, item, "." + type.ToString().ToLower(), out var exportFullPath))
                return false;
            var image = ((Sprite)item.Asset).GetImage();
            if (image != null)
            {
                using (image)
                {
                    using (var file = File.OpenWrite(exportFullPath))
                    {
                        image.WriteToStream(file, type);
                    }
                    return true;
                }
            }
            return false;
        }

        //public static bool ExportSpriteName(AssetItem item, string name, string exportPath)
        //{
        //    ImageFormat format = null;
        //    var type = Properties.Settings.Default.convertType;
        //    bool tga = false;
        //    switch (type)
        //    {
        //        case "BMP":
        //            format = ImageFormat.Bmp;
        //            break;
        //        case "PNG":
        //            format = ImageFormat.Png;
        //            break;
        //        case "JPEG":
        //            format = ImageFormat.Jpeg;
        //            break;
        //        case "TGA":
        //            tga = true;
        //            break;
        //    }
        //    if (!TryExportFileRename(exportPath, name, item, "." + type.ToLower(), out var exportFullPath))
        //        return false;
        //    var bitmap = ((Sprite)item.Asset).GetImage();
        //    if (bitmap != null)
        //    {
        //        if (tga)
        //        {
        //            var file = new TGA(bitmap);
        //            file.Save(exportFullPath);
        //        }
        //        else
        //        {
        //            bitmap.Save(exportFullPath, format);
        //        }
        //        bitmap.Dispose();
        //        return true;
        //    }
        //    return false;
        //}

        public static bool ExportRawFile(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".dat", out var exportFullPath))
                return false;
            File.WriteAllBytes(exportFullPath, item.Asset.GetRawData());
            return true;
        }

        private static bool TryExportFile(string dir, AssetItem item, string extension, out string fullPath)
        {
            var fileName = FixFileName(item.Text);
            fullPath = Path.Combine(dir, fileName + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            fullPath = Path.Combine(dir, fileName + item.UniqueID + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            return false;
        }

        private static bool TryExportFileRename(string dir, string name, AssetItem item, string extension, out string fullPath)
        {
            var fileName = FixFileName(name);
            fullPath = Path.Combine(dir, fileName + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            fullPath = Path.Combine(dir, fileName + item.UniqueID + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            return false;
        }

        public static bool ExportAnimator(AssetItem item, string exportPath, List<AssetItem> animationList = null)
        {
            var exportFullPath = Path.Combine(exportPath, item.Text, item.Text + ".fbx");
            if (File.Exists(exportFullPath))
            {
                exportFullPath = Path.Combine(exportPath, item.Text + item.UniqueID, item.Text + ".fbx");
            }
            var m_Animator = (Animator)item.Asset;
            var convert = animationList != null
                ? new ModelConverter(m_Animator, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(m_Animator, Properties.Settings.Default.convertType);
            ExportFbx(convert, exportFullPath);
            return true;
        }

        public static void ExportGameObject(GameObject gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var convert = animationList != null
                ? new ModelConverter(gameObject, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(gameObject, Properties.Settings.Default.convertType);
            exportPath = exportPath + FixFileName(gameObject.m_Name) + ".fbx";
            ExportFbx(convert, exportPath);
        }

        public static void ExportGameObjectMerge(List<GameObject> gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var rootName = Path.GetFileNameWithoutExtension(exportPath);
            var convert = animationList != null
                ? new ModelConverter(rootName, gameObject, Properties.Settings.Default.convertType, animationList.Select(x => (AnimationClip)x.Asset).ToArray())
                : new ModelConverter(rootName, gameObject, Properties.Settings.Default.convertType);
            ExportFbx(convert, exportPath);
        }

        private static void ExportFbx(IImported convert, string exportPath)
        {
            var eulerFilter = Properties.Settings.Default.eulerFilter;
            var filterPrecision = (float)Properties.Settings.Default.filterPrecision;
            var exportAllNodes = Properties.Settings.Default.exportAllNodes;
            var exportSkins = Properties.Settings.Default.exportSkins;
            var exportAnimations = Properties.Settings.Default.exportAnimations;
            var exportBlendShape = Properties.Settings.Default.exportBlendShape;
            var castToBone = Properties.Settings.Default.castToBone;
            var boneSize = (int)Properties.Settings.Default.boneSize;
            var exportAllUvsAsDiffuseMaps = Properties.Settings.Default.exportAllUvsAsDiffuseMaps;
            var scaleFactor = (float)Properties.Settings.Default.scaleFactor;
            var fbxVersion = Properties.Settings.Default.fbxVersion;
            var fbxFormat = Properties.Settings.Default.fbxFormat;
            ModelExporter.ExportFbx(exportPath, convert, eulerFilter, filterPrecision,
                exportAllNodes, exportSkins, exportAnimations, exportBlendShape, castToBone, boneSize, exportAllUvsAsDiffuseMaps, scaleFactor, fbxVersion, fbxFormat == 1);
        }

        private static void ExportAssetTableUsingPathID(List<AssetItem> allAssets, string exportPath, List<object> m_assets)
        {
            foreach (var i in m_assets)
            {
                if (i is OrderedDictionary m_i)
                {
                    var ident = m_i["m_identifier"] as string;
                    var o_sprite_path = (m_i["m_originalSprite"] as OrderedDictionary)["m_PathID"];
                    if (o_sprite_path is long m_o_sprite_path)
                    {
                        if (m_o_sprite_path == 0)
                        {
                            System.Console.WriteLine(o_sprite_path);
                        }
                        var asset = allAssets.Find((x) => x.m_PathID == m_o_sprite_path);
                        if (asset is AssetItem m_asset)
                        {
                            if (m_asset.Type == ClassIDType.Sprite)
                            {
                                ExportSpriteName(m_asset, ident, exportPath);
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(o_sprite_path);
                    }
                }
            }
        }

        private static void ExportAssetTableWithResources(List<object> m_assets, List<object> resources, List<KeyValuePair<string, int>> AssetIndices, List<AssetItem> allAssets, string exportPath)
        {
            for (int index = 0; index < m_assets.Count; index++)
            {
                var asset = m_assets[index];
                var resource = resources[index];
                // Crc32
                if (asset is OrderedDictionary asset_dict)
                {
                    if (resource is OrderedDictionary resource_dict)
                    {
                        var ident = asset_dict["m_identifier"] as string;
                        var crc = new Crc32();
                        var guid = resource_dict["_editorGuid"] as string;
                        var hashedGuid = crc.Get(guid);
                        var AssetIndex = AssetIndices.Find(x => x.Value == hashedGuid);
                        var a = allAssets.Find((x) => x.Text == AssetIndex.Key);
                        if (a is AssetItem m_asset)
                        {
                            if (m_asset.Type == ClassIDType.Sprite)
                            {
                                ExportSpriteName(m_asset, ident, exportPath);
                            }
                            else if (m_asset.Type == ClassIDType.Texture2D)
                            {
                                ExportTexture2D(m_asset, ident, exportPath);
                            }
                        }
                    }
                }
            }
        }

        private static bool ExportAssetTable2019(AssetItem item, List<KeyValuePair<string, int>> AssetIndices, List<AssetItem> allAssets, string exportPath)
        {
            var type = item.Asset.ToType();
            var assets = type["m_assetTable"];
            var resources = type["_resources"] as List<object>;
            if (assets is List<object> m_assets)
            {
                if (resources != null)
                {
                    ExportAssetTableWithResources(m_assets, resources, AssetIndices, allAssets, exportPath);
                }
                else
                {
                    ExportAssetTableUsingPathID(allAssets, exportPath, m_assets);
                }
            }
            return true;
        }

        private static bool ExportAssetTable2021(AssetItem item, List<KeyValuePair<string, int>> AssetIndices, List<AssetItem> allAssets, string exportPath)
        {
            var type = item.Asset.ToType();
            var Entries = type["Entries"];
            var resources = type["Resources"] as List<object>;
            if (Entries is List<object> m_assets)
            {
                if (resources != null)
                {
                    ExportAssetTableWithResources(m_assets, resources, AssetIndices, allAssets, exportPath);
                }
                else
                {
                    ExportAssetTableUsingPathID(allAssets, exportPath, m_assets);
                }
            }
            return true;
        }

        private static List<KeyValuePair<string, int>> LookupAssetIndices(List<AssetItem> allAssets)
        {
            var master_index = allAssets.Find(x => x.Text == "master_index");
            var AssetBundles = master_index.Asset.ToType()["AssetBundles"] as List<object>;
            var AssetIndices = new List<KeyValuePair<String, int>>();
            foreach (var AssetBundle in AssetBundles)
            {
                var assetIndices = (AssetBundle as OrderedDictionary)["_assetIndices"] as List<object>;
                var mappedIndices = assetIndices.Select(index =>
                {
                    var indexDict = index as OrderedDictionary;
                    var name = indexDict["_name"] as String;
                    var hashedGuid = (int)indexDict["_hashedGuid"];
                    return new KeyValuePair<string, int>(name, hashedGuid);
                }).ToList();
                AssetIndices.AddRange(mappedIndices);
            }
            return AssetIndices;
        }

        public static bool ExportAssetTable(AssetItem item, List<AssetItem> allAssets, string exportPath)
        {
            if (item.Asset is MonoBehaviour m_MonoBehaviour)
            {
                var type = m_MonoBehaviour.ToType();
                var AssetIndices = LookupAssetIndices(allAssets);
                if (type.Contains("m_assetTable"))
                {
                    return ExportAssetTable2019(item, AssetIndices, allAssets, exportPath);
                }
                else
                {
                    return ExportAssetTable2021(item, AssetIndices, allAssets, exportPath);
                }
            }
            return false;
        }

        public static bool ExportDumpFile(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".txt", out var exportFullPath))
                return false;
            var str = item.Asset.Dump();
            if (str == null && item.Asset is MonoBehaviour m_MonoBehaviour)
            {
                var m_Type = Studio.MonoBehaviourToTypeTree(m_MonoBehaviour);
                str = m_MonoBehaviour.Dump(m_Type);
            }
            if (str != null)
            {
                File.WriteAllText(exportFullPath, str);
                return true;
            }
            return false;
        }

        public static bool ExportConvertFile(AssetItem item, string exportPath)
        {
            switch (item.Type)
            {
                case ClassIDType.Texture2D:
                    return ExportTexture2D(item, exportPath);
                case ClassIDType.AudioClip:
                    return ExportAudioClip(item, exportPath);
                case ClassIDType.Shader:
                    return ExportShader(item, exportPath);
                case ClassIDType.TextAsset:
                    return ExportTextAsset(item, exportPath);
                case ClassIDType.MonoBehaviour:
                    return ExportMonoBehaviour(item, exportPath);
                case ClassIDType.Font:
                    return ExportFont(item, exportPath);
                case ClassIDType.Mesh:
                    return ExportMesh(item, exportPath);
                case ClassIDType.VideoClip:
                    return ExportVideoClip(item, exportPath);
                case ClassIDType.MovieTexture:
                    return ExportMovieTexture(item, exportPath);
                case ClassIDType.Sprite:
                    return ExportSprite(item, exportPath);
                case ClassIDType.Animator:
                    return ExportAnimator(item, exportPath);
                case ClassIDType.AnimationClip:
                    return false;
                default:
                    return ExportRawFile(item, exportPath);
            }
        }

        public static string FixFileName(string str)
        {
            if (str.Length >= 260) return Path.GetRandomFileName();
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }
    }
}
