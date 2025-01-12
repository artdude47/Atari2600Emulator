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

        private bool _halted = false;

        public bool Halted { get { return _halted; } }

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

                case 0x29: // AND Immediate
                    A &= GetImmediateValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x25: // AND Zero Page
                    A &= GetZeroPageValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x2D: // AND Absolute
                    A &= GetAbsoluteValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x35: // AND Zero Page, X
                    A &= GetZeroPageXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x3D: // AND Absolute, X
                    A &= GetAbsoluteXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x39: // AND Absolute, Y
                    A &= GetAbsoluteYValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x09: // ORA Immediate
                    A |= GetImmediateValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x05: // ORA Zero Page
                    A |= GetZeroPageValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x0D: // ORA Absolute
                    A |= GetAbsoluteValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x15: // ORA Zero Page, X
                    A |= GetZeroPageXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x1D: // ORA Absolute, X
                    A |= GetAbsoluteXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x19: // ORA Absolute, Y
                    A |= GetAbsoluteYValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x49: // EOR Immediate
                    A ^= GetImmediateValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x45: // EOR Zero Page
                    A ^= GetZeroPageValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x4D: // EOR Absolute
                    A ^= GetAbsoluteValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x55: // EOR Zero Page, X
                    A ^= GetZeroPageXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x5D: // EOR Absolute, X
                    A ^= GetAbsoluteXValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x59: // EOR Absolute, Y
                    A ^= GetAbsoluteYValue();
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x24: // BIT Zero Page
                    {
                        byte value = GetZeroPageValue();
                        P = (byte)((P & ~(FLAG_NEGATIVE | FLAG_OVERFLOW | FLAG_ZERO)) |
                                   (value & (FLAG_NEGATIVE | FLAG_OVERFLOW)) |
                                   ((value & A) == 0 ? FLAG_ZERO : 0));
                        break;
                    }

                case 0x2C: // BIT Absolute
                    {
                        byte value = GetAbsoluteValue();
                        P = (byte)((P & ~(FLAG_NEGATIVE | FLAG_OVERFLOW | FLAG_ZERO)) |
                                   (value & (FLAG_NEGATIVE | FLAG_OVERFLOW)) |
                                   ((value & A) == 0 ? FLAG_ZERO : 0));
                        break;
                    }

                case 0xE6: // INC Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        value++;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xF6: // INC Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        value++;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xEE: // INC Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        value++;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xFE: // INC Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        value++;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0xC6: // DEC Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        value--;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xD6: // DEC Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        value--;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xCE: // DEC Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        value--;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0xDE: // DEC Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        value--;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0xE8: // INX
                    X++;
                    SetZeroAndNegativeFlags(X);
                    break;

                case 0xCA: // DEX
                    X--;
                    SetZeroAndNegativeFlags(X);
                    break;

                case 0xC8: // INY
                    Y++;
                    SetZeroAndNegativeFlags(Y);
                    break;

                case 0x88: // DEY
                    Y--;
                    SetZeroAndNegativeFlags(Y);
                    break;

                case 0x0A: // ASL A
                    P = (byte)((P & ~FLAG_CARRY) | ((A & 0x80) >> 7));
                    A <<= 1;
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x06: // ASL Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value <<= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x16: // ASL Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value <<= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x0E: // ASL Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value <<= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x1E: // ASL Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value <<= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0x4A: // LSR A
                    P = (byte)((P & ~FLAG_CARRY) | (A & 0x01));
                    A >>= 1;
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x46: // LSR Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value >>= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x56: // LSR Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value >>= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x4E: // LSR Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value >>= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x5E: // LSR Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value >>= 1;
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0x2A: // ROL A
                    {
                        byte carry = (byte)((P & FLAG_CARRY) >> 0);
                        P = (byte)((P & ~FLAG_CARRY) | ((A & 0x80) >> 7));
                        A = (byte)((A << 1) | carry);
                        SetZeroAndNegativeFlags(A);
                        break;
                    }
                case 0x26: // ROL Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) >> 0);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value = (byte)((value << 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x36: // ROL Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) >> 0);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value = (byte)((value << 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x2E: // ROL Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) >> 0);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value = (byte)((value << 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x3E: // ROL Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) >> 0);
                        P = (byte)((P & ~FLAG_CARRY) | ((value & 0x80) >> 7));
                        value = (byte)((value << 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0x6A: // ROR A
                    {
                        byte carry = (byte)((P & FLAG_CARRY) << 7);
                        P = (byte)((P & ~FLAG_CARRY) | (A & 0x01));
                        A = (byte)((A >> 1) | carry);
                        SetZeroAndNegativeFlags(A);
                        break;
                    }
                case 0x66: // ROR Zero Page
                    {
                        byte address = _memory.ReadByte(PC++);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) << 7);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value = (byte)((value >> 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x76: // ROR Zero Page, X
                    {
                        byte address = (byte)(_memory.ReadByte(PC++) + X);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) << 7);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value = (byte)((value >> 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x6E: // ROR Absolute
                    {
                        ushort address = GetAbsoluteAddress();
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) << 7);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value = (byte)((value >> 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }
                case 0x7E: // ROR Absolute, X
                    {
                        ushort baseAddress = GetAbsoluteAddress();
                        ushort address = (ushort)(baseAddress + X);
                        byte value = _memory.ReadByte(address);
                        byte carry = (byte)((P & FLAG_CARRY) << 7);
                        P = (byte)((P & ~FLAG_CARRY) | (value & 0x01));
                        value = (byte)((value >> 1) | carry);
                        _memory.WriteByte(address, value);
                        SetZeroAndNegativeFlags(value);
                        break;
                    }

                case 0x18: // CLC
                    P &= (byte)(~FLAG_CARRY & 0xFF);
                    break;

                case 0x58: // CLI
                    P &= (byte)(~FLAG_INTERRUPT & 0xFF);
                    break;

                case 0xB8: // CLV
                    P &= (byte)(~FLAG_OVERFLOW & 0xFF);
                    break;

                case 0xD8: // CLD
                    P &= (byte)(~FLAG_DECIMAL & 0xFF);
                    break;

                case 0x38: // SEC
                    P |= FLAG_CARRY;
                    break;

                case 0x78: // SEI
                    P |= FLAG_INTERRUPT;
                    break;

                case 0xF8: // SED
                    P |= FLAG_DECIMAL;
                    break;

                case 0x48: // PHA
                    _memory.WriteByte((ushort)(0x0100 + SP), A);
                    SP--;
                    break;

                case 0x08: // PHP
                    _memory.WriteByte((ushort)(0x0100 + SP), (byte)(P | FLAG_BREAK)); // Push P with the break flag set
                    SP--;
                    break;

                case 0x68: // PLA
                    SP++;
                    A = _memory.ReadByte((ushort)(0x0100 + SP));
                    SetZeroAndNegativeFlags(A);
                    break;

                case 0x28: // PLP
                    SP++;
                    P = (byte)(_memory.ReadByte((ushort)(0x0100 + SP)) & 0xEF); // Clear the break flag
                    break;

                case 0x4C: // JMP Absolute
                    PC = GetAbsoluteAddress();
                    break;

                case 0x6C: // JMP Indirect
                    {
                        ushort indirectAddress = GetAbsoluteAddress();
                        ushort targetAddress = (ushort)(_memory.ReadByte(indirectAddress) |
                                                        (_memory.ReadByte((ushort)((indirectAddress + 1) & 0xFFFF)) << 8));
                        PC = targetAddress;
                        break;
                    }

                case 0x20: // JSR Absolute
                    {
                        ushort returnAddress = (ushort)(PC + 1); // Return address is PC + 1
                        _memory.WriteByte((ushort)(0x0100 + SP), (byte)((returnAddress >> 8) & 0xFF)); // Push high byte
                        SP--;
                        _memory.WriteByte((ushort)(0x0100 + SP), (byte)(returnAddress & 0xFF));        // Push low byte
                        SP--;
                        PC = GetAbsoluteAddress();
                        break;
                    }

                case 0x60: // RTS
                    {
                        SP++;
                        ushort returnAddress = _memory.ReadByte((ushort)(0x0100 + SP));
                        SP++;
                        returnAddress |= (ushort)(_memory.ReadByte((ushort)(0x0100 + SP)) << 8);
                        PC = (ushort)(returnAddress + 1); // Add 1 to point to the instruction after JSR
                        break;
                    }

                case 0x40: // RTI
                    {
                        SP++;
                        P = (byte)(_memory.ReadByte((ushort)(0x0100 + SP)) & 0xEF); // Pop `P` (clear Break flag)
                        SP++;
                        ushort returnAddress = _memory.ReadByte((ushort)(0x0100 + SP));
                        SP++;
                        returnAddress |= (ushort)(_memory.ReadByte((ushort)(0x0100 + SP)) << 8);
                        PC = returnAddress;
                        break;
                    }

                case 0x00: // BRK
                    PC++;
                    TriggerInterrupt(0xFFFE, true);
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

        private void TriggerInterrupt(ushort vectorAddress, bool isBRK = false)
        {
            // Push PC onto the stack
            _memory.WriteByte((ushort)(0x0100 + SP), (byte)((PC >> 8) & 0xFF)); // High byte
            SP--;
            _memory.WriteByte((ushort)(0x0100 + SP), (byte)(PC & 0xFF));        // Low byte
            SP--;

            // Push status register onto the stack
            byte flags = (byte)(P | (isBRK ? FLAG_BREAK : 0));
            _memory.WriteByte((ushort)(0x0100 + SP), flags);
            SP--;

            // Set interrupt disable flag
            P |= FLAG_INTERRUPT;

            // Set PC to the interrupt vector
            PC = (ushort)(_memory.ReadByte(vectorAddress) |
                         (_memory.ReadByte((ushort)(vectorAddress + 1)) << 8));
        }

        public void TriggerNMI()
        {
            TriggerInterrupt(0xFFFA);
        }

        public void TriggerIRQ()
        {
            if ((P & FLAG_INTERRUPT) == 0) // Check if interrupts are enabled
            {
                TriggerInterrupt(0xFFFE);
            }
        }

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
