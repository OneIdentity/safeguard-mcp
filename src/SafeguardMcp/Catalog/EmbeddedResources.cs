using System.Reflection;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Loads embedded markdown resources packaged under <c>SafeguardMcp.Catalog.Resources.*.md</c>.
/// Resource classes call <see cref="Load"/> once into a <c>static readonly</c> field so the
/// content is materialized at first access and reused for every subsequent request.
/// </summary>
internal static class EmbeddedResources
{
    private const string ResourcePrefix = "SafeguardMcp.Catalog.Resources.";

    public static string Load(string fileName)
    {
        var resourceName = ResourcePrefix + fileName;
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found. Check that the .md file is included as <EmbeddedResource> in SafeguardMcp.csproj.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
