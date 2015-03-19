//
// - BufferAllocator.cs
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
using System.Threading.Tasks;
using Open.Tcp;

namespace Open.HttpProxy.BufferManager
{
    public class BufferAllocator : IBufferAllocator
    {
        public const int BlockSize = 8 * 1024; 
        private readonly BuddyBufferAllocator _allocator;
        private readonly byte[] _buffer;
        private readonly AsyncManualResetEvent _event = new AsyncManualResetEvent();

        public BufferAllocator(byte[] buffer)
        {
            _buffer = buffer;
            _allocator = BuddyBufferAllocator.Create(SizeToBlocks(buffer.Length));
        }

        public async Task<ArraySegment<byte>> AllocateAsync(int size)
        {
            var offset = -1;
            var blocks = SizeToBlocks(size);
            _event.Reset();
            while((offset = _allocator.Allocate(blocks))==-1)
            {
                await _event.WaitAsync();
            }

            return new ArraySegment<byte>(_buffer, offset * BlockSize, size);
        }

        public void Free(ArraySegment<byte> _buffer)
        {
            lock (_allocator)
            {
                _allocator.Free(_buffer.Offset / BlockSize);
                _event.Set();
            }
        }

        private int AllocateInternal(int blocks)
        {
            lock (_allocator)
            {
                return _allocator.Allocate(blocks);
            }
        }

        private static int SizeToBlocks(int size)
        {
            return (int) Math.Ceiling((decimal) size/BlockSize);
        }
    }
}