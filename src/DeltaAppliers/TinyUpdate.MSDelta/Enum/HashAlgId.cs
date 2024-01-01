namespace TinyUpdate.MSDelta.Enum;

internal enum HashAlgId
{
    /// <summary>No signature.</summary>
    None = 0,

    /// <summary>32-bit CRC defined in msdelta.dll.</summary>
    Crc32 = 32 // 0x00000020
}