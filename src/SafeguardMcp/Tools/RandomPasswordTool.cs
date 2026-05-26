using System.ComponentModel;
using System.Security.Cryptography;
using ModelContextProtocol.Server;

/// <summary>
/// Can be used to generate a random password locally. Some options for password complexity can be
/// specified. Otherwise, it will default to a length of 12, consisting of just letters and numbers.
/// </summary>
[McpServerToolType]
public class RandomPasswordTool
{
    /// <summary>Notice the absence of 0, 1, O, and L, both lower and upper case.</summary>
    private static readonly string PasswordChars = "abcdefghijkmnpqrstuvwxyzABCDEFGHIJKMNPQRSTUVWXYZ23456789";

    /// <summary>Just a small handful of what most things will hopefully consider "special characters".</summary>
    private static readonly string SpecialChars = "~!@#%^&*=+";

    [McpServerTool]
    [Description("""
        Can be used to generate a random password locally. Some options for password complexity can be specified.
        Otherwise, it will default to a length of 12, consisting of just letters and numbers.
        """
        )]
    public string GetRandomPassword(
        [Description("Number of characters to be generated for the password. It cannot be less than 4.")]
        int length = 12,

        [Description("Whether to include what are typically considered 'special characters' as part of the password or not.")]
        bool includeSpecialCharacter = false)
    {
        if (length < 4)
        {
            throw new ArgumentException("Password length cannot be less than 4.");
        }

        if (!includeSpecialCharacter)
        {
            return RandomNumberGenerator.GetString(PasswordChars, length);
        }

        var password = RandomNumberGenerator.GetString(PasswordChars, length - 2);
        password += RandomNumberGenerator.GetString(SpecialChars, 2);

        return password;
    }
}