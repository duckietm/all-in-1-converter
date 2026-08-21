using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Tools
{
    public static class NativeSwfExtractor
    {
        private const string DisableEnvVar = "CONFURTER_DISABLE_NATIVE_SWF";
        private const string VerifyEnvVar = "CONFURTER_VERIFY_NATIVE_SWF";

        public static async Task ExtractWithFallbackAsync(
            string swfFilePath,
            string outputDirectory,
            Func<string, Task> ffdecRunner)
        {
            if (Environment.GetEnvironmentVariable(DisableEnvVar) == "1")
            {
                await ffdecRunner(outputDirectory);
                return;
            }

            bool nativeOk = false;
            try
            {
                nativeOk = Extract(swfFilePath, outputDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Native SWF extraction failed for {Path.GetFileName(swfFilePath)} ({ex.Message}); falling back to FFDEC.");
            }

            if (!nativeOk)
            {
                await ffdecRunner(outputDirectory);
                return;
            }

            if (Environment.GetEnvironmentVariable(VerifyEnvVar) == "1")
            {
                string verifyDirectory = outputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "_ffdec_verify";
                if (Directory.Exists(verifyDirectory)) Directory.Delete(verifyDirectory, recursive: true);
                Directory.CreateDirectory(verifyDirectory);
                await ffdecRunner(verifyDirectory);
                CompareExports(Path.GetFileName(swfFilePath), outputDirectory, verifyDirectory);
            }
        }

        public static bool Extract(string swfFilePath, string outputDirectory)
        {
            byte[]? body = ReadBody(swfFilePath);
            if (body == null) return false;

            int pos = 0;
            SkipHeaderRect(body, ref pos);
            pos += 4; // frame rate (u16) + frame count (u16)

            // Pass 1: collect the raw tags we care about.
            var symbols = new List<(int Id, string Name)>();
            var binaryTags = new List<(int CharId, byte[] Data)>();
            var bitmapTags = new List<(int Code, byte[] Payload)>();

            while (pos + 2 <= body.Length)
            {
                int codeAndLength = ReadU16(body, ref pos);
                int code = codeAndLength >> 6;
                int length = codeAndLength & 0x3F;
                if (length == 0x3F)
                {
                    if (pos + 4 > body.Length) return false;
                    length = (int)ReadU32(body, ref pos);
                }
                if (pos + length > body.Length) return false;

                if (code == 0) break; // End tag

                switch (code)
                {
                    case 76: // SymbolClass
                    {
                        int p = pos;
                        int count = ReadU16(body, ref p);
                        for (int i = 0; i < count; i++)
                        {
                            int tagId = ReadU16(body, ref p);
                            string name = ReadNullTerminated(body, ref p);
                            symbols.Add((tagId, name));
                        }
                        break;
                    }
                    case 87: // DefineBinaryData
                    {
                        int p = pos;
                        int charId = ReadU16(body, ref p);
                        p += 4; // reserved u32
                        binaryTags.Add((charId, body.Skip(p).Take(pos + length - p).ToArray()));
                        break;
                    }
                    case 20: // DefineBitsLossless
                    case 36: // DefineBitsLossless2
                    case 21: // DefineBitsJPEG2
                    case 35: // DefineBitsJPEG3
                        bitmapTags.Add((code, body.Skip(pos).Take(length).ToArray()));
                        break;
                    case 6:  // DefineBits (needs JPEGTables)
                    case 90: // DefineBitsJPEG4
                        return false; // let FFDEC handle these rare formats
                }

                pos += length;
            }

            // Name lookups: how many symbol names point at each character id.
            var namesById = symbols
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Name).ToList());

            // Pass 2: write the export tree.
            string imagesDir = ResetDirectory(Path.Combine(outputDirectory, "images"));
            string binaryDir = ResetDirectory(Path.Combine(outputDirectory, "binaryData"));
            string symbolDir = ResetDirectory(Path.Combine(outputDirectory, "symbolClass"));

            File.WriteAllLines(
                Path.Combine(symbolDir, "symbols.csv"),
                symbols.Select(s => $"{s.Id};\"{s.Name}\""));

            foreach (var (charId, data) in binaryTags)
            {
                string name = namesById.TryGetValue(charId, out var names) && names.Count > 0
                    ? $"{charId}_{names[0]}"
                    : charId.ToString();
                File.WriteAllBytes(Path.Combine(binaryDir, $"{name}.bin"), data);
            }

            foreach (var (code, payload) in bitmapTags)
            {
                int p = 0;
                int charId = ReadU16(payload, ref p);

                using Image<Rgba32>? image = code switch
                {
                    20 => DecodeLossless(payload, hasAlpha: false),
                    36 => DecodeLossless(payload, hasAlpha: true),
                    21 => DecodeJpeg2(payload),
                    35 => DecodeJpeg3(payload),
                    _ => null
                };

                if (image == null) return false;

                var names = namesById.TryGetValue(charId, out var list) ? list : new List<string>();
                string fileName = names.Count == 1 ? $"{charId}_{names[0]}.png" : $"{charId}.png";
                image.SaveAsPng(Path.Combine(imagesDir, fileName));
            }

            return true;
        }

        // ---------------------------------------------------------------
        // Container / primitives
        // ---------------------------------------------------------------

        private static byte[]? ReadBody(string swfFilePath)
        {
            byte[] raw = File.ReadAllBytes(swfFilePath);
            if (raw.Length < 9) return null;

            string signature = Encoding.ASCII.GetString(raw, 0, 3);

            if (signature == "FWS")
            {
                return raw.Skip(8).ToArray();
            }

            if (signature == "CWS")
            {
                using var input = new MemoryStream(raw, 8, raw.Length - 8);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }

            return null; // ZWS (LZMA) and everything else -> FFDEC fallback
        }

        private static void SkipHeaderRect(byte[] body, ref int pos)
        {
            int nbits = body[pos] >> 3;
            int totalBits = 5 + (4 * nbits);
            pos += (totalBits + 7) / 8;
        }

        private static int ReadU16(byte[] data, ref int pos)
        {
            int value = data[pos] | (data[pos + 1] << 8);
            pos += 2;
            return value;
        }

        private static uint ReadU32(byte[] data, ref int pos)
        {
            uint value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return value;
        }

        private static string ReadNullTerminated(byte[] data, ref int pos)
        {
            int start = pos;
            while (pos < data.Length && data[pos] != 0) pos++;
            string value = Encoding.UTF8.GetString(data, start, pos - start);
            pos++; // consume terminator
            return value;
        }

        private static byte[] Inflate(byte[] data, int offset)
        {
            using var input = new MemoryStream(data, offset, data.Length - offset);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }

        private static string ResetDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            Directory.CreateDirectory(path);
            return path;
        }

        // ---------------------------------------------------------------
        // Bitmap tag decoding
        // ---------------------------------------------------------------

        private static Image<Rgba32>? DecodeLossless(byte[] payload, bool hasAlpha)
        {
            int p = 2; // skip charId
            int format = payload[p]; p += 1;
            int width = ReadU16(payload, ref p);
            int height = ReadU16(payload, ref p);
            if (width <= 0 || height <= 0) return null;

            int paletteCount = 0;
            if (format == 3)
            {
                paletteCount = payload[p] + 1;
                p += 1;
            }

            byte[] data = Inflate(payload, p);
            var pixels = new byte[width * height * 4];

            if (format == 3)
            {
                int entrySize = hasAlpha ? 4 : 3;
                int paletteBytes = paletteCount * entrySize;
                int stride = (width + 3) & ~3;
                if (data.Length < paletteBytes + (stride * height)) return null;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = data[paletteBytes + (y * stride) + x];
                        if (index >= paletteCount) index = 0;
                        int e = index * entrySize;
                        byte r = data[e], g = data[e + 1], b = data[e + 2];
                        byte a = hasAlpha ? data[e + 3] : (byte)255;
                        WritePixel(pixels, ((y * width) + x) * 4, r, g, b, a, premultiplied: hasAlpha);
                    }
                }
            }
            else if (format == 5)
            {
                int stride = width * 4;
                if (data.Length < stride * height) return null;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int s = (y * stride) + (x * 4);
                        // stored as [A|reserved], R, G, B
                        byte a = hasAlpha ? data[s] : (byte)255;
                        byte r = data[s + 1], g = data[s + 2], b = data[s + 3];
                        WritePixel(pixels, ((y * width) + x) * 4, r, g, b, a, premultiplied: hasAlpha);
                    }
                }
            }
            else if (format == 4 && !hasAlpha)
            {
                int stride = ((width * 2) + 3) & ~3;
                if (data.Length < stride * height) return null;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int s = (y * stride) + (x * 2);
                        int pix = (data[s] << 8) | data[s + 1]; // PIX15 is big-endian
                        byte r = Expand5((pix >> 10) & 0x1F);
                        byte g = Expand5((pix >> 5) & 0x1F);
                        byte b = Expand5(pix & 0x1F);
                        WritePixel(pixels, ((y * width) + x) * 4, r, g, b, 255, premultiplied: false);
                    }
                }
            }
            else
            {
                return null;
            }

            return Image.LoadPixelData<Rgba32>(pixels, width, height);
        }

        private static byte Expand5(int value) => (byte)((value << 3) | (value >> 2));

        private static void WritePixel(byte[] pixels, int offset, byte r, byte g, byte b, byte a, bool premultiplied)
        {
            if (premultiplied)
            {
                if (a == 0)
                {
                    r = 0; g = 0; b = 0;
                }
                else if (a != 255)
                {
                    r = (byte)Math.Min(255, ((r * 255) + (a / 2)) / a);
                    g = (byte)Math.Min(255, ((g * 255) + (a / 2)) / a);
                    b = (byte)Math.Min(255, ((b * 255) + (a / 2)) / a);
                }
            }

            pixels[offset] = r;
            pixels[offset + 1] = g;
            pixels[offset + 2] = b;
            pixels[offset + 3] = a;
        }

        private static Image<Rgba32>? DecodeJpeg2(byte[] payload)
        {
            byte[] jpeg = StripToJpegStart(payload.Skip(2).ToArray());
            try
            {
                return Image.Load<Rgba32>(jpeg);
            }
            catch
            {
                return null;
            }
        }

        private static Image<Rgba32>? DecodeJpeg3(byte[] payload)
        {
            int p = 2;
            int alphaOffset = (int)ReadU32(payload, ref p);
            if (p + alphaOffset > payload.Length) return null;

            byte[] jpeg = StripToJpegStart(payload.Skip(p).Take(alphaOffset).ToArray());
            Image<Rgba32> image;
            try
            {
                image = Image.Load<Rgba32>(jpeg);
            }
            catch
            {
                return null;
            }

            byte[] alpha = Inflate(payload, p + alphaOffset);
            if (alpha.Length < image.Width * image.Height)
            {
                image.Dispose();
                return null;
            }

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    pixel.A = alpha[(y * image.Width) + x];
                    image[x, y] = pixel;
                }
            }

            return image;
        }

        private static byte[] StripToJpegStart(byte[] data)
        {
            // Flash-era JPEG data may be prefixed with a spurious EOI/SOI pair.
            for (int i = 0; i + 2 < data.Length; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD8 && data[i + 2] == 0xFF)
                {
                    return i == 0 ? data : data.Skip(i).ToArray();
                }
            }
            return data;
        }

        // ---------------------------------------------------------------
        // Verification against FFDEC (CONFURTER_VERIFY_NATIVE_SWF=1)
        // ---------------------------------------------------------------

        private static void CompareExports(string swfName, string nativeDir, string ffdecDir)
        {
            var issues = new List<string>();

            // symbols.csv: same (id, name) multiset.
            var nativeSymbols = ReadCsvPairs(Path.Combine(nativeDir, "symbolClass", "symbols.csv"));
            var ffdecSymbols = ReadCsvPairs(Path.Combine(ffdecDir, "symbolClass", "symbols.csv"));
            if (!nativeSymbols.OrderBy(x => x).SequenceEqual(ffdecSymbols.OrderBy(x => x)))
                issues.Add($"symbols.csv differs (native {nativeSymbols.Count} vs ffdec {ffdecSymbols.Count} records)");

            // binaryData: same file names and identical bytes.
            issues.AddRange(CompareFiles(nativeDir, ffdecDir, "binaryData", "*.bin",
                (a, b) => File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)) ? null : "bytes differ"));

            // images: same file names, pixel-equal within ±1 (premultiply rounding).
            issues.AddRange(CompareFiles(nativeDir, ffdecDir, "images", "*.png", (a, b) =>
            {
                using var ia = Image.Load<Rgba32>(a);
                using var ib = Image.Load<Rgba32>(b);
                if (ia.Width != ib.Width || ia.Height != ib.Height) return $"size {ia.Width}x{ia.Height} vs {ib.Width}x{ib.Height}";
                int off = 0;
                for (int y = 0; y < ia.Height; y++)
                    for (int x = 0; x < ia.Width; x++)
                    {
                        var pa = ia[x, y];
                        var pb = ib[x, y];
                        if (pa.A == 0 && pb.A == 0) continue;
                        if (Math.Abs(pa.R - pb.R) > 1 || Math.Abs(pa.G - pb.G) > 1 ||
                            Math.Abs(pa.B - pb.B) > 1 || Math.Abs(pa.A - pb.A) > 1) off++;
                    }
                return off == 0 ? null : $"{off} pixels differ by more than 1";
            }));

            if (issues.Count == 0)
            {
                Console.WriteLine($"✅ [verify] {swfName}: native export matches FFDEC.");
            }
            else
            {
                Console.WriteLine($"❌ [verify] {swfName}: {issues.Count} difference(s):");
                foreach (var issue in issues) Console.WriteLine($"   - {issue}");
            }
        }

        private static List<string> ReadCsvPairs(string path)
        {
            if (!File.Exists(path)) return new List<string>();
            return File.ReadLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().Replace("\"", ""))
                .ToList();
        }

        private static IEnumerable<string> CompareFiles(
            string nativeDir, string ffdecDir, string subDir, string pattern,
            Func<string, string, string?> comparer)
        {
            string a = Path.Combine(nativeDir, subDir);
            string b = Path.Combine(ffdecDir, subDir);
            var aFiles = Directory.Exists(a) ? Directory.GetFiles(a, pattern).Select(Path.GetFileName).ToHashSet() : new HashSet<string?>();
            var bFiles = Directory.Exists(b) ? Directory.GetFiles(b, pattern).Select(Path.GetFileName).ToHashSet() : new HashSet<string?>();

            foreach (var missing in bFiles.Except(aFiles)) yield return $"{subDir}/{missing}: missing in native export";
            foreach (var extra in aFiles.Except(bFiles)) yield return $"{subDir}/{extra}: only in native export";

            foreach (var name in aFiles.Intersect(bFiles))
            {
                string? problem = null;
                try
                {
                    problem = comparer(Path.Combine(a, name!), Path.Combine(b, name!));
                }
                catch (Exception ex)
                {
                    problem = $"compare failed: {ex.Message}";
                }
                if (problem != null) yield return $"{subDir}/{name}: {problem}";
            }
        }
    }
}
