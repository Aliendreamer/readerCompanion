using System.Text.RegularExpressions;

namespace ReaderCompanion.Core.Library;

/// <summary>The app's genres: BISAC's main headings (the publishing industry's subject list), in plain names. File
/// subjects are mapped onto them by their words or their BISAC codes; anything that is not one of these genres
/// ("Fiction", "General", a series, a place) is dropped.</summary>
public static partial class GenreMap
{
    // Each genre with the words (folded: lower case, letters and digits only) and BISAC code prefixes that mean it.
    private static readonly (string Genre, string[] Words, string[] Codes)[] Genres =
    [
        ("Fantasy", ["fantasy", "magic", "sword and sorcery", "dragons", "wizards", "forgotten realms", "dragonlance", "drizzt", "shannara"], ["FIC009"]),
        ("Science Fiction", ["science fiction", "sci fi", "scifi", "sf", "space opera", "cyberpunk", "dystopia", "dystopian", "time travel", "aliens", "star wars", "star trek", "post apocalyptic", "apocalyptic"], ["FIC028"]),
        ("Horror", ["horror", "vampire", "vampires", "zombie", "zombies", "ghost", "ghosts", "occult"], ["FIC015"]),
        ("Mystery & Crime", ["mystery", "mysteries", "detective", "crime", "noir"], ["FIC022", "FIC050"]),
        ("Thriller", ["thriller", "thrillers", "suspense", "espionage", "spy"], ["FIC031", "FIC006"]),
        ("Romance", ["romance", "romantic"], ["FIC027"]),
        ("Historical Fiction", ["historical"], ["FIC014"]),
        ("Action & Adventure", ["adventure"], ["FIC002"]),
        ("War & Military", ["war", "military", "world war"], ["FIC032"]),
        ("Literary Fiction", ["literary"], ["FIC019"]),
        ("Humour", ["humor", "humour", "humorous", "comedy", "satire"], ["FIC016", "HUM"]),
        ("Short Stories", ["short stories", "anthology", "anthologies"], ["FIC029"]),
        ("Classics", [], ["FIC004"]), // only whole subjects: "classic fantasy" is fantasy (see WholeSubjects)
        ("Young Adult", ["young adult", "teen", "teens", "ya"], ["YAF", "YAN"]),
        ("Programming & Computers", ["programming", "computers", "computer", "software", "coding", "databases", "web development"], ["COM"]),
        ("Business", ["business", "economics", "management", "finance"], ["BUS"]),
        ("History", ["history"], ["HIS"]),
        ("Biography & Memoir", ["biography", "autobiography", "memoir", "memoirs"], ["BIO"]),
        ("Science", ["science", "physics", "mathematics", "biology", "chemistry", "astronomy"], ["SCI", "MAT"]),
        ("Philosophy", ["philosophy"], ["PHI"]),
        ("Psychology", ["psychology"], ["PSY"]),
        ("Self-Help", ["self help", "personal growth", "self improvement"], ["SEL"]),
        ("Religion & Spirituality", ["religion", "spirituality", "christian", "christianity", "theology", "buddhism"], ["REL", "OCC"]),
        ("Travel", ["travel"], ["TRV"]),
        ("Cooking", ["cooking", "cookbook", "cookbooks"], ["CKB"]),
        ("Art", ["art"], ["ART"]),
        ("Poetry", ["poetry", "poems"], ["POE"]),
        ("Comics & Graphic Novels", ["comics", "comic", "graphic novel", "graphic novels", "manga"], ["CGN"]),
    ];

    // Genres a subject means only when it is nothing else, folded.
    private static readonly Dictionary<string, string> WholeSubjects = new(StringComparer.Ordinal)
    {
        ["classic"] = "Classics",
        ["classics"] = "Classics",
        ["classic literature"] = "Classics",
    };

