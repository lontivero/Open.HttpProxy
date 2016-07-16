//
// - Buffer.cs
// 
// Author:
//	 Lucas Ontivero <lucasontivero@gmail.com>
// 
// Copyright 2013 Lucas E. Ontivero
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

// <summary></summary>

using System;
using System.Collections.Generic;

namespace Open.HttpProxy.BufferManager
{
	using Utils;

	public class BufferList
	{
		private readonly List<ArraySegment<byte>> _buffers;
		private int _size;

		public BufferList()
		{
			_buffers = new List<ArraySegment<byte>> ();
			_size = 0;
		}

		public BufferList(ArraySegment<byte> buffer)
		{
			_buffers = new List<ArraySegment<byte>> { buffer };
			_size = buffer.Count;
		}

		public void Add(ArraySegment<byte> buffer)
		{
			_buffers.Add(buffer);
			_size += buffer.Count;
		}

		public void CopyTo(byte[] array)
		{
			Guard.IsGreaterOrEqualTo(array.Length, Capacity, "array too small to copy buffer");
			foreach (var buffer in _buffers)
			{
				Buffer.BlockCopy(buffer.Array, buffer.Offset, array, 0, buffer.Count);
			}
		}

		public ArraySegment<byte> this[int i] => _buffers[i];

	    public int Capacity => _size;

	    public int BufferCount => _buffers.Count;
	}
}