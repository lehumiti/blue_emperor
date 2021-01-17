//-------------------------------------------------
//                    TNet 3
// Copyright © 2012-2018 Tasharen Entertainment Inc
//-------------------------------------------------

using System.IO;

namespace TNet
{
	/// <summary>
	/// BinaryReader and BinaryWriter extension methods.
	/// </summary>

	static public class BinaryExtensions
	{
		static System.Collections.Generic.Dictionary<byte, object[]> mTemp =
			new System.Collections.Generic.Dictionary<byte, object[]>();

		/// <summary>
		/// Get a temporary array of specified size.
		/// </summary>

		static object[] GetTempBuffer (int count)
		{
			object[] temp;

			if (!mTemp.TryGetValue((byte)count, out temp))
			{
				temp = new object[count];
				mTemp[(byte)count] = temp;
			}
			return temp;
		}

		/// <summary>
		/// Write the array of objects into the specified writer.
		/// </summary>

		static public void WriteArray (this BinaryWriter bw, object[] objs)
		{
			if (objs != null)
			{
				bw.WriteInt(objs.Length);
				if (objs.Length == 0) return;
				for (int b = 0, bmax = objs.Length; b < bmax; ++b) bw.WriteObject(objs[b]);
			}
			else bw.WriteInt(0);
		}

		/// <summary>
		/// Read the object array from the specified reader.
		/// </summary>

		static public object[] ReadArray (this BinaryReader reader)
		{
			var count = reader.ReadInt();
			if (count == 0) return null;

			var temp = GetTempBuffer(count);

			for (int i = 0; i < count; ++i)
				temp[i] = reader.ReadObject();

			return temp;
		}

		/// <summary>
		/// Read the object array from the specified reader. The first value will be set to the specified object.
		/// </summary>

		static public object[] ReadArray (this BinaryReader reader, object obj)
		{
			var count = reader.ReadInt() + 1;
			var temp = GetTempBuffer(count);

			temp[0] = obj;
			for (int i = 1; i < count; ++i)
				temp[i] = reader.ReadObject();

			return temp;
		}

		/// <summary>
		/// Combine the specified object and array into one array in an efficient manner.
		/// </summary>

		static public object[] CombineArrays (object obj, object[] objs)
		{
			var count = objs.Length;
			var temp = GetTempBuffer(count + 1);

			temp[0] = obj;
			for (int i = 0; i < count; ++i)
				temp[i + 1] = objs[i];

			return temp;
		}
	}
}
