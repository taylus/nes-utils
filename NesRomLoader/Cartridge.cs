using System;
using System.IO;
using System.Linq;

namespace NesRomLoader
{
    public class Cartridge
    {
        //the entire ROM
        private byte[] romData;

        //copies of special sections of the ROM
        private byte[] header;
        private byte[] prgRom;  //program ROM - addressable by the CPU in mapped chunks
        private byte[] chrRom;  //graphics ("character") ROM - addressable by the PPU in mapped chunks

        /// <summary>
        /// Loads a ROM file in the iNES file format:
        /// http://wiki.nesdev.com/w/index.php/INES
        /// </summary>
        public Cartridge(string path)
        {
            LoadRom(File.ReadAllBytes(path));

            const int romPreviewSize = 64;
            Console.WriteLine($"\nFirst {romPreviewSize} bytes of PRG ROM (compare to an emulator's memory viewer):");
            HexDump(prgRom, 16, 64);

            Console.WriteLine($"\nFirst {romPreviewSize} bytes of CHR ROM (compare to an emulator's memory viewer):");
            HexDump(chrRom, 16, 64);
        }

        private void LoadRom(byte[] romData)
        {
            this.romData = romData;
            header = romData.Take(16).ToArray();
            Console.WriteLine($"Parsing ROM header: {BitConverter.ToString(header)}");

            //first 4 bytes should be constant ASCII "NES" + ^Z
            if (header[0] == 0x4e && header[1] == 0x45 && header[2] == 0x53 && header[3] == 0x1a)
            {
                Console.WriteLine("- Valid NES magic number.");
            }
            else
            {
                Console.WriteLine("- Invalid NES magic number!");
            }

            byte prgRom16kBanks = header[4];
            int prgRomBytes = prgRom16kBanks * 16 * 1024;
            Console.WriteLine($"- PRG ROM: {prgRom16kBanks} x 16 KB = {prgRomBytes} bytes");

            byte chrRom8kBanks = header[5];
            int chrRomBytes = chrRom8kBanks * 8 * 1024;
            Console.WriteLine($"- CHR ROM: {chrRom8kBanks} x 8 KB = {chrRom8kBanks * 8} KB");

            byte prgRam8kBanks = header[8];
            Console.WriteLine($"- PRG RAM: {prgRam8kBanks} x 8 KB = {prgRam8kBanks * 8} KB");

            byte flags6 = header[6];
            if ((flags6 & 1) == 0)
            {
                Console.WriteLine("- Mirroring mode: Horizontal");
            }
            else
            {
                Console.WriteLine("- Mirroring mode: Vertical");
            }

            bool usesTrainer = (flags6 & 4) != 0;
            Console.WriteLine($"- Trainer: {(usesTrainer ? "Yes" : "No")}");
            int trainerSize = usesTrainer ? 512 : 0;

            //load program and character memories now that we know where they begin and end
            prgRom = romData.Skip(16 + trainerSize).Take(prgRomBytes).ToArray();
            chrRom = romData.Skip(16 + trainerSize + prgRomBytes).Take(chrRomBytes).ToArray();

            //mappers control how the game cartridge's > 64k of memory is bank switched to be addressable by the CPU's 16 bit memory bus
            //mappers are numbered by a byte composed of the most significant bits of flags7 followed by the most significant bits of flags6
            byte flags7 = header[7];
            byte mapperNumber = (byte)((flags6 >> 4) | (flags7 & 0b1111_0000));
            Console.WriteLine($"- Mapper number: {mapperNumber}");

            //TODO: this isn't right? (FCEUX says my SMB1 ROM is PAL but this code is saying it's NTSC)
            byte flags9 = header[9];
            if ((flags9 & 1) == 0)
            {
                Console.WriteLine("- Region: NTSC");
            }
            else
            {
                Console.WriteLine("- Region: PAL");
            }
        }

        /// <summary>
        /// Prints the first N bytes of the given buffer to the console.
        /// </summary>
        private void HexDump(byte[] bytes, int bytesPerLine = 16, int? stopAfterBytes = null)
        {
            int length = stopAfterBytes ?? bytes.Length;
            for (int i = 0; i < length; i++)
            {
                Console.Write("{0:X2} ", bytes[i]);
                if (i % bytesPerLine == bytesPerLine - 1) Console.WriteLine();
            }
        }
    }
}
