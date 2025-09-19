namespace Shared.General;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class GuidCalculator
{
    #region Methods

    public static Guid Combine(Guid firstGuid,
                               Guid secondGuid,
                               Byte offset) {
        Byte[] firstAsBytes = firstGuid.ToByteArray();
        Byte[] secondAsBytes = secondGuid.ToByteArray();

        Byte[] newBytes = new Byte[16];

        for (Int32 i = 0; i < 16; i++) {
            // Add and truncate any overflow
            newBytes[i] = (Byte)(firstAsBytes[i] + secondAsBytes[i] + offset);
        }

        return new Guid(newBytes);
    }

    public static Guid Combine(Guid firstGuid,
                               Guid secondGuid) {
        return GuidCalculator.Combine(firstGuid, secondGuid, 0);
    }

    public static Guid Combine(Guid firstGuid,
                               Guid secondGuid,
                               Guid thirdGuid,
                               Byte offset) {
        Byte[] firstAsBytes = firstGuid.ToByteArray();
        Byte[] secondAsBytes = secondGuid.ToByteArray();
        Byte[] thirdAsBytes = thirdGuid.ToByteArray();

        Byte[] newBytes = new Byte[16];

        for (Int32 i = 0; i < 16; i++) {
            // Add and truncate any overflow
            newBytes[i] = (Byte)(firstAsBytes[i] + secondAsBytes[i] + thirdAsBytes[i] + offset);
        }

        return new Guid(newBytes);
    }

    public static Guid Combine(Guid firstGuid,
                               Guid secondGuid,
                               Guid thirdGuid) {
        return GuidCalculator.Combine(firstGuid, secondGuid, thirdGuid, 0);
    }

    #endregion
}