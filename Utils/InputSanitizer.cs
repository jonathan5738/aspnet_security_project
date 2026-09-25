namespace SecurityProject.Utils;
using System.Text.RegularExpressions;
public static class InputSanitizer
{
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string sanitized = input;

        // Remove script tags
        sanitized = Regex.Replace(sanitized, "<script.*?>.*?</script>", "", RegexOptions.IgnoreCase);

        // Remove HTML tags
        sanitized = Regex.Replace(sanitized, "<.*?>", "");

        // Remove SQL keywords
        sanitized = Regex.Replace(sanitized, @"\b(SELECT|INSERT|DELETE|UPDATE|DROP|ALTER|CREATE|EXEC|UNION)\b",
                                  "", RegexOptions.IgnoreCase);

        // Remove special characters often used in attacks
        sanitized = Regex.Replace(sanitized, @"['"";]", "");

        // Trim whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }
}
