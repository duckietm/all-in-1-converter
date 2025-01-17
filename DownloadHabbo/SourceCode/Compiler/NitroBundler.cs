using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public class NitroBundler
    {
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

        public void AddFile(string name, byte[] data)
        {
            _files[name] = data;
        }

        public async Task<byte[]> ToBufferAsync()
        {
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write((short)_files.Count);

            foreach (var file in _files)
            {
                string fileName = file.Key;
                byte[] fileData = file.Value;

                binaryWriter.Write((short)fileName.Length);
                binaryWriter.Write(Encoding.UTF8.GetBytes(fileName));

                byte[] compressed = Compress(fileData);

                binaryWriter.Write(compressed.Length);
                binaryWriter.Write(compressed);
            }

            return memoryStream.ToArray();
        }

        private static byte[] Compress(byte[] data)
        {
            using var memoryStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
            {
                deflateStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
    }
}