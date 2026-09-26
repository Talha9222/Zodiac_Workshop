using UnityEngine;

public enum ZWVisibility { Visible, ShownOnce, Partial }

[System.Serializable]
public struct ZWLevel
{
    public int level, sequenceLength, decoyCount, retryBudget;
    public ZWVisibility targetVisibility;
    public bool reversedOrder;

    public ZWLevel(int level, int sequenceLength, ZWVisibility targetVisibility, int decoyCount, int retryBudget, bool reversedOrder)
    {
        this.level = level;
        this.sequenceLength = sequenceLength;
        this.targetVisibility = targetVisibility;
        this.decoyCount = decoyCount;
        this.retryBudget = retryBudget;
        this.reversedOrder = reversedOrder;
    }
}

// Level data array + saved progress. Animals are indexed in zodiac order (0 Rat .. 11 Pig),
// which is also the ring order: slot i always holds the genuine seal of animal i.
public static class ZWLevels
{
    public const int Count = 20;
    public const string ScrollsCompleted = "ZW_ScrollsCompleted";
    public const string PerfectSequences = "ZW_PerfectSequences";
    public const string LevelsCleared = "ZW_LevelsCleared";

    const ZWVisibility V = ZWVisibility.Visible, O = ZWVisibility.ShownOnce, P = ZWVisibility.Partial;

    public static readonly ZWLevel[] All =
    {
        new ZWLevel(1, 3, V, 0, 5, false),
        new ZWLevel(2, 3, V, 0, 5, false),
        new ZWLevel(3, 3, V, 0, 5, false),
        new ZWLevel(4, 3, V, 0, 5, false),
        new ZWLevel(5, 3, V, 0, 5, false),
        new ZWLevel(6, 3, V, 0, 5, false),
        new ZWLevel(7, 3, V, 0, 5, false),
        new ZWLevel(8, 4, O, 1, 3, false),
        new ZWLevel(9, 4, O, 1, 3, false),
        new ZWLevel(10, 4, O, 2, 3, false),
        new ZWLevel(11, 5, O, 1, 3, false),
        new ZWLevel(12, 5, O, 2, 3, false),
        new ZWLevel(13, 5, O, 2, 3, false),
        new ZWLevel(14, 5, P, 2, 2, true),
        new ZWLevel(15, 5, P, 2, 2, false),
        new ZWLevel(16, 5, P, 3, 2, true),
        new ZWLevel(17, 6, P, 2, 2, false),
        new ZWLevel(18, 6, P, 3, 2, true),
        new ZWLevel(19, 6, P, 3, 2, false),
        new ZWLevel(20, 6, P, 3, 2, true),
    };

    // Deterministic per level. seq = target animals in banner order.
    // ring[slot] = animal carved on that slot; ring[slot] != slot means a counterfeit (decoy).
    public static void Build(int level, out int[] seq, out int[] ring)
    {
        var d = All[level - 1];
        var rng = new System.Random(level * 7919);
        seq = new int[d.sequenceLength];

        int[] pool = Shuffled(rng);
        if (d.targetVisibility == ZWVisibility.Partial)
        {
            // Consecutive zodiac run, so hidden gaps can be inferred from the ring order.
            int start = rng.Next(12);
            for (int i = 0; i < seq.Length; i++) seq[i] = (start + i) % 12;
        }
        else
        {
            for (int i = 0; i < seq.Length; i++) seq[i] = pool[i];
        }

        ring = new int[12];
        for (int i = 0; i < 12; i++) ring[i] = i;

        // Counterfeits: a slot whose animal isn't in the sequence shows a copy of a target seal.
        int placed = 0, offset = rng.Next(seq.Length);
        foreach (int slot in Shuffled(rng))
        {
            if (placed == d.decoyCount) break;
            if (System.Array.IndexOf(seq, slot) >= 0) continue;
            ring[slot] = seq[(offset + placed) % seq.Length];
            placed++;
        }
    }

    static int[] Shuffled(System.Random rng)
    {
        int[] a = new int[12];
        for (int i = 0; i < 12; i++) a[i] = i;
        for (int i = 11; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
        return a;
    }

    public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt("ZW_Unlocked", 1), 1, Count);
    public static int Stars(int level) => PlayerPrefs.GetInt("ZW_Stars_" + level, 0);
    public static int Stat(string key) => PlayerPrefs.GetInt(key, 0);

    public static void RecordClear(int level, int stars, bool perfect)
    {
        Add(ScrollsCompleted);
        if (perfect) Add(PerfectSequences);
        if (Stars(level) == 0) Add(LevelsCleared);
        if (stars > Stars(level)) PlayerPrefs.SetInt("ZW_Stars_" + level, stars);
        if (level + 1 > Unlocked) PlayerPrefs.SetInt("ZW_Unlocked", Mathf.Min(level + 1, Count));
        PlayerPrefs.Save();
    }

    static void Add(string key) => PlayerPrefs.SetInt(key, Stat(key) + 1);
}
