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
            Console.WriteLine($"Reset Vector Loaded: PC = {PC:X4}");
        }

        public void Step()
        {
            byte opcode = _memory.ReadByte(PC);
            PC++;

            Console.WriteLine($"Opcode: {opcode:X2}");

            DecodeAndExecuteOpcode(opcode);
            PrintState();
        }

        private void DecodeAndExecuteOpcode(byte opcode)
        {
            switch (opcode)
            {
                case 0xEA: //NOP
                    break;

                case 0xA9: // LDA Immediate
                    A = GetImmediateValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0xA5: // LDA Zero Page
                    A = GetZeroPageValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0xAD: // LDA Absolute
                    A = GetAbsoluteValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0xB5: //LDA Zero Page, X
                    A = GetZeroPageXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0xBD: // LDA Absolute, X
                    A = GetAbsoluteXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0xB9: //LDA Absolute, Y
                    A = GetAbsoluteYValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x85: // STA Zero Page
                    WriteZeroPage(A);
                    break;

                case 0x8D:
                    WriteAbsolute(A);
                    break;

                case 0x95: // STA Zero Page, X
                    WriteZeroPageX(A);
                    break;

                case 0x9D: // STA Absolute, X
                    WriteAbsoluteX(A);
                    break;

                case 0x99: //STA Absolute, Y
                    WriteAbsoluteY(A);
                    break;

                case 0x69: // ADC Immediate
                    AddWithCarry(GetImmediateValue());
                    break;

                case 0x65: // ADC Zero Page
                    AddWithCarry(GetZeroPageValue());
                    break;

                case 0x6D: // ADC Absolute
                    AddWithCarry(GetAbsoluteValue());
                    break;

                case 0x75: // ADC Zero Page, X
                    AddWithCarry(GetZeroPageXValue());
                    break;

                case 0x7D: // ADC Absolute, X
                    AddWithCarry(GetAbsoluteXValue());
                    break;

                case 0xF0: // BEQ
                    if ((P & FLAG_ZERO) != 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0xD0: // BNE
                    if ((P & FLAG_ZERO) == 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0xB0: // BCS
                    if ((P & FLAG_CARRY) != 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0x90: // BCC
                    if ((P & FLAG_CARRY) == 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0x30: // BMI
                    if ((P & FLAG_NEGATIVE) != 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0x10: // BPL
                    if ((P & FLAG_NEGATIVE) == 0)
                        Branch();
                    else
                        PC++;
                    break;

                case 0xE9: // SBC Immediate
                    SubtractWithCarry(GetImmediateValue());
                    break;

                case 0xE5: // SBC Zero Page
                    SubtractWithCarry(GetZeroPageValue());
                    break;

                case 0xED: // SBC Absolute
                    SubtractWithCarry(GetAbsoluteValue());
                    break;

                case 0xF5: // SBC Zero Page, X
                    SubtractWithCarry(GetZeroPageXValue());
                    break;

                case 0xFD: // SBC Absolute, X
                    SubtractWithCarry(GetAbsoluteXValue());
                    break;

                case 0xF9: // SBC Absolute, Y
                    SubtractWithCarry(GetAbsoluteYValue());
                    break;

                case 0x00: // BRK
                    //Break execution (interrupt)
                    break;

                default:
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented");
            }
        }

        public void PrintState()
        {
            Console.WriteLine($"A: {A:X2} X: {X:X2} Y: {Y:X2} PC: {PC:X4} P: {P:X2} [C:{(P & FLAG_CARRY) >> 0} Z:{(P & FLAG_ZERO) >> 1} N:{(P & FLAG_NEGATIVE) >> 7}]");
        }


        #region HELPER METHODS

        private void SetZeroAndNegativeFlags(byte value)
        {
            if (value == 0)
                P |= FLAG_ZERO;
            else
                P &= (byte)(~FLAG_ZERO & 0xFF);

            if ((value & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= (byte)(~FLAG_NEGATIVE & 0xFF);
        }

        private byte GetImmediateValue()
        {
            return _memory.ReadByte(PC++);
        }

        private byte GetZeroPageValue()
        {
            byte address = _memory.ReadByte(PC++);
            return _memory.ReadByte(address);
        }

        private byte GetAbsoluteValue()
        {
            ushort address = GetAbsoluteAddress();
            return _memory.ReadByte(address);
        }

        private ushort GetAbsoluteAddress()
        {
            //Combine two bytes from memory into a 16-bit address
            ushort address = (ushort)(_memory.ReadByte(PC) | (_memory.ReadByte((ushort)(PC + 1)) << 8));
            PC += 2;
            return address;
        }

        private byte GetZeroPageXValue()
        {
            byte address = (byte)(_memory.ReadByte(PC++) + X);
            return _memory.ReadByte(address);
        }

        private byte GetAbsoluteXValue()
        {
            ushort baseAddress = GetAbsoluteAddress();
            ushort effectiveAddress = (ushort)(baseAddress + X);
            return _memory.ReadByte(effectiveAddress);
        }

        private byte GetAbsoluteYValue()
        {
            ushort baseAddress = GetAbsoluteAddress();
            ushort effectiveAddress = (ushort)(baseAddress + Y);
            return _memory.ReadByte(effectiveAddress);
        }

        private void WriteZeroPage(byte value)
        {
            byte address = _memory.ReadByte(PC++);
            _memory.WriteByte(address, value);
        }

        private void WriteAbsolute(byte value)
        {
            ushort address = GetAbsoluteAddress();
            _memory.WriteByte(address, value);
        }

        private void WriteZeroPageX(byte value)
        {
            byte address = (byte)(_memory.ReadByte(PC++) + X);
            _memory.WriteByte(address, value);
        }

        private void WriteAbsoluteX(byte value)
        {
            ushort baseAddress = GetAbsoluteAddress();
            _memory.WriteByte((ushort)(baseAddress + X), value);
        }

        private void WriteAbsoluteY(byte value)
        {
            ushort baseAddress = GetAbsoluteAddress();
            _memory.WriteByte((ushort)(baseAddress + Y), value);
        }

        private void AddWithCarry(byte value)
        {
            int result = A + value + (P & FLAG_CARRY);

            // Set the Carry flag if there's an overflow beyond 8 bits
            if (result > 0xFF)
                P |= FLAG_CARRY;
            else
                P &= (byte)(~FLAG_CARRY & 0xFF);

            // Set the Overflow flag for signed arithmetic
            if (((A ^ value) & 0x80) == 0 && ((A ^ result) & 0x80) != 0)
                P |= FLAG_OVERFLOW;
            else
                P &= (byte)(~FLAG_OVERFLOW & 0xFF);

            A = (byte)result;

            // Update Zero and Negative flags
            SetZeroAndNegativeFlags(A);
        }

        private void Branch()
        {
            // Signed offset
            sbyte offset = (sbyte)_memory.ReadByte(PC++);
            PC = (ushort)(PC + offset);
        }

        private void SubtractWithCarry(byte value)
        {
            // Invert the value for two's complement subtraction
            value = (byte)~value;

            // Perform addition using ADC logic
            AddWithCarry(value);
        }

        #endregion
    }
}
