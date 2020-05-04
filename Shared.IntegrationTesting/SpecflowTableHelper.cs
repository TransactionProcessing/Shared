namespace Shared.IntegrationTesting
{
    using System;
    using TechTalk.SpecFlow;

    /// <summary>
    /// 
    /// </summary>
    public static class SpecflowTableHelper
    {
        #region Methods

        /// <summary>
        /// Gets the boolean value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static Boolean GetBooleanValue(TableRow row,
                                              String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return bool.TryParse(field, out Boolean value) && value;
        }

        /// <summary>
        /// Gets the date for date string.
        /// </summary>
        /// <param name="dateString">The date string.</param>
        /// <param name="today">The today.</param>
        /// <returns></returns>
        public static DateTime GetDateForDateString(String dateString,
                                                    DateTime today)
        {
            switch(dateString.ToUpper())
            {
                case "TODAY":
                    return today.Date;
                case "YESTERDAY":
                    return today.AddDays(-1).Date;
                case "LASTWEEK":
                    return today.AddDays(-7).Date;
                case "LASTMONTH":
                    return today.AddMonths(-1).Date;
                case "LASTYEAR":
                    return today.AddYears(-1).Date;
                case "TOMORROW":
                    return today.AddDays(1).Date;
                default:
                    return DateTime.Parse(dateString);
            }
        }

        /// <summary>
        /// Gets the decimal value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static Decimal GetDecimalValue(TableRow row,
                                              String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return decimal.TryParse(field, out Decimal value) ? value : -1;
        }

        /// <summary>
        /// Gets the int value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static Int32 GetIntValue(TableRow row,
                                        String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return int.TryParse(field, out Int32 value) ? value : -1;
        }

        /// <summary>
        /// Gets the short value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static Int16 GetShortValue(TableRow row,
                                          String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            if (short.TryParse(field, out Int16 value))
            {
                return value;
            }

            return -1;
        }

        /// <summary>
        /// Gets the string row value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static String GetStringRowValue(TableRow row,
                                               String key)
        {
            return row.TryGetValue(key, out String value) ? value : "";
        }

        #endregion
    }
}