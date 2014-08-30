//
// - Buffer.cs
// 
// Author:
//     Lucas Ontivero <lucasontivero@gmail.com>
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
using System.IO;
using System.Linq;
using Open.Tcp.Utils;

namespace Open.Tcp.BufferManager
{
    public class BufferList
    {
        private readonly List<ArraySegment<byte>> _listOf_buffers;
        private int _size;

        public BufferList()
        {
            _listOf_buffers = new List<ArraySegment<byte>> ();
            _size = 0;
        }

        public BufferList(ArraySegment<byte> _buffer)
        {
            _listOf_buffers = new List<ArraySegment<byte>> { _buffer };
            _size = _buffer.Count;
        }

        public void Add(ArraySegment<byte> _buffer)
        {
            _listOf_buffers.Add(_buffer);
            _size += _buffer.Count;
        }

        public void CopyTo(byte[] array)
        {
            Guard.IsGreaterOrEqualTo(array.Length, Capacity, "array too small to copy buffer");
            foreach (var _buffer in _listOf_buffers)
            {
                Buffer.BlockCopy(_buffer.Array, _buffer.Offset, array, 0, _buffer.Count);
            }
        }

        public ArraySegment<byte> this[int i]
        {
            get { return _listOf_buffers[i]; }
        } 
        
        public int Capacity
        {
            get { return _size; }
        }

        public int BufferCount
        {
            get { return _listOf_buffers.Count; }
        }

    }
}