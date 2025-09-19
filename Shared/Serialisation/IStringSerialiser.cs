using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Serialisation;

public interface IStringSerialiser
{
    #region Methods

    /// <summary>
    /// Deserialises a string back into an object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectToDeserialise">The object to deserialise.</param>
    /// <returns>The deserialized object</returns>
    T Deserialise<T>(String objectToDeserialise);

    /// <summary>
    /// Deserialises the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    Object Deserialise(String data);

    /// <summary>
    /// Deserialises the specified object to deserialise.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="type">The type.</param>
    /// <returns>Object.</returns>
    Object Deserialise(String data,
                       Type type);

    /// <summary>
    /// Serialises an object ointo a string
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectToSerialise">The object to serialise</param>
    /// <returns>The serialised message</returns>
    String Serialise<T>(T objectToSerialise);

    #endregion
}