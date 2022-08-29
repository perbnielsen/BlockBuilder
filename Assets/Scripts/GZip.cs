using System;
using System.IO;
using System.IO.Compression;

public class GZip
{
    public static byte[] compress(byte[] uncompressed)
    {
        if (uncompressed == null) throw new ArgumentNullException("uncompressed", "The given array is null!");
        if (uncompressed.LongLength > (long)int.MaxValue) throw new ArgumentException("The given array is to large!");
        using (MemoryStream memStream = new MemoryStream())
        {
            using (GZipStream gZipStream = new GZipStream(memStream, CompressionMode.Compress))
            {
                // Write the data to the stream to compress it
                gZipStream.Write(uncompressed, 0, uncompressed.Length);
                gZipStream.Close();
                // Get the compressed byte array back
                return memStream.ToArray();
            }
        }
    }

    public static byte[] decompress(byte[] compressed, int uncompressedSize)
    {
        if (compressed == null) throw new ArgumentNullException("compressed", "Data to decompress can't be null!");
        using (MemoryStream memStream = new MemoryStream(compressed))
        {
            using (GZipStream gZipStream = new GZipStream(memStream, CompressionMode.Decompress))
            {
                // Read the data into a buffer
                byte[] decompressed = new byte[uncompressedSize];
                gZipStream.Read(decompressed, 0, decompressed.Length);
                return decompressed;
            }
        }
    }
}
