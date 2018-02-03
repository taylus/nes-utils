using System.IO;
using System.Linq;
using System.Diagnostics;

namespace NesRomWriter
{
    public class Program
    {
        private const int PrgRomBankSize = 16 * 1024;   //16KB
        private const int ChrRomBankSize = 8 * 1024;    //8KB

        /// <summary>
        /// Generates an NES ROM and runs it on an emulator.
        /// This is a highly impractical learning exercise in NES assembly language programming.
        /// </summary>
        public static void Main(string[] args)
        {
            const string emulator = @"D:\emu\fceux-2.2.3-win32\fceux.exe";
            //const string rom = @"""C:\users\brandon\Desktop\Super Mario Bros. (W) [!].nes""";
            const string rom = @"bin\TestROM.nes";
            WriteiNesRom(rom);
            Process.Start(emulator, rom);
        }

        /// <summary>
        /// Writes an NES ROM in iNES format to the given file path.
        /// Default to the standard 2x16KB of PRG ROM and 1x8KB CHR ROM
        /// that a cartridge with no mapper shenanigans uses (like SMB1).
        /// </summary>
        private static void WriteiNesRom(string path, byte prgRomBankCount = 2, byte chrRomBankCount = 1)
        {
            byte[] header = WriteiNesHeader(prgRomBankCount, chrRomBankCount);
            byte[] prgRom = WritePrgRom(prgRomBankCount);
            byte[] chrRom = WriteChrRom(chrRomBankCount);
            File.WriteAllBytes(path, header.Concat(prgRom).Concat(chrRom).ToArray());
        }

        /// <summary>
        /// Returns an iNES file header according to https://wiki.nesdev.com/w/index.php/INES#iNES_file_format.
        /// This header identifies that the NES ROM has a given number of program and
        /// character (graphics) memory banks, plus lots of other settings (currently zero-filled).
        /// </summary>
        private static byte[] WriteiNesHeader(byte prgRomBankCount, byte chrRomBankCount)
        {
            return new byte[]
            {
                0x4E, 0x45, 0x53, 0x1A, //"NES^Z"
                prgRomBankCount, chrRomBankCount, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };
        }

        /// <summary>
        /// Returns the program ROM for the most simplistic NES ROM imaginable --
        /// it infinitely loops loading the constant 1 into the CPU's accumulator.
        /// (You have to use an emulator's debugger to even see that it's doing anything.)
        /// </summary>
        private static byte[] WritePrgRom(byte bankCount)
        {
            ///the NES loads PRG ROM into address $8000 when it boots:
            //https://en.wikibooks.org/wiki/NES_Programming/Memory_Map
            var assembly = new byte[]
            {
                0xA9, 0x01,         //8000: LDA #$01
                0x4C, 0x00, 0x80    //8002: JMP $8000
                //zero fill...      //8005: BRK
                //...
            };

            //special "interrupt vectors" are expected at the end of addressable memory ($FFFA-FFFF)
            //these control where the processor jumps in special situations (startup, interrupts)
            //https://wiki.nesdev.com/w/index.php/CPU_memory_map
            var interruptVectors = new byte[]
            {
                0x00, 0x00, //NMI vector at address $0000
                0x00, 0x80, //RESET vector at address $8000 (start executing PRG ROM at this address, which is where the NES loads it by default)
                0x00, 0x80  //IRQ/BRK vector at address $8000 (jump here whenever we execute a BRK instruction, which is most of the ROM since it's all zeroes)
            };

            //fill the rest of PRG ROM with zeroes so that the interrupt vectors line up where they're supposed to
            var zeroPadding = Enumerable.Repeat<byte>(0, (bankCount * PrgRomBankSize) - (assembly.Length + interruptVectors.Length));
            return assembly.Concat(zeroPadding).Concat(interruptVectors).ToArray();
        }

        /// <summary>
        /// Returns the character (graphics) ROM for the most simplistic NES ROM imaginable -- no graphics.
        /// </summary>
        private static byte[] WriteChrRom(byte bankCount)
        {
            return new byte[bankCount * ChrRomBankSize];
        }
    }
}
