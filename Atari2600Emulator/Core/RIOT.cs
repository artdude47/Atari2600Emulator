using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class RIOT
    {
        // ---------------------------------------------------------------------------------------------------------------
        // RIOT Registers: 96 bytes (0x0020-0x007F)
        // ---------------------------------------------------------------------------------------------------------------
        private readonly byte[] _registers;

        // ---------------------------------------------------------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Event triggered when a RIOT register is written to.
        /// Provides the address and value written.
        /// </summary>
        public event Action<ushort, byte> OnRegisterWrite;

        // ---------------------------------------------------------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------------------------------------------------------
        public RIOT()
        {
            _registers = new byte[0x0060]; // 96 bytes
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Read RIOT Register
        // ---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Reads the value of a RIOT register.
        /// </summary>
        /// <param name="address">Address of the RIOT register (0x0000-0x005F).</param>
        /// <returns>Value of the register.</returns>
        public byte ReadRegister(ushort address)
        {
            if (address >= 0x0060)
                throw new ArgumentOutOfRangeException(nameof(address), "RIOT register address out of range.");

            return _registers[address];
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Write RIOT Register
        // ---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Writes a value to a RIOT register and triggers side effects.
        /// </summary>
        /// <param name="address">Address of the RIOT register (0x0000-0x005F).</param>
        /// <param name="value">Value to write to the register.</param>
        public void WriteRegister(ushort address, byte value)
        {
            if (address >= 0x0060)
                throw new ArgumentOutOfRangeException(nameof(address), "RIOT register address out of range.");

            _registers[address] = value;

            // Handle side effects based on register
            HandleRegisterWrite(address, value);

            // Raise event for external handlers
            OnRegisterWrite?.Invoke(address, value);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Handle Register Side Effects
        // ---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Handles side effects triggered by writing to specific RIOT registers.
        /// </summary>
        /// <param name="address">Address of the RIOT register.</param>
        /// <param name="value">Value written to the register.</param>
        private void HandleRegisterWrite(ushort address, byte value)
        {
            switch (address)
            {
                case 0x0000: // CIA1 Port A
                    // Implement GPIO Port A write logic (e.g., handling joystick input)
                    break;
                case 0x0001: // CIA1 Port B
                    // Implement GPIO Port B write logic
                    break;
                // Add cases for other RIOT registers as needed

                default:
                    break;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Reset RIOT Registers
        // ---------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Resets all RIOT registers to their default state.
        /// </summary>
        public void Reset()
        {
            Array.Clear(_registers, 0, _registers.Length);
        }
    }
}
