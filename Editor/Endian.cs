
using System.IO;
using System.IO.Compression;
	
namespace LibGDX.Decoder
{
	public static class Endian
	{
		public static int Reverse( int value)
		{
			return	(value & 0xff) << 24
				|	((value >> 8) & 0xff) << 16
				|	((value >> 16) & 0xff) << 8
				|	((value >> 24) & 0xff);
		}
		public static uint Reverse( uint value)
		{
			return	(value & 0xff) << 24
				|	((value >> 8) & 0xff) << 16
				|	((value >> 16) & 0xff) << 8
				|	((value >> 24) & 0xff);
		}
	}
}