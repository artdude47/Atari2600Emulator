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
