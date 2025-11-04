
using System.IO;
using System.IO.Compression;
	
namespace LibGDX.Decoder
{
	public static class GZip
	{
		public static bool IsCompressed( byte[] bytes)
		{
			return bytes.Length > 2 && bytes[ 0] == 0x1f && bytes[ 1] == 0x8b;
		}
		public static byte[] Decompress( byte[] compressed)
		{
			using( var input = new MemoryStream( compressed))
			using( var gzip = new GZipStream( input, CompressionMode.Decompress))
			using( var output = new MemoryStream())
			{
				gzip.CopyTo( output);
				return output.ToArray();
			}
		}
	}
}