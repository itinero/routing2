using System.Globalization;
using System.Text.RegularExpressions;

namespace Itinero.Profiles.Lua.ItineroLib
{
    internal static class Parsers
    {
        private const string RegexDecimal = @"\s*(\d+(?:\.\d*)?)\s*";
        private const string RegexUnitTons = @"\s*(t|to|tonnes|tonnen)?\s*";
        private const string RegexUnitMeters = @"\s*(m|meters|metres|meter)?\s*";
        private const string RegexDecimalWhiteSpace = @"\s*" + RegexDecimal + @"\s*"; 
        private const string RegexUnitKilometersPerHour = @"\s*(km/h|kmh|kph|kmph)?\s*";
        private const string RegexUnitKnots = @"\s*(knots)\s*";
        private const string RegexUnitMilesPerHour = @"\s*(mph)\s*";
        
        /// <summary>
        /// Tries to parse a weight value from a given tag-value.
        /// </summary>
        internal static bool TryParseWeight(string s, out float kilogram)
        {
            kilogram = float.MaxValue;

            if (string.IsNullOrWhiteSpace(s))
                return false;

            var tonnesRegex = new Regex("^" + RegexDecimal + RegexUnitTons + "$", RegexOptions.IgnoreCase);
            var tonnesMatch = tonnesRegex.Match(s);
            if (!tonnesMatch.Success) return false;
            kilogram = float.Parse(tonnesMatch.Groups[1].Value, CultureInfo.InvariantCulture) * 1000;
            return true;
        }

        /// <summary>
        /// Tries to parse a distance measure from a given tag-value.
        /// </summary>
        internal static bool TryParseLength(string s, out float meter)
        {
            meter = float.MaxValue;

            if (string.IsNullOrWhiteSpace(s))
                return false;

            var metresRegex = new Regex("^" + RegexDecimal + RegexUnitMeters + "$", RegexOptions.IgnoreCase);
            var metresMatch = metresRegex.Match(s);
            if (metresMatch.Success)
            {
                meter = float.Parse(metresMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                return true;
            }

            var feetInchesRegex = new Regex("^(\\d+)\\'(\\d+)\\\"$", RegexOptions.IgnoreCase);
            var feetInchesMatch = feetInchesRegex.Match(s);
            if (!feetInchesMatch.Success) return false;
            var feet = int.Parse(feetInchesMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var inches = int.Parse(feetInchesMatch.Groups[2].Value, CultureInfo.InvariantCulture);

            meter = feet * 0.3048f + inches * 0.0254f;
            return true;
        }

        /// <summary>
        /// Tries to parse a speed value from a given tag-value.
        /// </summary>
        internal static bool TryParseSpeed(string s, out float kmPerHour)
        {
            kmPerHour = float.MaxValue;

            if (string.IsNullOrWhiteSpace(s))
                return false;

            if (s[0] != '0' && s[0] != '1' && s[0] != '2' && s[0] != '3' && s[0] != '4' &&
                s[0] != '5' && s[0] != '6' && s[0] != '7' && s[0] != '8' && s[0] != '9')
            { // performance improvement, quick negative answer.
                return false;
            }

            if (s.Contains(","))
            { // refuse comma as a decimal separator or anywhere else in the number.
                return false;
            }

            // try regular speed: convention in OSM is km/h in this case.
            if (float.TryParse(s, NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kmPerHour))
            {
                return true;
            }

            // try km/h
            if (TryParseKilometerPerHour(s, out kmPerHour))
            {
                return true;
            }

            // try mph.
            if (TryParseMilesPerHour(s, out var milesPerHour))
            {
                kmPerHour = milesPerHour * 1.60934f;
                return true;
            }

            // try knots.
            if (TryParseKnots(s, out var resultKnots))
            {
                kmPerHour = resultKnots * 1.85200f;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to parse a string containing a kilometer per hour value.
        /// </summary>
        internal static bool TryParseKilometerPerHour(string s, out float kmPerHour)
        {
            s = s.ToStringEmptyWhenNull().Trim().ToLower();

            if (float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out kmPerHour))
            { // the value is just a numeric value.
                return true;
            }

            // do some more parsing work.
            var regex = new Regex("^" + RegexDecimalWhiteSpace +
                RegexUnitKilometersPerHour + "$", RegexOptions.IgnoreCase);
            var match = regex.Match(s);
            if (!match.Success) return false;
            kmPerHour = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Tries to parse a string containing a miles per hour value.
        /// </summary>
        internal static bool TryParseMilesPerHour(string s, out float milesPerHour)
        {
            milesPerHour = 0;
            float value;
            if (float.TryParse(s, out value))
            { // the value is just a numeric value.
                milesPerHour = value;
                return true;
            }

            // do some more parsing work.
            var regex = new Regex("^" + RegexDecimalWhiteSpace +
                RegexUnitMilesPerHour + "$", RegexOptions.IgnoreCase);
            var match = regex.Match(s);
            if (!match.Success) return false;
            milesPerHour = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Tries to parse a string containing knots.
        /// </summary>
        internal static bool TryParseKnots(string s, out float knots)
        {
            knots = 0;
            if (float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out knots))
            { // the value is just a numeric value.
                return true;
            }

            // do some more parsing work.
            var regex = new Regex("^" + RegexDecimalWhiteSpace + 
                RegexUnitKnots + "$", RegexOptions.IgnoreCase);
            var match = regex.Match(s);
            if (!match.Success) return false;
            knots = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return true;
        }
        
        
        /// <summary>
        /// Returns the result of the ToString() method or an empty string
        /// when the given object is null.
        /// </summary>
        internal static string ToStringEmptyWhenNull(this object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            return obj.ToString();
        }
    }
}