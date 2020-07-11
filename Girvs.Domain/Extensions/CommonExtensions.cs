using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Girvs.Domain.Extensions
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class CommonExtensions
    {
        /// <summary>
        /// 返回一个值，该值指示是否不可能进行真正的选择
        /// </summary>
        /// <param name="items">Items</param>
        /// <param name="ignoreZeroValue">A value indicating whether we should ignore items with "0" value</param>
        /// <returns>A value indicating whether real selection is not possible</returns>
        public static bool SelectionIsNotPossible(this IList<SelectListItem> items, bool ignoreZeroValue = true)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            //we ignore items with "0" value? Usually it's something like "Select All", "etc
            return items.Count(x => !ignoreZeroValue || !x.Value.ToString().Equals("0")) < 2;
        }

        /// <summary>
        /// DateTime的相对格式（例如2小时前，一个月前）
        /// </summary>
        /// <param name="source">Source (UTC format)</param>
        /// <param name="languageCode">Language culture code</param>
        /// <returns>Formatted date and time string</returns>
        public static string RelativeFormat(this DateTime source, string languageCode = "en-US")
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - source.Ticks);
            var delta = ts.TotalSeconds;

            CultureInfo culture = null;
            try
            {
                culture = new CultureInfo(languageCode);
            }
            catch (CultureNotFoundException)
            {
                culture = new CultureInfo("en-US");
            }
            return TimeSpan.FromSeconds(delta).Humanize(precision: 1, culture: culture, maxUnit: TimeUnit.Year);
        }
    }
}