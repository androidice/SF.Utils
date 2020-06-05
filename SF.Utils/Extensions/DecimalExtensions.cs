using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization; 

namespace SF.Utils.Extensions
{
    public static class DecimalExtensions
    {
        /// <summary>
        /// Convert to currency and support localization
        /// default supported culture English (United States) en-US
        /// Please see link https://docs.sdl.com/LiveContent/content/en-US/SDL_MediaManager_241/concept_A9F20DF9433C46FF8FED8FA11A29FAA0
        /// for the list of supported language code
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static string FormatToCurrency(this decimal input, CultureInfo cultureInfo = null) {
            if (cultureInfo == null)
                cultureInfo = new CultureInfo("en-US");

            return string.Format(cultureInfo, "{0:C}", input);
        }
           

        public static string ConvertToNumericFormat(this decimal input) =>
            input.ToString("#,0");
    }
}
