namespace Shared.Serialisation;

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

[ExcludeFromCodeCoverage]
public class JsonSerialiser : IStringSerialiser
{
    #region Constructors

    public JsonSerialiser() : this(JsonSerialiser.GetDefaultSettings) {

    }
    public JsonSerialiser(Func<JsonSerializerSettings> jsonOptionsFunc)
    {
        this.JsonSerializerSettings = jsonOptionsFunc();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the json serializer settings.
    /// </summary>
    /// <value>
    /// The json serializer settings.
    /// </value>
    public JsonSerializerSettings JsonSerializerSettings { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Defaults this instance.
    /// </summary>
    /// <returns></returns>
    public static IStringSerialiser Default()
    {
        return new JsonSerialiser(JsonSerialiser.GetDefaultSettings);
    }

    /// <summary>
    /// Deserializes a message
    /// </summary>
    /// <typeparam name="T">The type of message</typeparam>
    /// <param name="data">The aggregateEvent to deserialize</param>
    /// <returns>
    /// The deserialized message
    /// </returns>
    public T Deserialise<T>(String data)
    {
        //As far as the caller is concerned, we are doing some nice generic stuff for them...
        //This could be purely based on generics, but I wanted to re-use the deserialize code. 
        return (T)this.Deserialise(data, typeof(T));
    }

    /// <summary>
    /// Deserializes a message
    /// </summary>
    /// <param name="data">The aggregateEvent to deserialize</param>
    /// <param name="type">The type of message</param>
    /// <returns>
    /// The deserialized message
    /// </returns>
    public Object Deserialise(String data,
                              Type type)
    {
        return JsonConvert.DeserializeObject(data, type, this.JsonSerializerSettings);
    }

    /// <summary>
    /// Deserialises the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    public Object Deserialise(String data)
    {
        return JsonConvert.DeserializeObject(data, this.JsonSerializerSettings);
    }

    /// <summary>
    /// Events the store default.
    /// </summary>
    /// <returns></returns>
    public static IStringSerialiser EventStoreDefault()
    {
        return new JsonSerialiser(JsonSerialiser.GetEventStoreDefaultSettings);
    }

    /// <summary>
    /// Gets the default settings.
    /// </summary>
    /// <returns></returns>
    public static JsonSerializerSettings GetDefaultSettings()
    {
        return new JsonSerializerSettings
               {
                   ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                   TypeNameHandling = TypeNameHandling.All,
                   Formatting = Formatting.Indented,
                   DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                   ContractResolver = new CamelCasePropertyNamesContractResolver()
               };
    }

    /// <summary>
    /// Gets the event store default settings.
    /// </summary>
    /// <returns></returns>
    public static JsonSerializerSettings GetEventStoreDefaultSettings()
    {
        return new JsonSerializerSettings
               {
                   ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                   TypeNameHandling = TypeNameHandling.All,
                   Formatting = Formatting.Indented,
                   DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                   ContractResolver = new CamelCasePropertyNamesContractResolver()
               };
    }

    /// <summary>
    /// Serializes a message
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectToSerialize">The object to serialize</param>
    /// <returns>
    /// The serialized message as a string
    /// </returns>
    public String Serialise<T>(T objectToSerialize)
    {
        return JsonConvert.SerializeObject(objectToSerialize, this.JsonSerializerSettings);
    }

    #endregion
}