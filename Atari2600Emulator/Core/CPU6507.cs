using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class CPU6507
    {
        private AtariMemory _memory;

        //Registers
        private byte A, X, Y;   //Accumulator, Index Registers
        private ushort PC;      //Program counter
        private byte SP;        //Stack Pointer
        private byte P;         //Processor Status

        // Flags
        private const byte FLAG_CARRY = 0x01;
        private const byte FLAG_ZERO = 0x02;
        private const byte FLAG_INTERRUPT = 0x04;
        private const byte FLAG_DECIMAL = 0x08;
        private const byte FLAG_BREAK = 0x10;
        private const byte FLAG_OVERFLOW = 0x40;
        private const byte FLAG_NEGATIVE = 0x80;

        public CPU6507(AtariMemory memory)
        {
            _memory = memory;
            Reset();
        }

        public void Reset()
        {
            PC = (ushort)(_memory.ReadByte(0xFFFC) | (_memory.ReadByte(0xFFFD) << 8));
            SP = 0xFF;
            P = 0x34;
        }

        public void Step()
        {
            byte opcode = _memory.ReadByte(PC);
            PC++;

            DecodeAndExecuteOpcode(opcode);
        }

        private void DecodeAndExecuteOpcode(byte opcode)
        {
            switch (opcode)
            {
                case 0xEA: //NOP
                    break;
                default:
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented");
            }
        }
    }
}
