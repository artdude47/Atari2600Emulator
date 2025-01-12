using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class AtariMemory
    {
        private byte[] _memory;

        public AtariMemory(int size = 0x10000) // 64KB
        {
            _memory = new byte[size];
        }

        public byte ReadByte(ushort address)
        {
            return _memory[address];
        }

        public void WriteByte(ushort address, byte value)
        {
            _memory[address] = value; 
        }
    }
}
