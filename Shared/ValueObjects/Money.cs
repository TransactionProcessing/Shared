namespace Shared.ValueObjects
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IEquatable&lt;Shared.ValueObjects.Money&gt;" />
    public record Money
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Money"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        private Money(Decimal value)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public Decimal Value { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Money Create(Decimal value)
        {
            return new Money(value);
        }

        /// <summary>
        /// Implements the operator +.
        /// </summary>
        /// <param name="leftHand">The left hand.</param>
        /// <param name="rightHand">The right hand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static Money operator +(Money leftHand,
                                       Money rightHand)
        {
            return new Money(leftHand.Value + rightHand.Value);
        }

        /// <summary>
        /// Implements the operator -.
        /// </summary>
        /// <param name="leftHand">The left hand.</param>
        /// <param name="rightHand">The right hand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static Money operator -(Money leftHand,
                                       Money rightHand)
        {
            return new Money(leftHand.Value - rightHand.Value);
        }

        #endregion
    }
}