using used_car_predictor.Backend.Evaluation;

namespace used_car_predictor.Backend.Services;

public static class BundleId
{
    private static string N(string s) => ModelNormalizer.Normalize(s ?? string.Empty);

    private static string Canon(string? s)
    {
        var x = (s ?? string.Empty).Trim();
        x = x.Replace(' ', '_').Replace('-', '_');
        while (x.Contains("__")) x = x.Replace("__", "_");
        x = x.Trim('_');
        return x;
    }
    
    public static string From(string manufacturer, string model)
    {
        var make = Canon(N(manufacturer));
        var mod  = Canon(N(model));

        var needle = make + "_";
        
        var idx = mod.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
        {
            var before = Canon(mod[..idx]);
            var after  = Canon(mod[idx..]); 
            mod = string.IsNullOrEmpty(before) ? after : $"{after}_{before}";
        }
        
        if (!mod.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
            mod = $"{make}_{mod}";
        
        mod = Canon(mod);
        return mod;
    }
    
    public static class BundleLabel
    {
        public static string From(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return model;
            var clean = model.Replace('_', ' ').Trim();
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(clean.ToLowerInvariant());
        }
    }
}