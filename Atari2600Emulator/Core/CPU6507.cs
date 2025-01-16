using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
            Reset();
        }

        public void Reset()
        {
            A = 0;
            X = 0;
            Y = 0;
            SP = 0xFF;
            P = 0x20;

            byte low = _memory.ReadByte(0xFFFC);
            byte high = _memory.ReadByte(0xFFFD);
            PC = (ushort)(low | (high << 8));
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

                #region CPX Opcodes

                case 0xE0: // CPX Immediate
                    value = ReadImmediate();
                    CPX(value);
                    return 2;

                case 0xE4: // CPX Zero Page
                    value = ReadZeroPage();
                    CPX(value);
                    return 3;

                case 0xEC: // CPX Absolute
                    value = ReadAbsolute();
                    CPX(value);
                    return 4;

                #endregion

                #region CPY Opcodes

                case 0xC0: //CPY Immediate
                    value = ReadImmediate();
                    CPY(value);
                    return 2;

                case 0xC4: // CPY Zero Page
                    value = ReadZeroPage();
                    CPY(value);
                    return 3;

                case 0xCC: //CPY Absolute
                    value = ReadAbsolute();
                    CPY(value);
                    return 4;

                #endregion

                #region DEC Opcodes

                case 0xC6: // DEC Zero Page
                    DEC_Memory(DECZeroPageAddr());
                    return 5;

                case 0xD6: // DEC Zero Page, X
                    DEC_Memory(GetZeroPageXAddress());
                    return 6;

                case 0xCE: //DEC Absolute
                    DEC_Memory(DECAbsolute());
                    return 6;

                case 0xDE: //DEC Absolute, X
                    DEC_Memory(DECAbsoluteX());
                    return 7;

                #endregion

                #region EOR Opcodes

                //TODO: Separate into helper methods

                // EOR Immediate
                case 0x49:
                    {
                        value = ReadImmediate();
                        EOR(value);
                        return 2;
                    }

                // EOR Zero Page
                case 0x45:
                    {
                        value = ReadZeroPage();
                        EOR(value);
                        return 3;
                    }

                // EOR Zero Page,X
                case 0x55:
                    {
                        value = ReadZeroPageX();
                        EOR(value);
                        return 4;
                    }

                // EOR Absolute
                case 0x4D:
                    {
                        value = ReadAbsolute();
                        EOR(value);
                        return 4;
                    }

                // EOR Absolute,X
                case 0x5D:
                    {
                        value = ReadAbsoluteX(out pageBoundaryCrossed);
                        EOR(value);
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }

                // EOR Absolute,Y
                case 0x59:
                    {
                        value = ReadAbsoluteY(out pageBoundaryCrossed);
                        EOR(value);
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }

                // EOR (Indirect,X)
                case 0x41:
                    {
                        value = ReadIndirectX();
                        EOR(value);
                        return 6;
                    }

                // EOR (Indirect),Y
                case 0x51:
                    {
                        value = ReadIndirectY(out pageBoundaryCrossed);
                        EOR(value);
                        return 5 + (pageBoundaryCrossed ? 1 : 0);
                    }


                #endregion

                #region Flag (Processor Status) Opcodes

                case 0x18: // CLC
                    {
                        P &= unchecked((byte)~FLAG_CARRY);
                        return 2;
                    }

                case 0x38: // SEC
                    {
                        P |= FLAG_CARRY;
                        return 2;
                    }

                case 0x58: // CLI
                    {
                        P &= unchecked((byte)~FLAG_INTERRUPT);
                        return 2;
                    }

                case 0x78: // SEI
                    {
                        P |= FLAG_INTERRUPT;
                        return 2;
                    }

                case 0xB8: // CLV
                    {
                        P &= unchecked((byte)~FLAG_OVERFLOW);
                        return 2;
                    }

                case 0xD8: // CLD
                    {
                        P &= unchecked((byte)~FLAG_DECIMAL);
                        return 2;
                    }

                case 0xF8: // SED
                    {
                        P |= FLAG_DECIMAL;
                        return 2;
                    }

                #endregion

                #region INC Opcodes

                case 0xE6: // INC Zero Page
                    INC_Memory(ReadZeroPage());
                    return 5;

                case 0xF6: // INC Zero Page, X
                    INC_Memory(GetZeroPageXAddress());
                    return 6;

                case 0xEE: //INC Absolute
                    INC_Memory(DECAbsolute());
                    return 6;

                case 0xFE: // INC Absolute, X
                    INC_Memory(DECAbsoluteX());
                    return 7;

                #endregion

                #region JMP Opcodes
                //TODO: Need helper methods 

                case 0x4C: //JMP Absolute
                    ushort addr = _memory.ReadWord(PC);
                    PC = addr;
                    return 3;

                case 0x6C: // READ Indirect
                    ushort pointerAddr = _memory.ReadWord(PC);
                    ushort page = (ushort)(pointerAddr & 0xFF00);
                    byte low = _memory.ReadByte(pointerAddr);
                    byte high;

                    if ((pointerAddr & 0x00FF) == 0xFF)
                    {
                        high = _memory.ReadByte((ushort)(page));
                    }
                    else
                    {
                        high = _memory.ReadByte((ushort)(pointerAddr + 1));
                    }

                    ushort targetAddr = (ushort)(low | (high << 8));
                    PC = targetAddr;
                    return 5;

                #endregion

                #region JSR Opcodes

                case 0x20: // JSR Absolute
                    return JSR_Instruction();

                #endregion

                #region LDA Opcodes

                case 0xA9: // LDA Immediate
                    value = ReadImmediate();
                    LDA(value);
                    return 2;

                case 0xA5: // LDA Zero Page
                    value = ReadZeroPage();
                    LDA(value);
                    return 3;

                case 0xB5: // LDA Zero Page, X
                    value = ReadZeroPageX();
                    LDA(value);
                    return 4;

                case 0xAD: //LDA Absolute
                    value = ReadAbsolute();
                    LDA(value);
                    return 4;

                case 0xBD: //LDA Absolute, X
                    value = ReadAbsoluteX(out pageBoundaryCrossed);
                    LDA(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xB9: // LDA Absolute, Y
                    value = ReadAbsoluteY(out pageBoundaryCrossed);
                    LDA(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xA1: // LDA (Indirect, X)
                    value = ReadIndirectX();
                    LDA(value);
                    return 6;

                case 0xB1: //LDA (Indirect), Y
                    value = ReadIndirectY(out pageBoundaryCrossed);
                    LDA(value);
                    return 5 + (pageBoundaryCrossed ? 1 : 0);

                #endregion

                #region LDX Opcodes

                // LDX Immediate
                case 0xA2:
                    {
                        value = ReadImmediate();
                        LDX(value);
                        return 2; // 2 cycles
                    }

                // LDX Zero Page
                case 0xA6:
                    {
                        value = ReadZeroPage();
                        LDX(value);
                        return 3; // 3 cycles
                    }

                // LDX Zero Page,Y
                case 0xB6:
                    {
                        value = ReadZeroPageY(); // Implement ReadZeroPageY similar to ReadZeroPageX
                        LDX(value);
                        return 4; // 4 cycles
                    }

                // LDX Absolute
                case 0xAE:
                    {
                        value = ReadAbsolute();
                        LDX(value);
                        return 4; // 4 cycles
                    }

                // LDX Absolute,Y
                case 0xBE:
                    {
                        value = ReadAbsoluteY(out pageBoundaryCrossed);
                        LDX(value);
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }

                #endregion

                #region LDY Opcodes

                // LDY Immediate
                case 0xA0:
                    {
                        value = ReadImmediate();
                        LDY(value);
                        return 2; // 2 cycles
                    }

                // LDY Zero Page
                case 0xA4:
                    {
                        value = ReadZeroPage();
                        LDY(value);
                        return 3; // 3 cycles
                    }

                // LDY Zero Page,X
                case 0xB4:
                    {
                        value = ReadZeroPageX(); // Implement ReadZeroPageX if not already
                        LDY(value);
                        return 4; // 4 cycles
                    }

                // LDY Absolute
                case 0xAC:
                    {
                        value = ReadAbsolute();
                        LDY(value);
                        return 4; // 4 cycles
                    }

                // LDY Absolute,X
                case 0xBC:
                    {
                        value = ReadAbsoluteX(out pageBoundaryCrossed);
                        LDY(value);
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }


                #endregion

                #region LSR Opcodes

                case 0x4A: // LSR Accumulator
                    LSR_Accumulator();
                    return 2;

                case 0x46: // LSR Zero Page
                    LSR_Memory(ReadZeroPage());
                    return 5;

                case 0x56: // LSR Zero Page, X
                    LSR_Memory(ReadZeroPageX());
                    return 6;

                case 0x4E: // LSR Absolute
                    LSR_Memory(ReadAbsolute());
                    return 6;

                case 0x5E: // LSR Absolute, X
                    LSR_Memory(ReadAbsoluteX(out pageBoundaryCrossed));
                    return 7;

                #endregion

                #region NOP Opcode

                case 0xEA: //NOP
                    return 2;


                #endregion

                #region ORA Opcodes

                // ORA Immediate
                case 0x09:
                    value = ReadImmediate();
                    ORA(value);
                    return 2; // 2 cycles

                // ORA Zero Page
                case 0x05:
                    {
                        value = ReadZeroPage();
                        ORA(value);
                        return 3; // 3 cycles
                    }

                // ORA Zero Page,X
                case 0x15:
                    {
                        value = ReadZeroPageX();
                        ORA(value);
                        return 4; // 4 cycles
                    }

                // ORA Absolute
                case 0x0D:
                    {
                        value = ReadAbsolute();
                        ORA(value);
                        return 4; // 4 cycles
                    }

                // ORA Absolute,X
                case 0x1D:
                    {
                        value = ReadAbsoluteX(out pageBoundaryCrossed);
                        ORA(value);
                        // 4 cycles + 1 if page boundary crossed
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }

                // ORA Absolute,Y
                case 0x19:
                    {
                        value = ReadAbsoluteY(out pageBoundaryCrossed);
                        ORA(value);
                        // 4 cycles + 1 if page boundary crossed
                        return 4 + (pageBoundaryCrossed ? 1 : 0);
                    }

                // ORA (Indirect,X)
                case 0x01:
                    {
                        value = ReadIndirectX();
                        ORA(value);
                        return 6; // 6 cycles
                    }

                // ORA (Indirect),Y
                case 0x11:
                    {
                        value = ReadIndirectY(out pageBoundaryCrossed);
                        ORA(value);
                        // 5 cycles + 1 if page boundary crossed
                        return 5 + (pageBoundaryCrossed ? 1 : 0);
                    }

                #endregion

                #region Register Opcodes

                case 0xAA: //TAX (Transfer Accumulator to X)
                    TAX();
                    return 2;

                case 0x8A: //TXA (Transfer X to Accumulator
                    TXA();
                    return 2;

                case 0xCA: //DEX (Decrement X)
                    DEX();
                    return 2;

                case 0xE8: //INX (Increment X)
                    INX();
                    return 2;

                case 0xA8: //TAY (Transfer Accumulator to Y)
                    TAY();
                    return 2;

                case 0x98: // TYA (Transfer Y to Accumulator)
                    TYA();
                    return 2;

                case 0x88: //DEY (Decrement Y)
                    DEY();
                    return 2;

                case 0xC8: //INY (Increment Y)
                    INY();
                    return 2;

                #endregion

                #region ROL Opcodes

                case 0x2A: // ROL Accumulator
                    ROL_Accumulator();
                    return 2;

                case 0x26: // ROL Zero Page
                    ROL_Memory(ReadZeroPage());
                    return 5;

                case 0x36: // ROL Zero Page, X
                    ROL_Memory(ReadZeroPageX());
                    return 6;

                case 0x2E: //ROL Aboslute
                    ROL_Memory(ReadAbsolute());
                    return 6;

                case 0x3E: // ROL Absolute, X
                    ROL_Memory(ReadAbsoluteX(out pageBoundaryCrossed));
                    return 7;

                #endregion

                #region ROR Opcodes

                case 0x6A: // ROR Accumulator
                    ROR_Accumulator();
                    return 2;

                case 0x66: // ROR Zero Page
                    ROR_Memory(ReadZeroPage());
                    return 5;

                case 0x76: // ROR Zero Page, X
                    ROR_Memory(ReadZeroPageX());
                    return 6;

                case 0x6E: // ROR Absolute
                    ROR_Memory(ReadAbsolute());
                    return 6;

                case 0x7E:
                    ROR_Memory(ReadAbsoluteX(out pageBoundaryCrossed));
                    return 7;

                #endregion

                #region RTI Opcodes

                case 0x40: // RTI (Return From Interrupt)
                    RTI();
                    return 6;

                #endregion

                #region RTS Opcodes

                case 0x60: // RTS (Return from subroutine)
                    RTS();
                    return 6;

                #endregion

                #region SBC Opcodes

                case 0xE9: // SBC Immediate
                    value = ReadImmediate();
                    SBC(value);
                    return 2;

                case 0xE5: // SBC Zero Page
                    value = ReadZeroPage();
                    SBC(value);
                    return 3;

                case 0xF5: // SBC Zero Page, X
                    value = ReadZeroPageX();
                    SBC(value);
                    return 4;

                case 0xED: // SBC Absolute
                    value = ReadAbsolute();
                    SBC(value);
                    return 4;

                case 0xFD: // SBC Absolute, X
                    value = ReadAbsoluteX(out pageBoundaryCrossed);
                    SBC(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xF9: // SBC Absolute, Y
                    value = ReadAbsoluteY(out pageBoundaryCrossed);
                    SBC(value);
                    return 4 + (pageBoundaryCrossed ? 1 : 0);

                case 0xE1: // SBC (indirect, X)
                    value = ReadIndirectX();
                    SBC(value);
                    return 6;

                case 0xF1: // SBC (Indirect), Y
                    value = ReadIndirectY(out pageBoundaryCrossed);
                    SBC(value);
                    return 5 + (pageBoundaryCrossed ? 1 : 0);

                #endregion

                #region STA Opcodes

                case 0x85: // STA Zero Page
                    STA(ReadZeroPage(), A);
                    return 3;

                case 0x95: // STA Zero Page, X
                    STA(ReadZeroPageX(), A);
                    return 4;

                case 0x8D: // STA Absolute
                    STA(ReadAbsolute(), A);
                    return 4;

                case 0x9D: // STA Absolute, X
                    STA(ReadAbsoluteX(out pageBoundaryCrossed), A);
                    return 5;

                case 0x99: // STA Absolute, Y
                    STA(ReadAbsoluteY(out pageBoundaryCrossed), A);
                    return 5;

                case 0x81: // STA (Indirect, X)
                    STA(ReadIndirectX(), A);
                    return 6;

                case 0x91: // STA (Indirect), Y)
                    STA(ReadIndirectY(out pageBoundaryCrossed), A);
                    return 6;

                #endregion

                #region STX Opcodes

                case 0x86: // STX Zero Page
                    STX(ReadZeroPage(), X);
                    return 3;

                case 0x96: // STX Zero Page, Y
                    STX(ReadZeroPageY(), X);
                    return 4;

                case 0x8E: // STX Absolute
                    STX(ReadAbsolute(), X);
                    return 4;

                #endregion

                #region STY Opcodes

                case 0x84: // STY Zero Page
                    STY(ReadZeroPage(), Y);
                    return 3;

                case 0x94: // STY Zero Page, X
                    STY(ReadZeroPageX(), Y);
                    return 4;

                case 0x8C: // STY Absolute
                    STY(ReadAbsolute(), Y);
                    return 4;

                #endregion

                #region Stack Opcodes

                case 0x9A: //TXS (Transfer X to Stack Pointer
                    TXS();
                    return 2;

                case 0xBA: // TSX (Transfer stack pointer to X)
                    TSX();
                    return 2;

                case 0x48: // PHA (Push Accumulator)
                    PHA();
                    return 3;

                case 0x68: // PLA (Pull Accumulator)
                    PLA();
                    return 4;

                case 0x08: // PHP (Push Processor Status)
                    PHP();
                    return 3;

                case 0x28: // PLP (Pull Processor Status)
                    PLP();
                    return 4;

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

        #region CPX Logic

        private void CPX(byte value)
        {
            //Compare X with value
            int temp = X - value;

            // Carry is set if X >= value
            if (X >= value)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            // Zero set if X == value
            if ((temp & 0xFF) == 0)
                P |= FLAG_ZERO;
            else
                P &= unchecked((byte)~FLAG_ZERO);

            // Negative depends on bit 7 of the (X - value) result
            byte result8 = (byte)(temp & 0xFF);
            if ((result8 & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= unchecked((byte)~FLAG_NEGATIVE);
        }

        #endregion

        #region CPY Logic

        private void CPY(byte value)
        {
            // Compare Y with 'value' (like Y - value)
            int temp = Y - value;

            // Carry = 1 if Y >= value
            if (Y >= value)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            // Zero if Y == value
            if ((temp & 0xFF) == 0)
                P |= FLAG_ZERO;
            else
                P &= unchecked((byte)~FLAG_ZERO);

            // Negative if bit 7 of the result is set
            byte result8 = (byte)(temp & 0xFF);
            if ((result8 & 0x80) != 0)
                P |= FLAG_NEGATIVE;
            else
                P &= unchecked((byte)~FLAG_NEGATIVE);
        }


        #endregion

        #region DEC Logic

        private void DEC_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);
            value--;
            _memory.WriteByte(address, value);

            SetZeroAndNegativeFlags(value);
        }

        #endregion

        #region EOR Logic

        private void EOR(byte value)
        {
            A ^= value;
            SetZeroAndNegativeFlags(A);
        }

        #endregion

        #region INC Logic

        private void INC_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);
            value++;
            _memory.WriteByte(address, value);

            SetZeroAndNegativeFlags(value);
        }

        #endregion

        #region JSR Logic

        private int JSR_Instruction()
        {
            ushort target = _memory.ReadWord(PC);

            ushort returnAddress = (ushort)(PC + 1);
            PC += 2;

            PushByte((byte)((returnAddress >> 8) & 0xFF));
            PushByte((byte)(returnAddress & 0xFF));

            PC = target;
            return 6;
        }

        #endregion

        #region LDA Logic

        private void LDA(byte value)
        {
            A = value;
            SetZeroAndNegativeFlags(A);
        }

        #endregion

        #region LDX Logic

        private void LDX(byte value)
        {
            X = value;
            SetZeroAndNegativeFlags(X);
        }

        #endregion

        #region LDY Logic

        private void LDY(byte value)
        {
            Y = value;
            SetZeroAndNegativeFlags(Y);
        }

        #endregion

        #region LSR Logic

        private void LSR_Accumulator()
        {
            bool carry = (A & 0x01) != 0;

            A >>= 1;

            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            SetZeroAndNegativeFlags(A);
        }

        private void LSR_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);

            bool carry = (value & 0x01) != 0;

            value >>= 1;

            _memory.WriteByte(address, value);

            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            SetZeroAndNegativeFlags(value);
        }

        #endregion

        #region ORA Logic

        private void ORA(byte value)
        {
            A |= value;
            SetZeroAndNegativeFlags(A);
        }

        #endregion

        #region Register Logic

        private void TAX()
        {
            X = A;
            SetZeroAndNegativeFlags(X);
        }

        private void TXA()
        {
            A = X;
            SetZeroAndNegativeFlags(A);
        }

        private void DEX()
        {
            X--;
            SetZeroAndNegativeFlags(X);
        }

        private void INX()
        {
            X++;
            SetZeroAndNegativeFlags(X);
        }

        private void TAY()
        {
            Y = A;
            SetZeroAndNegativeFlags(Y);
        }

        private void TYA()
        {
            A = Y;
            SetZeroAndNegativeFlags(A);
        }

        private void DEY()
        {
            Y--;
            SetZeroAndNegativeFlags(Y);
        }

        private void INY()
        {
            Y++;
            SetZeroAndNegativeFlags(Y);
        }

        #endregion

        #region ROL Logic

        private void ROL_Accumulator()
        {
            bool carry = (A & 0x80) != 0;
            A <<= 1;

            if ((P & FLAG_CARRY) != 0)
                A |= 0x01;

            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            SetZeroAndNegativeFlags(A);
        }

        private void ROL_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);

            bool carry = (value & 0x80) != 0;
            value <<= 1;

            if ((P & FLAG_CARRY) != 0)
                value |= 0x01;

            _memory.WriteByte(address, value);

            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            SetZeroAndNegativeFlags(value);
        }

        #endregion

        #region ROR Logic

        private void ROR_Accumulator()
        {
            // Original bit 0 is shifted into Carry
            bool carry = (A & 0x01) != 0;

            // Shift right by one
            A >>= 1;

            // If Carry was set before, set bit 7
            if ((P & FLAG_CARRY) != 0)
                A |= 0x80;

            // Set or clear Carry flag based on original bit 0
            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            // Update Zero and Negative flags
            SetZeroAndNegativeFlags(A);
        }

        private void ROR_Memory(ushort address)
        {
            byte value = _memory.ReadByte(address);

            // Original bit 0 is shifted into Carry
            bool carry = (value & 0x01) != 0;

            // Shift right by one
            value >>= 1;

            // If Carry was set before, set bit 7
            if ((P & FLAG_CARRY) != 0)
                value |= 0x80;

            // Write the shifted value back to memory
            _memory.WriteByte(address, value);

            // Set or clear Carry flag based on original bit 0
            if (carry)
                P |= FLAG_CARRY;
            else
                P &= unchecked((byte)~FLAG_CARRY);

            // Update Zero and Negative flags
            SetZeroAndNegativeFlags(value);
        }


        #endregion

        #region RTI Logic

        private void RTI()
        {
            byte status = PopByte();
            status &= unchecked((byte)~FLAG_BREAK);
            status &= unchecked((byte)~0x20);
            P = status;

            // Pull Program Counter low byte
            byte pcLow = PopByte();
            // Pull Program Counter high byte
            byte pcHigh = PopByte();
            PC = (ushort)(pcLow | (pcHigh << 8));
        }


        #endregion

        #region RTS Logic

        private void RTS()
        {
            byte pcLow = PopByte();
            byte pcHigh = PopByte();
            ushort returnAddress = (ushort)(pcLow | (pcHigh << 8));

            PC = (ushort)(returnAddress + 1);
        }

        #endregion

        #region SBC Logic

        private void SBC(byte value)
        {
            if ((P & FLAG_DECIMAL) != 0)
            {
                int a = (A >> 4) * 10 + (A & 0x0F);
                int m = (value >> 4) * 10 + (value & 0x0F);
                int c = (P & FLAG_CARRY) != 0 ? 1 : 0;

                int result = a - m - (1 - c);

                if (result >= 0)
                    P |= FLAG_CARRY;
                else
                    P &= unchecked((byte)~FLAG_CARRY);

                if (result < 0)
                    result += 100;

                A = (byte)((result / 10) << 4 | (result % 10));

                if (A == 0)
                    P |= FLAG_ZERO;
                else
                    P &= unchecked((byte)~FLAG_ZERO);

                if ((A & 0x80) != 0)
                    P |= FLAG_NEGATIVE;
                else
                    P &= unchecked((byte)~FLAG_NEGATIVE);
            }
            else
            {
                byte complement = (byte)(~value);
                ADC(complement);
            }
        }

        #endregion

        #region STA Logic

        private void STA(ushort address, byte value)
        {
            _memory.WriteByte(address, value);
        }

        #endregion

        #region STX Logic

        private void STX(ushort address, byte value)
        {
            _memory.WriteByte(address, value);
        }

        #endregion

        #region STY Logic

        private void STY(ushort address, byte value)
        {
            _memory.WriteByte(address, value);
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

        private byte ReadZeroPageY()
        {
            byte addr = _memory.ReadByte(PC);
            PC++;
            addr = (byte)(addr + Y);
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

        private byte DECZeroPageAddr()
        {
            byte zpAddr = _memory.ReadByte(PC);
            PC++;
            return zpAddr;
        }

        private byte GetZeroPageXAddress()
        {
            byte zpAddr = _memory.ReadByte(PC);
            PC++;
            byte finalAddr = (byte)(zpAddr + X);
            return finalAddr;
        }

        private ushort DECAbsolute()
        {
            ushort addr = _memory.ReadWord(PC);
            PC += 2;
            return addr;
        }

        private ushort DECAbsoluteX()
        {
            ushort baseAddr = _memory.ReadWord(PC);
            PC += 2;
            ushort finalAddr = (ushort)(baseAddr + X);
            return finalAddr;
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

        #region Stack Operations

        private void PushByte(byte value)
        {
            _memory.WriteByte((ushort)(0x0100 + SP), value);
            SP--;
        }

        private byte PopByte()
        {
            SP++;
            return _memory.ReadByte((ushort)(0x0100 + SP));
        }

        private void TXS()
        {
            SP = X;
        }

        private void TSX()
        {
            X = SP;
            SetZeroAndNegativeFlags(X);
        }

        private void PHA()
        {
            PushByte(A);
        }

        private void PLA()
        {
            A = PopByte();
            SetZeroAndNegativeFlags(A);
        }

        private void PHP()
        {
            byte status = (byte)(P | FLAG_BREAK | 0x20);
            PushByte(status);
        }

        private void PLP()
        {
            byte status = PopByte();
            status &= unchecked((byte)~FLAG_BREAK);
            status &= 0xEF;

            P = status;
        }

        #endregion
    }
}
