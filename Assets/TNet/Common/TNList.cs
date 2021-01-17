//-------------------------------------------------
//                    TNet 3
// Copyright © 2012-2018 Tasharen Entertainment Inc
//-------------------------------------------------

using System.Diagnostics;

namespace TNet
{
	/// <summary>
	/// Generic interface with an Add function so that it can be used by the Serializer.
	/// </summary>

	public interface TList
	{
		int Count { get; }
		object Get (int index);
		void Add (object obj);
		void RemoveAt (int index);
		void Clear ();
	}

	/// <summary>
	/// This improved version of the System.Collections.Generic.List that doesn't release the buffer on Clear(),
	/// resulting in better performance and less garbage collection.
	/// </summary>

	[System.Serializable]
	public class List<T> : TList
	{
		/// <summary>
		/// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use List.size.
		/// </summary>

		public T[] buffer;

		/// <summary>
		/// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
		/// </summary>

		public int size = 0;

		public List () { }
		public List (int size) { Allocate(size); }

		/// <summary>
		/// For 'foreach' functionality.
		/// </summary>

		public System.Collections.Generic.IEnumerator<T> GetEnumerator ()
		{
			if (buffer != null)
			{
				for (int i = 0; i < size; ++i)
				{
					yield return buffer[i];
				}
			}
		}

		/// <summary>
		/// Convenience function. I recommend using .buffer instead.
		/// </summary>

		[System.Obsolete("Use list.buffer[index], it's a lot faster as it avoids the operator")]
		public T this[int i]
		{
			get { return buffer[i]; }
			set { buffer[i] = value; }
		}

		/// <summary>
		/// For compatibility with Systems.Collections.Generic.List. It's faster to use 'size' instead.
		/// </summary>

		public int Count { get { return size; } }

		/// <summary>
		/// Used by the TList interface, needed to be able to iterate through the data in the Serializer.
		/// </summary>

		public object Get (int index) { return buffer[index]; }

		/// <summary>
		/// Ensure that the list has allocated at least the specified number of elements.
		/// Note that this will not resize the array or actually add the said elements. It only allocates memory upfront.
		/// </summary>

		public void Allocate (int newSize)
		{
			if (buffer == null || buffer.Length < newSize)
			{
				T[] newList = new T[newSize];
				if (buffer != null && this.size > 0) buffer.CopyTo(newList, 0);
				buffer = newList;
			}
		}

		/// <summary>
		/// Helper function that expands the size of the array, maintaining the content.
		/// </summary>

		public void AllocateMore ()
		{
			int max = (buffer == null) ? 0 : (buffer.Length << 1);
			if (max < 32) max = 32;
			T[] newList = new T[max];
			if (buffer != null && size > 0) buffer.CopyTo(newList, 0);
			buffer = newList;
		}

		/// <summary>
		/// Resize the array to the specified size, keeping the existing elements.
		/// </summary>

		public void Resize (int newSize)
		{
			if (size != newSize)
			{
				if (buffer == null) buffer = new T[newSize];
				else System.Array.Resize(ref buffer, newSize);
				size = newSize;
			}
		}

		/// <summary>
		/// Trim the unnecessary memory, resizing the buffer to be of 'Length' size.
		/// Call this function only if you are sure that the buffer won't need to resize anytime soon.
		/// </summary>

		public void Trim ()
		{
			if (size > 0)
			{
				if (size < buffer.Length)
				{
					T[] newList = new T[size];
					for (int i = 0; i < size; ++i) newList[i] = buffer[i];
					buffer = newList;
				}
			}
			else buffer = new T[0];
		}

		/// <summary>
		/// Clear the array by resetting its size to zero. Note that the memory is not actually released.
		/// </summary>

		public void Clear () { size = 0; }

		/// <summary>
		/// Clear the array by resetting its size to zero and reserve memory for the specified number of entries.
		/// </summary>

		public void Clear (int allocateSize) { size = 0; Allocate(allocateSize); }

		/// <summary>
		/// Clear the array and release the used memory.
		/// </summary>

		public void Release () { size = 0; buffer = null; }

		/// <summary>
		/// Add a new element to the array, returning its index.
		/// </summary>

		public int Expand ()
		{
			if (buffer == null || size == buffer.Length) AllocateMore();
			return size++;
		}

		/// <summary>
		/// Add the specified number of new elements to the array, increasing in the process.
		/// </summary>

		public int Expand (int count)
		{
			var offset = size;
			var needed = size + count;
			if (buffer == null || size + needed >= buffer.Length) Allocate(size + needed);
			size += count;
			return offset;
		}

		/// <summary>
		/// Add the specified item to the end of the list.
		/// </summary>

		public void Add (T item)
		{
			if (buffer == null || size == buffer.Length) AllocateMore();
			buffer[size] = item;
			++size;
		}

		/// <summary>
		/// Add the specified items to the array.
		/// </summary>

		public void Add (T[] items, int offset, int count)
		{
			var needed = size + count;
			if (buffer == null || size + needed >= buffer.Length) Allocate(size + needed);
			System.Array.Copy(items, offset, buffer, size, count);
			size += count;
		}

