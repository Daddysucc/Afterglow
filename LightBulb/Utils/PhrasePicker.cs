using System;
using System.Collections.Generic;

namespace LightBulb.Utils;

/// <summary>
/// Picks a random phrase for the current category and keeps showing it until the category changes.
/// When a category comes around again, the phrase that was last shown for it is not repeated.
/// </summary>
public class PhrasePicker<TCategory>(Random random)
    where TCategory : notnull
{
    private readonly Dictionary<TCategory, int> _lastIndices = new();
    private TCategory? _currentCategory;
    private bool _hasCurrentCategory;

    public PhrasePicker()
        : this(Random.Shared) { }

    public string Get(TCategory category, IReadOnlyList<string> phrases)
    {
        if (phrases.Count == 0)
            return string.Empty;

        var isNewCategory =
            !_hasCurrentCategory
            || !EqualityComparer<TCategory>.Default.Equals(_currentCategory, category);

        if (isNewCategory || !_lastIndices.ContainsKey(category))
        {
            var index = random.Next(phrases.Count);

            // Don't repeat the phrase that was shown the last time this category was active
            if (
                phrases.Count > 1
                && _lastIndices.TryGetValue(category, out var previousIndex)
                && index == previousIndex
            )
            {
                index = (index + 1 + random.Next(phrases.Count - 1)) % phrases.Count;
            }

            _lastIndices[category] = index;
            _currentCategory = category;
            _hasCurrentCategory = true;
        }

        // The list may have changed in the meantime (e.g. a different language), so stay in range
        return phrases[Math.Min(_lastIndices[category], phrases.Count - 1)];
    }
}
