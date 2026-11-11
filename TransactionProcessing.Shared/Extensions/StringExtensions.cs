using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TransactionProcessing.Shared.Serialisation;

namespace TransactionProcessing.Shared.Extensions;

public static class StringExtensions
{
    public static bool TryParseJson<T>(this string obj,
                                       out T result) {
        try {
            result = StringSerialiser.Deserialise<T>(obj);
            return true;
        }
        catch (JsonException) {
            result = default;
            return false;
        }
    }
}