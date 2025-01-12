using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class AtariMemory
    {
        private byte[] RAM;     //128 bytes of RIOT RAM
        private byte[] ROM;     //ROM data
        private byte[] TIA;     //TIA Registers

        public AtariMemory(byte[] romData)
        {
            //Initialize memory regions
            RAM = new byte[128];
            TIA = new byte[128];
            ROM = new byte[4096];
            LoadROM(romData);
        }

        public void LoadROM(byte[] romData)
        {
            Array.Clear(ROM, 0, ROM.Length);
            Array.Copy(romData, 0, ROM, 0, Math.Min(romData.Length, ROM.Length));

            ushort startAddress = 0xF000;
            ROM[0x0FFC] = (byte)(startAddress & 0xFF);
            ROM[0x0FFD] = (byte)((startAddress >> 8) & 0xFF);
        }

        public byte ReadByte(ushort address)
        {
            if (address <= 0x007F) return TIA[address];
            if (address >= 0x0080 && address <= 0x00FF) return RAM[address - 0x0080];
            if (address >= 0x1000 && address <= 0x107F) return RAM[(address - 0x1000) % 128];
            if (address >= 0xF000 && address <= 0xFFFF) return ROM[address - 0xF000];
            throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X4} is out of bounds");
        }

        public void WriteByte(ushort address, byte value)
        {
            // (Implementation unchanged)
            if (address <= 0x007F) TIA[address] = value;
            else if (address >= 0x0080 && address <= 0x00FF) RAM[address - 0x0080] = value;
            else if (address >= 0x1000 && address <= 0x107F) RAM[(address - 0x1000) % 128] = value;
            else throw new ArgumentOutOfRangeException(nameof(address), $"Address {address:X4} is out of range.");
        }
    }
}
