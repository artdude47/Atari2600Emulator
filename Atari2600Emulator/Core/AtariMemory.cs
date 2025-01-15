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
        public byte[] VideoBuffer { get; private set; }

        public AtariMemory(byte[] romData)
        {
            //Initialize memory regions
            RAM = new byte[128];
            TIA = new byte[128];
            ROM = new byte[4096];
            VideoBuffer = new byte[160 * 192];
            LoadROM(romData);
        }

        public void LoadROM(byte[] romData)
        {
            Array.Clear(ROM, 0, ROM.Length);

            int lengthToCopy = Math.Min(romData.Length, ROM.Length);
            Array.Copy(romData, 0, ROM, 0, lengthToCopy);
        }

        public byte ReadByte(ushort address)
        {
            // Mask off top 3 bits (6507 only has 13 address lines)
            ushort effAddr = (ushort)(address & 0x1FFF);

            if (effAddr < 0x0080)
            {
                // TIA
                return TIA[effAddr];
            }
            else if (effAddr < 0x0100)
            {
                // RAM
                return RAM[effAddr - 0x0080];
            }
            else if (effAddr < 0x0200)
            {
                // Another RAM mirror, etc.
                return RAM[(effAddr - 0x0080) % 128];
            }
            else if (effAddr >= 0x1000 && effAddr <= 0x1FFF)
            {
                // 4K Cartridge region
                ushort romOffset = (ushort)(effAddr - 0x1000);
                return ROM[romOffset];
            }
            else
            {
                // Possibly unused or mirrored to something else
                return 0xFF;
            }
        }

        public ushort ReadWord(ushort address)
        {
            //Read low byte
            byte low = ReadByte(address);

            //Read high byte
            byte high = ReadByte((ushort)(address + 1));

            // Combine low & high and return 16-bit value
            return (ushort)(low | (high << 8));
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address <= 0x007F)
            {
                // TIA
                TIA[address] = value;
                // Possibly handle specific registers here...
            }
            else if (address >= 0x0080 && address <= 0x01FF)
            {
                // Mirror addresses 0x0080–0x01FF into RAM
                RAM[(address - 0x0080) % 128] = value;
            }
            else if (address >= 0x1000 && address <= 0x107F)
            {
                // Another RAM mirror region
                RAM[(address - 0x1000) % 128] = value;
            }
            else if (address >= 0xF000 && address <= 0xFFFF)
            {
                // Usually ROM is read-only, but bank-switch carts might need special handling.
                // For a simple 4K cart, do nothing or throw if you like:
                // throw new InvalidOperationException($"Can't write to ROM at {address:X4}");
            }
            else if (address >= 0xD000 && address <= 0xFFFF)
            {
                // Usually no-op (ROM is read-only), or handle bankswitch logic if needed.
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(address),
                    $"Address {address:X4} is out of range.");
            }
        }


        public void ClearVideoBuffer()
        {
            Array.Clear(VideoBuffer, 0, VideoBuffer.Length);
        }

    }
}
