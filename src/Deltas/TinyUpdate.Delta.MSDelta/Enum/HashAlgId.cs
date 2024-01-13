namespace TinyUpdate.Delta.MSDelta.Enum;

/// <summary>
/// Algorithm used for hashing
/// </summary>
internal enum HashAlgId
{
    /// <summary>No signature</summary>
    None = 0,

    /// <summary>32-bit CRC defined in msdelta.dll</summary>
    Crc32 = 32
}