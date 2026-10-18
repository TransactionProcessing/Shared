namespace Shared.Extensions;

using System;

public static class GuidExtensions
{

    public static DateTime ToDateTime(this Guid guid)
    {
        Byte[] bytes = guid.ToByteArray();

        Array.Resize(ref bytes, 8);

        return new DateTime(BitConverter.ToInt64(bytes));
    }

}