namespace Shared.EventStore.Aggregate;

using System;
using System.Globalization;
using Newtonsoft.Json;

[JsonObject]
public struct AggregateVersion : IComparable
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateVersion" /> class.
    /// </summary>
    /// <param name="value">The Value.</param>
    [Newtonsoft.Json.JsonConstructor]
    private AggregateVersion(Int64 value) : this()
    {
        this.Value = value;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the Value.
    /// </summary>
    /// <value>The value.</value>
    [JsonProperty(Order = 1)]
    public Int64 Value { get; private set; }

    #endregion

    #region Methods

    /// <summary>
    /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
    /// </summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public Int32 CompareTo(Object obj)
    {
        AggregateVersion otherVersion = (AggregateVersion)obj;
        return this.Value.CompareTo(otherVersion.Value);
    }

    /// <summary>
    /// The create.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="AggregateVersion" />.</returns>
    public static AggregateVersion CreateFrom(Int64 value)
    {
        return new AggregateVersion(value);
    }

    /// <summary>
    /// Creates the a new aggregate ID.
    /// </summary>
    /// <returns>AggregateVersion.</returns>
    public static AggregateVersion CreateNew()
    {
        return new AggregateVersion(0);
    }

    /// <summary>
    /// Equalses the specified other.
    /// </summary>
    /// <param name="other">The other.</param>
    /// <returns>Boolean.</returns>
    public Boolean Equals(AggregateVersion other)
    {
        return this.Value.Equals(other.Value);
    }

    /// <summary>
    /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override Boolean Equals(Object obj)
    {
        if (object.ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is AggregateVersion && this.Equals((AggregateVersion)obj);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and aggregateEvent structures like a hash table.</returns>
    public override Int32 GetHashCode()
    {
        return this.Value.GetHashCode();
    }

    /// <summary>
    /// Parses the specified identifier.
    /// </summary>
    /// <param name="valueAsString">The value as string.</param>
    /// <returns>AggregateVersion.</returns>
    public static AggregateVersion Parse(String valueAsString)
    {
        return AggregateVersion.CreateFrom(Int64.Parse(valueAsString));
    }

    /// <summary>
    /// Returns the underlying value of this instance as a string.
    /// </summary>
    /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
    public override String ToString()
    {
        return this.Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="leftHand">The left hand.</param>
    /// <param name="rightHand">The right hand.</param>
    /// <returns>The result of the operator.</returns>
    public static Boolean operator ==(AggregateVersion leftHand,
                                      AggregateVersion rightHand)
    {
        return leftHand.Value == rightHand.Value;
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="AggregateVersion" /> to <see cref="Int32" />.
    /// </summary>
    /// <param name="aggregateVersion">The aggregate version.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Int64(AggregateVersion aggregateVersion)
    {
        return aggregateVersion.Value;
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="Int32" /> to <see cref="AggregateVersion" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator AggregateVersion(Int64 value)
    {
        return AggregateVersion.CreateFrom(value);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="leftHand">The left hand.</param>
    /// <param name="rightHand">The right hand.</param>
    /// <returns>The result of the operator.</returns>
    public static Boolean operator !=(AggregateVersion leftHand,
                                      AggregateVersion rightHand)
    {
        return leftHand.Value != rightHand.Value;
    }

    #endregion
}