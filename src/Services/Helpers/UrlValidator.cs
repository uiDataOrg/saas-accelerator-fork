using System;

namespace Marketplace.SaaS.Accelerator.Services.Helpers;

/// <summary>
/// URL Validator.
/// </summary>
public class UrlValidator
{
    /// <summary>
    /// Validates the URL for HTTPS.
    /// Helps validate if the URL is a valid HTTPS URL.
    /// </summary>
    public static bool IsValidUrlHttps(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
        {
            // Allow https://localhost:5001 specifically
            if (uriResult.Scheme == Uri.UriSchemeHttps &&
                uriResult.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                uriResult.Port == 5001)
            {
                return true;
            }

            // Default check for HTTPS and port 443
            return uriResult.Scheme == Uri.UriSchemeHttps && uriResult.Port == 443;
        }

        return false;
    }

}