using System;

namespace NesRomLoader
{
    public class Program
    {
        public static void Main()
        {
            var cart = new Cartridge(@"roms\smb1.nes");
        }
    }
}
