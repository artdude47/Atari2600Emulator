using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class CPU6507
    {
        // ---------------------------------------------------------------------------------------------------------------
        //Registers
        // ---------------------------------------------------------------------------------------------------------------

        private byte A, X, Y;   //Accumulator, Index Registers
        private ushort PC;      //Program counter
        private byte SP;        //Stack Pointer
        private byte P;         //Processor Status

        // ---------------------------------------------------------------------------------------------------------------
        // Flags in the Processor Status Register (P)
        // ---------------------------------------------------------------------------------------------------------------
        private const byte FLAG_CARRY = 0x01;
        private const byte FLAG_ZERO = 0x02;
        private const byte FLAG_INTERRUPT = 0x04;
        private const byte FLAG_DECIMAL = 0x08;
        private const byte FLAG_BREAK = 0x10;
        // bit 5 is unused in 6502
        private const byte FLAG_OVERFLOW = 0x40;
        private const byte FLAG_NEGATIVE = 0x80;

        // Reference to memory
        private AtariMemory _memory;

        public CPU6507(AtariMemory memory)
        {
            _memory = memory;
        }

        // ---------------------------------------------------------------------------------------------------------------
        //The Step method fetches the opcodes, decodes them, executes them,
        //then returns # of cpu cycles used
        // ---------------------------------------------------------------------------------------------------------------
        public int Step()
        {
            byte opcode = _memory.ReadByte(PC);
            PC++;

            return DecodeAndExecuteOpcode(opcode);
        }


        // ---------------------------------------------------------------------------------------------------------------
        // Executes the opcodes then return # of cpu cycles
        // ---------------------------------------------------------------------------------------------------------------
        private int DecodeAndExecuteOpcode(byte opcode)
        {
            byte value;
            bool pageBoundaryCrossed = false;

            switch (opcode)
            {
                case 0xEA: //NOP
                    return 0;

                #region ADC Opcodes

                case 0x69: //ADC Immediate
                    value = ReadImmediate();
                    ADC(value);
                    return 2; // 2 cycles

                case 0x65: // ADC Zero Page
                    value = ReadZeroPage();
                    ADC(value);
                    return 3; // 3 cycles

                case 0x75: // ADC Zero Page, X
                    value = ReadZeroPageX();
                    ADC(value);
                    return 4; // 4 cycles

                case 0x6D: // ADC Absolute
                    value = ReadAbsolute();
                    ADC(value);
                    return 4; // 4 cycles

                case 0x7D: // ADC Absolute, X (4 cycles + 1 if page crossed)
                    value = ReadAbsoluteX(out pageBoundaryCrossed);
                    ADC(value);
                    // if page bondary is crossed, add 1 cycle
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0x79: // ADC Absolute, Y
                    value = ReadAbsoluteY(out pageBoundaryCrossed);
                    ADC(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0x61: //ADC (Indirect, X)
                    value = ReadIndirectX();
                    ADC(value);
                    return 6; // 6 cycles

                case 0x71: //ADC (Indirect, Y), 5 cycles + 1 if page crossed
                    value = ReadIndirectY(out pageBoundaryCrossed);
                    ADC(value);
                    return 5 + (pageBoundaryCrossed ? 1 : 0);

                #endregion

                #region AND Opcodes

                case 0x29: // AND Immediate
                    value = ReadImmediate();
                    AND(value);
                    return 2;

                case 0x25: // AND Zero Page
                    value = ReadZeroPage();
                    AND(value);
                    return 3;

                case 0x35: // AND Zero Page, X
                    value = ReadZeroPageX();
                    AND(value);
                    return 4;

                case 0x2D: //AND Absolute
                    value = ReadAbsolute();
                    AND(value);
                    return 4;

                case 0x3D: //AND Absolute, X
                    value = ReadAbsoluteX(out pageBoundaryCrossed);
                    AND(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0x39: //AND Absolute, Y
                    value = ReadAbsoluteY(out pageBoundaryCrossed);
                    AND(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0x21: //AND (Indirect, X)
                    value = ReadIndirectX();
                    AND(value);
                    return 6;

                case 0x31: // AND (Indirect), Y
                    value = ReadIndirectY(out pageBoundaryCrossed);
                    AND(value);
                    return 5 + (pageBoundaryCrossed ? 1 : 0);

                #endregion

                #region ASL Opcodes

                case 0x0A: //ASL A (Accumulator)
                    ASL_Accumulator();
                    return 2;

                case 0x06: // ASL Zero Page
                    ASL_Memory(GetASLAddrZeroPage());
                    return 5;

                case 0x16: // ASL Zero Page, X
                    ASL_Memory(GetASLAddrZeroPageX());
                    return 6;

                case 0x0E: // ASL Absolute
                    ASL_Memory(ASLAbsolute());
                    return 6;

                case 0x1E: // ASL Absolute, X
                    ASL_Memory(ASLAbsoluteX());
                    return 7;

                #endregion

                #region BIT Opcodes

                case 0x24: //BIT Zero Page
                    BIT(BITZeroPage());
                    return 3;

                case 0x2C: //Bit Absolute
                    BIT(BITAbsolute());
                    return 4;

                #endregion

                #region Branch Opcodes

                case 0x10: // BPL (Branch if negative = 0)
                    return BranchIf((P & FLAG_NEGATIVE) == 0);

                case 0x30: // BMI (Branch if negative = 1)
                    return BranchIf((P & FLAG_NEGATIVE) != 0);

                case 0x50: // BVC (Branch if overflow = 0)
                    return BranchIf((P & FLAG_OVERFLOW) == 0);

                case 0x70: // BVS (Branch if oferlow = 1)
                    return BranchIf((P & FLAG_OVERFLOW) != 0);

                case 0x90: // BCC (Branch if carry = 0)
                    return BranchIf((P & FLAG_CARRY) == 0);

                case 0xB0: // BCS (Branch if carry = 1)
                    return BranchIf((P & FLAG_CARRY) != 0);

                case 0xD0: // BNE (Branch if zero = 0)
                    return BranchIf((P & FLAG_ZERO) == 0);

                case 0xF0: // BEQ (Branch if zero = 1)
                    return BranchIf((P & FLAG_ZERO) != 0);

                #endregion

                #region Break Opcode

                case 0x00: // BRK
                    return BRK_Instruction();

                #endregion

                #region CMP Opcodes

                case 0xC9: //CMP Immediate
                    value = ReadImmediate();
                    CMP(value);
                    return 2;

                case 0xC5: // CMP Zero Page
                    value = ReadZeroPage();
                    CMP(value);
                    return 3;

                case 0xD5: // CMP Zero Page, X
                    value = ReadZeroPageX();
                    CMP(value);
                    return 4;

                case 0xCD: // CMP Absolute
                    value = ReadAbsolute();
                    CMP(value);
                    return 4;

                case 0xDD: // CMP Absolute, X
                    value = ReadAbsoluteX(out pageBoundaryCrossed);
                    CMP(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xD9: // CMP Absolute, Y
                    value = ReadAbsoluteY(out pageBoundaryCrossed);
                    CMP(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xC1: // CMP (Indirect, X)
                    value = ReadIndirectX();
                    CMP(value);
                    return 6;

                case 0xD1: // CMP (Indirect), Y
                    value = ReadIndirectY(out pageBoundaryCrossed);
                    CMP(value);
                    return 5 + (pageBoundaryCrossed ? 1 : 0);

                #endregion

                default:
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented");
            }
        }

        #region ADC Logic

        // ---------------------------------------------------------------------------------------------------------------
        // ADC logic
        // Does the actual addition, sets the C, V, Z, N flags, and stores the result in A
        // ---------------------------------------------------------------------------------------------------------------
        private void ADC(byte value)
        {
            // Grab the carry bit (0 or 1)
            int carryIn = (P & FLAG_CARRY) != 0 ? 1 : 0;

            // Perform unsigned 8-bit addition
            int sum = A + value + carryIn;

            // Set / clear carry flag if > 0xFF
            if (sum > 0xFF)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            //Overflow check (for signed 8-bit)
            if (((A ^ value) & 0x80) == 0 && ((A ^ sum) & 0x80) != 0)
                P |= FLAG_OVERFLOW;
            else
                P &= unchecked((byte)~FLAG_OVERFLOW);

            //Store the low 8 bits back into A
            A = (byte)(sum & 0xFF);

            //Update Zero and Negative flags
            SetZeroAndNegativeFlags(A);
        }

        #endregion

        #region AND Logic

        private void AND(byte value)
        {
            //Perform bitwisee AND with Accumulator
            A = (byte)(A & value);
            SetZeroAndNegativeFlags(A);
        }

        #endregion

        #region ASL Logic

        private void ASL_Accumulator()
        {
            // Carry = old bit 7
            byte oldBit7 = (byte)(A & 0x80);
            if (oldBit7 != 0)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            //Shift left
            A <<= 1;

            //Set zero and negative
            SetZeroAndNegativeFlags(A);
        }

        private void ASL_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);

            //Carry = old bit 7
            byte oldBit7 = (byte)(value & 0x80);
            if (oldBit7 != 0)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            value <<= 1;

            _memory.WriteByte(address, value);

            SetZeroAndNegativeFlags(value);
        }

        #endregion

        #region BIT Logic

        private void BIT(byte value)
        {
            //Test bits
            byte temp = (byte)(A & value);

            //Z set if (A & value) == 0
            if (temp == 0)
                P |= FLAG_ZERO;
            else
                P &= unchecked((byte)~FLAG_ZERO);

            // N from bit 7 of value
            if ((value & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= unchecked((byte)~FLAG_NEGATIVE);

            // V from bit 6 of value
            if ((value & 0x40) != 0)
                P |= FLAG_OVERFLOW;
            else
                P &= unchecked((byte)~FLAG_OVERFLOW);
        }

        #endregion

        #region Branch Logic

        private int BranchIf(bool condition)
        {
            //Always fetch the signed offset
            sbyte offset = (sbyte)_memory.ReadByte(PC);
            PC++;

            //Base cost of 2 cycles
            int cycles = 2;

            if (condition)
            {
                cycles++;

                ushort oldPC = PC;
                ushort newPC = (ushort)(PC + offset);

                PC = newPC;

                // If we crossed page boundary, add another cycle
                if ((oldPC & 0xFF00) != (newPC & 0xFF00))
                    cycles++;
            }

            return cycles;

        }

        #endregion

        #region Break Logic

        private int BRK_Instruction()
        {
            // BRK is effectively software interrupt
            PC++;

            // Set the break flag in P
            P |= FLAG_BREAK;

            // Push PC High, then PC low
            PushByte((byte)((PC >> 8) & 0xFF));
            PushByte((byte)(PC & 0xFF));

            //Push status, but with break flag set and set I
            PushByte((byte)(P | 0x10));

            P |= FLAG_INTERRUPT;

            // Fetch new PC from IRQ/BRK vector ($FFFE/$FFFF)
            ushort low = _memory.ReadByte(0xFFFE);
            ushort high = _memory.ReadByte(0xFFFF);
            PC = (ushort)(low | (high << 8));

            return 7; // 7 pc cycles
        }

        private void PushByte(byte value)
        {
            _memory.WriteByte((ushort)(0x0100 + SP), value);
            SP--;
        }

        #endregion

        #region CMP Logic

        private void CMP(byte value)
        {
            int temp = A - value;

            // If A >= value => set carry
            if (A >= value)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            //If result == 0 => set zero
            if ((temp & 0xFF) == 0)
                P |= FLAG_ZERO;
            else
                P &= unchecked((byte)~FLAG_ZERO);

            //Negative depends on bit 7 of the result
            byte result8 = (byte)(temp & 0xFF);
            if ((result8 & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= unchecked((byte)~FLAG_NEGATIVE);
        }

        #endregion

        #region Helper Methods

        // ---------------------------------------------------------------------------------------------------------------
        // Helper methods for Addressing Modes
        // ---------------------------------------------------------------------------------------------------------------
        private byte ReadImmediate()
        {
            //For immediate addressing, read the next byte from PC, then increment PC
            byte val = _memory.ReadByte(PC);
            PC++;
            return val;
        }

        private byte ReadZeroPage()
        {
            //Next byte is a zero-page address
            byte addr = _memory.ReadByte(PC);
            PC++;
            return _memory.ReadByte(addr);
        }

        private byte ReadZeroPageX()
        {
            byte addr = _memory.ReadByte(PC);
            PC++;
            // wrap around zero page
            addr = (byte)(addr + X);
            return _memory.ReadByte(addr);
        }

        private byte ReadAbsolute()
        {
            //16-bit little-endian address
            ushort addr = _memory.ReadWord(PC);
            PC += 2;
            return _memory.ReadByte(addr);
        }

        private byte ReadAbsoluteX(out bool pageBoundaryCrossed)
        {
            ushort baseAddr = _memory.ReadWord(PC);
            PC += 2;
            ushort finalAddr = (ushort)(baseAddr + X);

            // If the high byte changes, page boundary has been crossed
            pageBoundaryCrossed = ((baseAddr & 0xFF00) != (finalAddr & 0xFF00));

            return _memory.ReadByte(finalAddr);
        }

        private byte ReadAbsoluteY(out bool pageBoundaryCrossed)
        {
            ushort baseAddr = _memory.ReadWord(PC);
            PC += 2;
            ushort finalAddr = (ushort)(baseAddr + Y);

            pageBoundaryCrossed = ((baseAddr & 0xFF00) != (finalAddr & 0xFF00));

            return _memory.ReadByte(finalAddr);
        }

        private byte ReadIndirectX()
        {
            //Fetch zero-page pointer from next byte, add X (wrap in zero page), read 16-bit address from pointer, read final data
            byte zp = _memory.ReadByte(PC);
            PC++;
            byte ptr = (byte)(zp + X);

            ushort addr = (ushort)(_memory.ReadByte(ptr) | (_memory.ReadByte((byte)(ptr + 1)) << 8));
            return _memory.ReadByte(addr);
        }

        private byte ReadIndirectY(out bool pageBoundaryCrossed)
        {
            //Fetch zero-page pointer from next byte, read 16 bit address from pointer, add Y, read final data from address
            byte zp = _memory.ReadByte(PC);
            PC++;

            ushort baseAddr = (ushort)(_memory.ReadByte(zp) | (_memory.ReadByte((byte)(zp + 1)) << 8));
            ushort finalAddr = (ushort)(baseAddr + Y);

            pageBoundaryCrossed = ((baseAddr & 0xFF00) != (finalAddr & 0xFF00));
            return _memory.ReadByte(finalAddr);
        }

        private byte GetASLAddrZeroPage()
        {
            byte addr = _memory.ReadByte(PC);
            PC++;
            return addr;
        }

        private byte GetASLAddrZeroPageX()
        {
            byte zeroPageAddr = _memory.ReadByte(PC);
            PC++;
            byte addr = (byte)(zeroPageAddr + X);
            return addr;
        }

        private ushort ASLAbsolute()
        {
            ushort addr = _memory.ReadWord(PC);
            PC += 2;
            return addr;
        }

        private ushort ASLAbsoluteX()
        {
            ushort baseAddr = _memory.ReadWord(PC);
            PC += 2;
            ushort finalAddr = (ushort)(baseAddr + X);
            return finalAddr;
        }

        private byte BITZeroPage()
        {
            byte addr = _memory.ReadByte(PC);
            PC++;
            byte value = _memory.ReadByte(addr);
            return value;
        }

        private byte BITAbsolute()
        {
            ushort addr = _memory.ReadWord(PC);
            PC += 2;
            byte value = _memory.ReadByte(addr);
            return value; 
        }

        #endregion

        #region Flag Updates

        // ---------------------------------------------------------------------------------------------------------------
        // Updating Flags
        // ---------------------------------------------------------------------------------------------------------------
        private void SetZeroAndNegativeFlags(byte value)
        {
            //Zero flag
            if (value == 0)
                P |= FLAG_ZERO;
            else
                P &= unchecked((byte)~FLAG_ZERO);

            // Negative flag
            if ((value & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= unchecked((byte)~FLAG_NEGATIVE);
        }

        #endregion
    }
}
