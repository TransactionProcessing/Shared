namespace Shared.ValueObjects
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IEquatable&lt;Shared.ValueObjects.PositiveMoney&gt;" />
    public record PositiveMoney
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PositiveMoney"/> class.
        /// </summary>
        /// <param name="moneyAmount">The money amount.</param>
        private PositiveMoney(Money moneyAmount)
        {
            this.GuardMonetaryAmount(moneyAmount);
            this.Money = moneyAmount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public Decimal Value
        {
            get
            {
                return this.Money.Value;
            }
        }

        /// <summary>
        /// Gets the money.
        /// </summary>
        /// <value>
        /// The money.
        /// </value>
        private Money Money { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the specified money amount.
        /// </summary>
        /// <param name="moneyAmount">The money amount.</param>
        /// <returns></returns>
        public static PositiveMoney Create(Money moneyAmount)
        {
            return new PositiveMoney(moneyAmount);
        }
        
        /// <summary>
        /// Guards the monetary amount.
        /// </summary>
        /// <param name="moneyAmount">The money amount.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value of {moneyAmount.Value} is not a valid value</exception>
        private void GuardMonetaryAmount(Money moneyAmount)
        {
            if (moneyAmount.Value <= 0)
            {
                throw new ArgumentOutOfRangeException($"Value of {moneyAmount.Value} is not a valid value");
            }
        }

        #endregion
    }
}