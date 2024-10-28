using Microsoft.Extensions.Logging;

namespace Agoda.GrapqhlGen;

public static class RegionParser
{
    public static IEnumerable<Region> Parse(string filePath, ILogger? logger = null)
    {
        logger ??= LoggerFactory.Create(builder => builder.AddConsole())
                               .CreateLogger(typeof(RegionParser).FullName!);

        if (!File.Exists(filePath))
        {
            logger.LogError("File does not exist: {FilePath}", filePath);
            return Enumerable.Empty<Region>();
        }

        var fileContent = File.ReadAllText(filePath);
        logger.LogDebug("File content before processing:{NewLine}{FileContent}",
            Environment.NewLine, fileContent);

        var regions = new List<Region>();
        var lines = File.ReadAllLines(filePath);
        var currentRegion = "";
        var currentLines = new List<string>();
        var imports = new List<string>();
        var inRegion = false;

        foreach (var line in lines)
        {
            logger.LogTrace("Processing line: '{Line}'", line);

            if (line.TrimStart().StartsWith("#region"))
            {
                inRegion = true;
                currentRegion = line.TrimStart().Replace("#region", "").Trim();
                logger.LogDebug("Starting region: '{RegionName}'", currentRegion);
                continue;
            }

            if (line.TrimStart().StartsWith("#endregion"))
            {
                if (inRegion)
                {
                    logger.LogDebug("Ending region: '{RegionName}' with {LineCount} lines",
                        currentRegion, currentLines.Count);
                    regions.Add(new Region(currentRegion, imports.Concat(currentLines).ToList()));
                    currentLines = new List<string>();
                    inRegion = false;
                }
                continue;
            }

            if (!inRegion)
            {
                imports.Add(line);
            }
            else
            {
                currentLines.Add(line);
            }
        }

        logger.LogInformation("Found {RegionCount} regions", regions.Count);

        foreach (var region in regions)
        {
            logger.LogDebug(
                "Region: '{RegionName}' with {LineCount} lines{NewLine}Content:{NewLine}{Content}",
                region.Name,
                region.Lines.Count,
                Environment.NewLine,
                region.GetSourceCode());
        }

        return regions;
    }
}