    // FictionBook (FB2) genre codes, from its standard genre list. A code is looked up whole, never read as words
    // ("sf_fantasy" is fantasy, not science fiction too); a code not here, such as "prose_classic", which some
    // converters put on nearly every book, gives no genre.
    private static readonly Dictionary<string, string> Fb2Codes = new(StringComparer.Ordinal)
    {
        ["sf"] = "Science Fiction",
        ["sf_social"] = "Science Fiction",
        ["sf_space"] = "Science Fiction",
        ["sf_cyberpunk"] = "Science Fiction",
        ["sf_history"] = "Science Fiction", // alternative history
        ["sf_action"] = "Science Fiction",
        ["sf_epic"] = "Science Fiction",
        ["sf_detective"] = "Science Fiction",
        ["sf_fantasy"] = "Fantasy",
        ["sf_heroic"] = "Fantasy",
        ["sf_horror"] = "Horror",
        ["sf_humor"] = "Humour",
        ["prose_history"] = "Historical Fiction",
        ["adv_history"] = "Historical Fiction",
        ["adventure"] = "Action & Adventure",
        ["adv_western"] = "Action & Adventure",
        ["det_classic"] = "Mystery & Crime",
        ["det_police"] = "Mystery & Crime",
        ["det_crime"] = "Mystery & Crime",
        ["det_history"] = "Mystery & Crime",
        ["detective"] = "Mystery & Crime",
        ["thriller"] = "Thriller",
        ["det_espionage"] = "Thriller",
        ["love_contemporary"] = "Romance",
        ["love_history"] = "Romance",
        ["love_sf"] = "Romance",
        ["humor"] = "Humour",
        ["humor_prose"] = "Humour",
        ["poetry"] = "Poetry",
        ["sci_history"] = "History",
        ["sci_philosophy"] = "Philosophy",
        ["sci_psychology"] = "Psychology",
        ["sci_religion"] = "Religion & Spirituality",
        ["religion"] = "Religion & Spirituality",
        ["nonf_biography"] = "Biography & Memoir",
        ["comp_programming"] = "Programming & Computers",
        ["computers"] = "Programming & Computers",
        ["cooking"] = "Cooking",
        ["travel_notes"] = "Travel",
    };

    /// <summary>Every genre, in the order genres are listed.</summary>
    public static IReadOnlyList<string> All { get; } = [.. Genres.Select(g => g.Genre)];

    /// <summary>The genres <paramref name="subjects"/> stand for, each once, in <see cref="All"/>'s order.</summary>
    public static IReadOnlyList<string> Map(IEnumerable<string> subjects)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var subject in subjects)
        {
            var trimmed = subject.Trim();
            if (Fb2Code().IsMatch(trimmed))
            {
                if (Fb2Codes.TryGetValue(trimmed, out var fb2))
                {
                    found.Add(fb2);
                }

                continue;
            }

            if (WholeSubjects.TryGetValue(MatchKeys.Fold(subject), out var whole))
            {
                found.Add(whole);
                continue;
            }

            var code = BisacCode().Match(trimmed);
            // "science" alone means the non-fiction heading: look for it with "science fiction" taken out.
            var text = $" {MatchKeys.Fold(subject)} ";
            var withoutScienceFiction = text.Replace(" science fiction ", " ", StringComparison.Ordinal);
            foreach (var (genre, words, codes) in Genres)
            {
                var haystack = genre == "Science" ? withoutScienceFiction : text;
                if ((code.Success && codes.Any(c => code.Value.StartsWith(c, StringComparison.Ordinal)))
                    || words.Any(w => haystack.Contains($" {w} ", StringComparison.Ordinal)))
                {
                    found.Add(genre);
                }
            }
        }

        return [.. All.Where(found.Contains)];
    }

    /// <summary>An FB2 genre code: lower-case words joined by "_" (e.g. sf_fantasy).</summary>
    [GeneratedRegex(@"^[a-z]+(?:_[a-z]+)+$")]
    private static partial Regex Fb2Code();

    /// <summary>A BISAC code: three letters, six digits (e.g. FIC028000).</summary>
    [GeneratedRegex(@"^[A-Z]{3}\d{6}$")]
    private static partial Regex BisacCode();
}
