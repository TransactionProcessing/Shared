namespace Shared.EventStore.Aggregate;

using System;
using System.Globalization;

public struct AggregateVersion : IComparable
{

    public Int64 Value { get; init; }

    public Int32 CompareTo(Object obj)
    {
        AggregateVersion otherVersion = (AggregateVersion)obj;
        return this.Value.CompareTo(otherVersion.Value);
    }

    public static AggregateVersion CreateFrom(Int64 value)
    {
        return new AggregateVersion { Value = value };
    }

    public static AggregateVersion CreateNew()
    {
        return new AggregateVersion { Value = 0 };
    }

    public Boolean Equals(AggregateVersion other)
    {
        return this.Value.Equals(other.Value);
    }

    public override Boolean Equals(Object obj)
    {
        if (object.ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is AggregateVersion && this.Equals((AggregateVersion)obj);
    }

    public override Int32 GetHashCode()
    {
        return this.Value.GetHashCode();
    }

    public static AggregateVersion Parse(String valueAsString)
    {
        return AggregateVersion.CreateFrom(Int64.Parse(valueAsString));
    }

    public override String ToString()
    {
        return this.Value.ToString(CultureInfo.InvariantCulture);
    }

    public static Boolean operator ==(AggregateVersion leftHand,
                                      AggregateVersion rightHand)
    {
        return leftHand.Value == rightHand.Value;
    }

    public static implicit operator Int64(AggregateVersion aggregateVersion)
    {
        return aggregateVersion.Value;
    }

    public static implicit operator AggregateVersion(Int64 value)
    {
        return AggregateVersion.CreateFrom(value);
    }

    public static Boolean operator !=(AggregateVersion leftHand,
                                      AggregateVersion rightHand)
    {
        return leftHand.Value != rightHand.Value;
    }

    public static bool operator <(AggregateVersion left, AggregateVersion right) =>
        left.Value < right.Value;

    public static bool operator >(AggregateVersion left, AggregateVersion right) =>
        left.Value > right.Value;

    public static bool operator <=(AggregateVersion left, AggregateVersion right) =>
        left.Value <= right.Value;

    public static bool operator >=(AggregateVersion left, AggregateVersion right) =>
        left.Value >= right.Value;
}