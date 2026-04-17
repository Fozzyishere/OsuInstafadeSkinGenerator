using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OsuInstaFadeSkinGenerator.Application.Ports;
using OsuInstaFadeSkinGenerator.Models;

namespace OsuInstaFadeSkinGenerator.Infrastructure.SkinIni;

public sealed class SkinIniWriter : ISkinIniWriter
{
    private readonly IFileSystem fileSystem;

    public SkinIniWriter(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public async Task UpdateAsync(string skinIniPath, RgbColor comboColor, int hitCircleOverlap, CancellationToken cancellationToken)
    {
        var lines = (await this.fileSystem.ReadAllLinesAsync(skinIniPath, cancellationToken).ConfigureAwait(false)).ToList();
        var result = new List<string>();
        var comboValue = $"{comboColor.R},{comboColor.G},{comboColor.B}";
        var indent = SkinIniCommon.DetectIndent(lines);
        string currentSection = string.Empty;
        bool combo1Written = false;
        bool overlapWritten = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            if (SkinIniCommon.TryGetSectionName(trimmed, out var sectionName))
            {
                FlushSection(
                    result,
                    currentSection,
                    indent,
                    comboValue,
                    hitCircleOverlap,
                    ref combo1Written,
                    ref overlapWritten);
                currentSection = sectionName;
            }

            if (currentSection == SkinIniCommon.FontsSection
                && trimmed.StartsWith("HitCircleOverlap", StringComparison.OrdinalIgnoreCase))
            {
                result.Add($"{SkinIniCommon.GetLineIndent(lines[i])}HitCircleOverlap: {hitCircleOverlap}");
                overlapWritten = true;
                continue;
            }

            if (currentSection == SkinIniCommon.ColoursSection && SkinIniCommon.ComboLineRegex().IsMatch(trimmed))
            {
                if (!combo1Written)
                {
                    result.Add($"{SkinIniCommon.GetLineIndent(lines[i])}Combo1: {comboValue}");
                    combo1Written = true;
                }

                continue;
            }

            result.Add(lines[i]);
        }

        FlushSection(
            result,
            currentSection,
            indent,
            comboValue,
            hitCircleOverlap,
            ref combo1Written,
            ref overlapWritten);

        if (!combo1Written)
        {
            AppendNewSection(result, "[Colours]", $"Combo1: {comboValue}", indent);
        }

        if (!overlapWritten)
        {
            AppendNewSection(result, "[Fonts]", $"HitCircleOverlap: {hitCircleOverlap}", indent);
        }

        await this.fileSystem.WriteAllLinesAsync(skinIniPath, result, cancellationToken).ConfigureAwait(false);
    }

    private static void FlushSection(
        List<string> result,
        string section,
        string indent,
        string comboValue,
        int hitCircleOverlap,
        ref bool combo1Written,
        ref bool overlapWritten)
    {
        if (section == SkinIniCommon.FontsSection && !overlapWritten)
        {
            result.Add($"{indent}HitCircleOverlap: {hitCircleOverlap}");
            overlapWritten = true;
        }

        if (section == SkinIniCommon.ColoursSection && !combo1Written)
        {
            result.Add($"{indent}Combo1: {comboValue}");
            combo1Written = true;
        }
    }

    private static void AppendNewSection(List<string> result, string header, string keyLine, string indent)
    {
        result.Add(string.Empty);
        result.Add(header);
        result.Add($"{indent}{keyLine}");
    }
}