		/// <summary>
		/// Add the specified item to the end of the list.
		/// </summary>

		public void Add (object item)
		{
			if (buffer == null || size == buffer.Length) AllocateMore();
			buffer[size] = (T)item;
			++size;
		}

		/// <summary>
		/// Add the specified item to the end of the list.
		/// </summary>

		public bool Add (T item, bool unique)
		{
			if (unique && Contains(item)) return false;
			if (buffer == null || size == buffer.Length) AllocateMore();
			buffer[size] = item;
			++size;
			return true;
		}

		/// <summary>
		/// Add the specified item to the end of the list.
		/// </summary>

		public bool Add (object item, bool unique)
		{
			if (unique && Contains((T)item)) return false;
			if (buffer == null || size == buffer.Length) AllocateMore();
			buffer[size] = (T)item;
			++size;
			return true;
		}

		/// <summary>
		/// Insert an item at the specified index, pushing the entries back.
		/// </summary>

		public void Insert (int index, T item)
		{
			if (buffer == null || size == buffer.Length) AllocateMore();

			if (index > -1 && index < size)
			{
				for (int i = size; i > index; --i) buffer[i] = buffer[i - 1];
				buffer[index] = item;
				++size;
			}
			else Add(item);
		}

		/// <summary>
		/// Returns 'true' if the specified item is within the list.
		/// </summary>

		public bool Contains (T item)
		{
			if (buffer == null) return false;
			for (int i = 0; i < size; ++i) if (buffer[i] != null && buffer[i].Equals(item)) return true;
			return false;
		}

		/// <summary>
		/// Return the index of the specified item.
		/// </summary>

		public int IndexOf (T item)
		{
			if (buffer == null) return -1;
			for (int i = 0; i < size; ++i) if (buffer[i] != null && buffer[i].Equals(item)) return i;
			return -1;
		}

		/// <summary>
		/// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
		/// </summary>

		public bool Remove (T item)
		{
			if (buffer != null)
			{
				var comp = System.Collections.Generic.EqualityComparer<T>.Default;

				for (int i = 0; i < size; ++i)
				{
					if (comp.Equals(buffer[i], item))
					{
						if (i + 1 < size) System.Array.Copy(buffer, i + 1, buffer, i, size - i - 1);
						--size;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Remove an item at the specified index.
		/// </summary>

		public void RemoveAt (int index)
		{
			if (buffer != null && index > -1 && index < size)
			{
				if (index + 1 < size) System.Array.Copy(buffer, index + 1, buffer, index, size - index - 1);
				--size;
			}
		}

		/// <summary>
		/// Remove the specified range of items.
		/// </summary>

		public void RemoveRange (int index, int count)
		{
			var end = index + count;

			if (end > size)
			{
				end = size;
				count = end - index;
			}

			if (buffer != null && index > -1 && index < size && index < end)
			{
				if (end + 1 < size) System.Array.Copy(buffer, end, buffer, index, size - end);
				size -= count;
			}
		}

		/// <summary>
		/// Remove an item from the end.
		/// </summary>

		public T Pop ()
		{
			if (buffer != null && size != 0)
			{
				T val = buffer[--size];
				buffer[size] = default(T);
				return val;
			}
			return default(T);
		}

		/// <summary>
		/// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
		/// </summary>

		public T[] ToArray () { Trim(); return buffer; }

		/// <summary>
		/// Comparison function should return -1 if left is less than right, 1 if left is greater than right, and 0 if they match.
		/// </summary>

		public delegate int CompareFunc (T left, T right);
		public delegate int CompareFunc2 (ref T left, ref T right);

		/// <summary>
		/// List.Sort equivalent.
		/// </summary>

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void Sort (CompareFunc comparer)
		{
			int start = 0;
			int max = size - 1;
			bool changed = true;

			while (changed)
			{
				changed = false;

				for (int i = start; i < max; ++i)
				{
					// Compare the two values
					if (comparer(buffer[i], buffer[i + 1]) > 0)
					{
						// Swap the values
						T temp = buffer[i];
						buffer[i] = buffer[i + 1];
						buffer[i + 1] = temp;
						changed = true;
					}
					else if (!changed)
					{
						// Nothing has changed -- we can start here next time
						start = (i == 0) ? 0 : i - 1;
					}
				}
			}
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void Sort (CompareFunc2 comparer)
		{
			int start = 0;
			int max = size - 1;
			bool changed = true;

			while (changed)
			{
				changed = false;

				for (int i = start; i < max; ++i)
				{
					// Compare the two values
					if (comparer(ref buffer[i], ref buffer[i + 1]) > 0)
					{
						// Swap the values
						T temp = buffer[i];
						buffer[i] = buffer[i + 1];
						buffer[i + 1] = temp;
						changed = true;
					}
					else if (!changed)
					{
						// Nothing has changed -- we can start here next time
						start = (i == 0) ? 0 : i - 1;
					}
				}
			}
		}
	}
}
