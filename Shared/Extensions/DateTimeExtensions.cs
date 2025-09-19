namespace Shared.Extensions;

using System;

public static class DateTimeExtensions
{
    #region Methods

    public static Guid ToGuid(this DateTime dt)
    {
        Byte[] bytes = BitConverter.GetBytes(dt.Ticks);

        Array.Resize(ref bytes, 16);

        return new Guid(bytes);
    }

    #endregion
}