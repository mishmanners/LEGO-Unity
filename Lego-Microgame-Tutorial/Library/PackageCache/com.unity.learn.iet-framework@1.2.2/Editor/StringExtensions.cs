namespace Unity.InteractiveTutorials
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
        public static bool IsNotNullOrEmpty(this string self) => !self.IsNullOrEmpty();

        public static bool IsNullOrWhitespace(this string self) => string.IsNullOrWhiteSpace(self);
        public static bool IsNotNullOrWhitespace(this string self) => !self.IsNullOrWhitespace();

        public static string AsNullIfWhitespace(this string self) => string.IsNullOrWhiteSpace(self) ? null : self;
        public static string AsNullIfEmpty(this string self) => self.IsNullOrEmpty() ? null : self;

        public static string AsEmptyIfNull(this string self) => self ?? string.Empty;
    }

    public static class StringExt
    {
        public static bool IsNotNullOrEmpty(string str) => !str.IsNullOrEmpty();
        public static bool IsNotNullOrWhitespace(string str) => !string.IsNullOrWhiteSpace(str);
    }
}
