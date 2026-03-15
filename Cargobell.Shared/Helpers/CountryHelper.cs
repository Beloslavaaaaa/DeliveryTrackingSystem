namespace Cargobell.Shared.Helpers
{
    public static class CountryHelper
    {
        private static readonly Dictionary<string, string> Codes = new()
        {
            { "1", "United States / Canada" },
            { "44", "United Kingdom" },
            { "359", "Bulgaria" },
            { "49", "Germany" },
            { "33", "France" },
            { "971", "United Arab Emirates" },
            { "41", "Switzerland" },
            { "31", "Netherlands" },
            { "34", "Spain" }
        };

        public static string GetCitizenship(string? fullPhoneNumber)
        {
            if (string.IsNullOrEmpty(fullPhoneNumber)) return "Global Citizen";
            string clean = fullPhoneNumber.StartsWith("+") ? fullPhoneNumber.Substring(1) : fullPhoneNumber;
            for (int i = 3; i >= 1; i--)
            {
                if (clean.Length >= i)
                {
                    string prefix = clean.Substring(0, i);
                    if (Codes.ContainsKey(prefix)) return Codes[prefix];
                }
            }
            return "International Elite";
        }
    }
}