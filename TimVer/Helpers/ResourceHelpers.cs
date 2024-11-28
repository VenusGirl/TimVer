// Copyright (c) Tim Kennedy. All Rights Reserved. Licensed under the MIT License.

namespace TimVer.Helpers;

internal static class ResourceHelpers
{
    #region Get count of strings in resource dictionary
    /// <summary>
    /// Gets the count of strings in the default resource dictionary.
    /// </summary>
    /// <returns>Count as int.</returns>
    public static int GetTotalDefaultLanguageCount()
    {
        ResourceDictionary dictionary = new()
        {
            Source = new Uri("Languages/Strings.en-US.xaml", UriKind.RelativeOrAbsolute)
        };
        return dictionary.Count;
    }
    #endregion Get count of strings in resource dictionary

    #region Get a resource string
    /// <summary>
    /// Gets the string resource for the key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>String</returns>
    /// <remarks>
    /// Want to throw here so that missing resource doesn't make it into a release.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Resource description is null.</exception>
    /// <exception cref="ArgumentException">Resource was not found.</exception>
    public static string GetStringResource(string key)
    {
        object description;
        try
        {
            description = Application.Current.TryFindResource(key);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                throw new ArgumentException($"Resource not found: {key}");
            }
            else
            {
                _log.Error(ex, $"Resource not found: {key}");
                return $"Resource not found: {key}";
            }
        }

        if (description is null)
        {
            if (Debugger.IsAttached)
            {
                throw new ArgumentNullException($"Resource not found: {key}");
            }
            else
            {
                _log.Error($"Resource not found: {key}");
                return $"Resource not found: {key}";
            }
        }

        return description.ToString()!;
    }
    #endregion Get a resource string

    #region Get composite format for a resource string
    private static CompositeFormat GetCompositeResource(string key)
    {
        try
        {
            return CompositeFormat.Parse(GetStringResource(key));
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error creating composite format for key: {key}");
            return CompositeFormat.Parse($"Error creating composite format for key: {key}");
        }
    }
    #endregion Get composite format for a resource string

    #region Composite format properties
    internal static CompositeFormat HardwareInfoUptimeString { get; } = GetCompositeResource("HardwareInfo_UptimeString");
    internal static CompositeFormat MsgTextAppUpdateNewerFound { get; } = GetCompositeResource("MsgText_AppUpdateNewerFound");
    internal static CompositeFormat MsgTextErrorOpeningFile { get; } = GetCompositeResource("MsgText_ErrorOpeningFile");
    internal static CompositeFormat MsgTextErrorReadingFile { get; } = GetCompositeResource("MsgText_ErrorReadingFile");
    internal static CompositeFormat MsgTextFilterRowsShown { get; } = GetCompositeResource("MsgText_FilterRowsShown");
    internal static CompositeFormat MsgTextUIColorSet { get; } = GetCompositeResource("MsgText_UIColorSet");
    internal static CompositeFormat MsgTextUISizeSet { get; } = GetCompositeResource("MsgText_UISizeSet");
    internal static CompositeFormat MsgTextUIThemeSet { get; } = GetCompositeResource("MsgText_UIThemeSet");
    #endregion Composite format properties

    #region Compute percentage of language strings
    /// <summary>
    /// Compute percentage of strings by dividing the number of strings
    /// for the supplied language by the total of en-US strings.
    /// </summary>
    /// <param name="language">Language code</param>
    /// <returns>The percentage with no decimal places as a string. Includes the "%".</returns>
    public static string GetLanguagePercent(string language)
    {
        ResourceDictionary dictionary = [];
        try
        {
            dictionary.Source = new Uri($"Languages/Strings.{language}.xaml", UriKind.RelativeOrAbsolute);
            int totalCount = GetTotalDefaultLanguageCount();
            if (totalCount == 0)
            {
                _log.Error("GetLanguagePercent totalCount is 0 for default dictionary");
                return GetStringResource("MsgText_Error_Caption");
            }
            if (dictionary.Count == 0)
            {
                _log.Error($"GetLanguagePercent Count is 0 for {dictionary.Source}");
                return GetStringResource("MsgText_Error_Caption");
            }
            double percent = (double)dictionary.Count / totalCount;
            percent = Math.Round(percent, 2, MidpointRounding.ToZero);
            return percent.ToString("P0", CultureInfo.InvariantCulture);
        }
        catch (IOException ex)
        {
            _log.Error(ex, $"IO exception in GetLanguagePercent for {dictionary.Source}");
            return GetStringResource("MsgText_Error_Caption");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error in GetLanguagePercent for {dictionary.Source}");
            return GetStringResource("MsgText_Error_Caption");
        }
    }
    #endregion Compute percentage of language strings

    #region Compare language dictionaries
    /// <summary>
    /// Compares language resource dictionaries to find missing keys
    /// </summary>
    public static void CompareLanguageDictionaries()
    {
        string currentLanguage = Thread.CurrentThread.CurrentCulture.Name;
        string compareLang = $"Languages/Strings.{currentLanguage}.xaml";

        ResourceDictionary dict1 = [];
        ResourceDictionary dict2 = [];

        dict1.Source = new Uri("Languages/Strings.en-US.xaml", UriKind.RelativeOrAbsolute);
        dict2.Source = new Uri(compareLang, UriKind.RelativeOrAbsolute);
        _log.Info($"Comparing {dict1.Source} and {dict2.Source}");

        Dictionary<string, string> enUSDict = [];
        Dictionary<string, string> compareDict = [];

        foreach (DictionaryEntry kvp in dict1)
        {
            enUSDict.Add(kvp.Key.ToString()!, kvp.Value!.ToString()!);
        }
        foreach (DictionaryEntry kvp in dict2)
        {
            compareDict.Add(kvp.Key.ToString()!, kvp.Value!.ToString()!);
        }

        bool same = enUSDict.Count == compareDict.Count && enUSDict.Keys.SequenceEqual(compareDict.Keys);

        if (same)
        {
            _log.Info($"{dict1.Source} and {dict2.Source} have the same keys");
        }
        else
        {
            if (enUSDict.Keys.Except(compareDict.Keys).Any())
            {
                _log.Info(new string('-', 80));
                _log.Warn($"[{AppInfo.AppName}] {dict2.Source} is missing the following keys");
                foreach (string item in enUSDict.Keys.Except(compareDict.Keys).Order())
                {
                    _log.Warn($"Key: {item}    Value: \"{GetStringResource(item)}\"");
                }
                _log.Info(new string('-', 80));
            }

            if (compareDict.Keys.Except(enUSDict.Keys).Any())
            {
                _log.Warn($"[{AppInfo.AppName}] {dict2.Source} has keys that {dict1.Source} does not have.");
                foreach (string item in compareDict.Keys.Except(enUSDict.Keys).Order())
                {
                    _log.Warn($"Key: {item}    Value: \"{GetStringResource(item)}\"");
                }
                _log.Info(new string('-', 80));
            }
        }
    }
    #endregion Compare language dictionaries

}
