﻿namespace Shared.IntegrationTesting
{
    using System;
    using TechTalk.SpecFlow;

    /// <summary>
    /// 
    /// </summary>
    public static class SpecflowTableHelper
    {
        #region Methods

        public static Boolean GetBooleanValue(TableRow row,
                                              String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return bool.TryParse(field, out Boolean value) && value;
        }

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

        public static Decimal GetDecimalValue(TableRow row,
                                              String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return decimal.TryParse(field, out Decimal value) ? value : -1;
        }

        public static Int32 GetIntValue(TableRow row,
                                        String key)
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return int.TryParse(field, out Int32 value) ? value : -1;
        }

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

        /// <returns></returns>
        public static String GetStringRowValue(TableRow row,
                                               String key)
        {
            return row.TryGetValue(key, out String value) ? value : "";
        }
        
        public static T GetEnumValue<T>(TableRow row,
                                        String key) where T : struct
        {
            String field = SpecflowTableHelper.GetStringRowValue(row, key);

            return Enum.Parse<T>(field, true);
        }


        #endregion
    }
}