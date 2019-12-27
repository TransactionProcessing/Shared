using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared.General
{
    public static class Guard
    {
        #region Public Methods

        #region public static void ThrowIfNull(Object argumentValue, String argumentName)

        /// <summary>
        /// Throws if null.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfNull(Object argumentValue, String argumentName)
        {
            // Check if the object is null
            if (argumentValue == null)
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #region public static void ThrowIfNull(Object value, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if null or empty.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        public static void ThrowIfNull(Object value, Type exceptionType, String exceptionMessage)
        {
            Guard.ThrowIfNullOrEmpty(exceptionMessage, nameof(exceptionMessage));

            if (value == null)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNullOrEmpty(String argumentValue, string argumentName)

        /// <summary>
        /// Throws if null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfNullOrEmpty(String argumentValue, String argumentName)
        {
            // Check if the string is null or empty
            if (String.IsNullOrEmpty(argumentValue))
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #region public static void ThrowIfNullOrEmpty(String value, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if null or empty.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        public static void ThrowIfNullOrEmpty(String value, Type exceptionType, String exceptionMessage)
        {
            Guard.ThrowIfNullOrEmpty(exceptionMessage, nameof(exceptionMessage));

            if (String.IsNullOrEmpty(value))
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfInvalidGuid(Guid argumentValue, string argumentName)

        /// <summary>
        /// Throws if invalid unique identifier.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfInvalidGuid(Guid argumentValue, String argumentName)
        {
            // Check if the Guid is null or empty
            if (argumentValue == null || argumentValue == Guid.Empty)
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #region public static void ThrowIfInvalidGuid(Guid argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if invalid unique identifier.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfInvalidGuid(Guid argumentValue, Type exceptionType, String exceptionMessage)
        {
            // Check if the Guid is null or empty
            if (argumentValue == null || argumentValue == Guid.Empty)
            {
                // Throw an exception 
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfInvalidEnum(Type enumType, object value, string argumentName)

        /// <summary>
        /// Throws if invalid enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="value">The value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static void ThrowIfInvalidEnum(Type enumType, Object value, String argumentName)
        {
            // Check if the enum is valid
            if (!Enum.IsDefined(enumType, value))
            {
                throw new ArgumentOutOfRangeException(argumentName, $"Value specified [{value}]");
            }
        }

        #endregion

        #region public static void ThrowIfInvalidEnum(Type enumType, object value, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if invalid enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="value">The value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static void ThrowIfInvalidEnum(Type enumType, Object value, Type exceptionType, String exceptionMessage)
        {
            // Check if the enum is valid
            if (!Enum.IsDefined(enumType, value))
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Int32 argumentValue, String argumentName)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Int32 argumentValue, String argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be less than zero");
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Int32 argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Int32 argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue < 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfZero(Int32 argumentValue, String argumentName)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Int32 argumentValue, String argumentName)
        {
            if (argumentValue == 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be equal zero");
            }
        }

        #endregion

        #region public static void ThrowIfZero(Int32 argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Int32 argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue == 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Double argumentValue, String argumentName)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Double argumentValue, String argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be less than zero");
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Double argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Double argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue < 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfZero(Double argumentValue, String argumentName)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Double argumentValue, String argumentName)
        {
            if (argumentValue == 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be equal zero");
            }
        }

        #endregion

        #region public static void ThrowIfZero(Double argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Double argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue == 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Decimal argumentValue, String argumentName)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Decimal argumentValue, String argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be less than zero");
            }
        }

        #endregion

        #region public static void ThrowIfNegative(Decimal argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if negative.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be less than zero</exception>
        public static void ThrowIfNegative(Decimal argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue < 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfZero(Decimal argumentValue, String argumentName)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Decimal argumentValue, String argumentName)
        {
            if (argumentValue == 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, "Value cannot be equal zero");
            }
        }

        #endregion

        #region public static void ThrowIfZero(Decimal argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if zero.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Value cannot be equal zero</exception>
        public static void ThrowIfZero(Decimal argumentValue, Type exceptionType, String exceptionMessage)
        {
            if (argumentValue == 0)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNullOrEmpty(String[] argumentValue, String argumentName)

        /// <summary>
        /// Throws if the array is null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfNullOrEmpty(String[] argumentValue, String argumentName)
        {
            // Array cant be null or empty
            if (argumentValue == null || !argumentValue.Any())
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #region public static void ThrowIfNullOrEmpty(String[] argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if the array is null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfNullOrEmpty(String[] argumentValue, Type exceptionType, String exceptionMessage)
        {
            // Array cant be null or empty
            if (argumentValue == null || !argumentValue.Any())
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfContainsNullOrEmpty(String[] argumentValue, String argumentName)

        /// <summary>
        /// Throws if any of the array elements are null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfContainsNullOrEmpty(String[] argumentValue, String argumentName)
        {
            // Check each element of the array to ensure it isn't a null or empty string
            foreach (var value in argumentValue)
            {
                // Throw an exception if not valid
                Guard.ThrowIfNullOrEmpty(value, nameof(argumentName));
            }
        }

        #endregion

        #region public static void ThrowIfContainsNullOrEmpty(String[] argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if any of the array elements are null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfContainsNullOrEmpty(String[] argumentValue, Type exceptionType,
            String exceptionMessage)
        {
            // Check each element of the array to ensure it isn't a null or empty string
            foreach (var value in argumentValue)
            {
                Guard.ThrowIfNullOrEmpty(value, exceptionType, exceptionMessage);
            }
        }

        #endregion

        #region public static void ThrowIfInvalidDate(DateTime argumentValue, String argumentName)

        /// <summary>
        /// Throws if invalid date.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void ThrowIfInvalidDate(DateTime argumentValue, String argumentName)
        {
            // Check if the Date is min or max value
            if (argumentValue == DateTime.MinValue)
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #region public static void ThrowIfInvalidDate(DateTime argumentValue, Type exceptionType, String exceptionMessage)

        /// <summary>
        /// Throws if invalid date.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="exceptionMessage">The exception message.</param>
        public static void ThrowIfInvalidDate(DateTime argumentValue, Type exceptionType, String exceptionMessage)
        {
            // Check if the Date is min or max value
            if (argumentValue == DateTime.MinValue)
            {
                var ex = (Exception) Activator.CreateInstance(exceptionType, exceptionMessage);
                throw ex;
            }
        }

        #endregion

        #region public static void ThrowIfNullOrEmpty(Byte[] argumentValue, String argumentName)        

        /// <summary>
        /// Throws if null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ThrowIfNullOrEmpty(Byte[] argumentValue, String argumentName)
        {
            // Array cant be null or empty
            if (argumentValue == null || !argumentValue.Any())
            {
                // Throw an exception
                throw new ArgumentNullException(argumentName);
            }
        }

        #endregion

        #endregion
    }
}
