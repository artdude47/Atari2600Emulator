using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class AtariMemory
    {
        //---------------------------------------------------------------------------------------
        //Memory Regions
        //---------------------------------------------------------------------------------------
        private TIA _tia; // 32 bytes: 0x0000 - 0x001F
        private RIOT _riot; // 96 bytes; 0x0020 - 0x007F
        private byte[] _rom; // 4KB: 0x1000 - 0x1FFF 

        //---------------------------------------------------------------------------------------
        //Events
        //---------------------------------------------------------------------------------------
        //Event triggered when a TIA or RIOT register is written to, provides address and value written
        public event Action<ushort, byte> OnTIARegisterWrite;
        public event Action<ushort, byte> OnRIOTRegisterWrite;

        //---------------------------------------------------------------------------------------
        //Constructor
        //---------------------------------------------------------------------------------------
        public AtariMemory()
        {
            _tia = new TIA();
            _riot = new RIOT();
            _rom = new byte[0x1000];

            _tia.OnRegisterWrite += HandleTIARegisterWrite;
            _riot.OnRegisterWrite += HandleRIOTRegisterWrite;
        }

        //---------------------------------------------------------------------------------------
        //Load ROM Data
        //---------------------------------------------------------------------------------------
        public void LoadROM(byte[] romData)
        {
            if (romData == null)
                throw new ArgumentNullException(nameof(romData), "ROM data cannot be null.");

            Array.Clear(_rom, 0, _rom.Length);

            int lengthToCopy = Math.Min(romData.Length, _rom.Length);
            Array.Copy(romData, 0, _rom, 0, lengthToCopy);
        }

        //---------------------------------------------------------------------------------------
        //Read Operations
        //---------------------------------------------------------------------------------------
        public byte ReadByte(ushort address)
        {
            // Mask off top 3 bits (6507 has 13 address lines: 0x0000-0x1FFF)
            ushort effAddr = (ushort)(address & 0x1FFF);

            if (effAddr < 0x0020)
            {
                // TIA Registers: 0x0000-0x001F
                return _tia.ReadRegister(effAddr);
            }
            else if (effAddr < 0x0080)
            {
                // RIOT Registers and RAM: 0x0020-0x007F
                return _riot.ReadRegister((ushort)(effAddr - 0x0020));
            }
            else if (effAddr >= 0x1000 && effAddr <= 0x1FFF)
            {
                // Cartridge ROM: 0x1000-0x1FFF
                ushort romOffset = (ushort)(effAddr - 0x1000);
                if (romOffset < _rom.Length)
                    return _rom[romOffset];
                else
                    return 0xFF; // Unmapped ROM area
            }
            else
            {
                // Unmapped Addresses: 0x0080-0x0FFF and 0x2000-0x1FFF (wrapped)
                // Typically return 0xFF or handle as needed
                return 0xFF;
            }
        }

        public ushort ReadWord(ushort address)
        {
            byte low = ReadByte(address);
            byte high = ReadByte((ushort)(address + 1));
            return (ushort)(low | (high << 8));
        }

        //---------------------------------------------------------------------------------------
        //Write Operations
        //---------------------------------------------------------------------------------------
        public void WriteByte(ushort address, byte value)
        {
            ushort effAddr = (ushort)(address & 0x1FFF);

            if (effAddr < 0x0020)
            {
                // TIA Registers: 0x0000-0x001F
                _tia.WriteRegister(effAddr, value);
                // Event handling is managed within TIA class
            }
            else if (effAddr < 0x0080)
            {
                // RIOT Registers and RAM: 0x0020-0x007F
                _riot.WriteRegister((ushort)(effAddr - 0x0020), value);
                // Event handling is managed within RIOT class
            }
            else if (effAddr >= 0x1000 && effAddr <= 0x1FFF)
            {
                // Cartridge ROM: 0x1000-0x1FFF is read-only in standard cartridges
                // Implement bank switching here if using mappers or special cartridges
                // For standard 4KB ROMs, writes are ignored or can throw an exception

                // Example: Ignoring writes
                // Do nothing

                // Alternatively, throw an exception
                // throw new InvalidOperationException($"Cannot write to ROM at address {effAddr:X4}");
            }
            else
            {
                // Unmapped Addresses: 0x0080-0x0FFF and 0x2000-0x1FFF (wrapped)
                // Typically, writes are ignored or can log a warning
                // Do nothing
            }
        }

        //---------------------------------------------------------------------------------------
        //Event Handlers
        //---------------------------------------------------------------------------------------
        private void HandleTIARegisterWrite(ushort address, byte value)
        {
            // Raise event to notify subscribers (e.g., Video class)
            OnTIARegisterWrite?.Invoke(address, value);
        }

        private void HandleRIOTRegisterWrite(ushort address, byte value)
        {
            // Raise event to notify subscribers (e.g., Input class)
            OnRIOTRegisterWrite?.Invoke(address, value);
        }

        public void Reset()
        {
            // Clear TIA and RIOT registers
            _tia.Reset();
            _riot.Reset();

            // Clear ROM if needed (optional)
            Array.Clear(_rom, 0, _rom.Length);
        }

        public void ClearVideoBuffer()
        {
            _tia.ClearVideoBuffer();
        }
    }
}
