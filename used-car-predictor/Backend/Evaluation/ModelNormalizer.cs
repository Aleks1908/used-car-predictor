using System.Text.RegularExpressions;

namespace used_car_predictor.Backend.Evaluation
{
    /// Provides a consistent way to normalize manufacturer and model names
    /// for file names, comparisons, and lookups.
    /// Used throughout the training and prediction pipelines.
    public static class ModelNormalizer
    {
        public static string Normalize(string? model, bool sortTokens = true)
        {
            if (string.IsNullOrWhiteSpace(model))
                return "";

            var cleaned = model.Trim().ToLower();

            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            var tokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (sortTokens)
                Array.Sort(tokens, StringComparer.Ordinal);

            return string.Join(" ", tokens);
        }
    }
}