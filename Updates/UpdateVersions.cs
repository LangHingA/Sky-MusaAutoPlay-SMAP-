namespace SMAP_WPF.Updates;

public static class UpdateVersions
{
    public static int Compare(string a, string b)
    {
        var aa = a.Split('-', 2); var bb = b.Split('-', 2);
        var pa = aa[0].Split('.');
        var pb = bb[0].Split('.');
        int len = Math.Max(pa.Length, pb.Length);
        for (int i = 0; i < len; i++)
        {
            int va = i < pa.Length ? Num(pa[i]) : 0;
            int vb = i < pb.Length ? Num(pb[i]) : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        bool Pre(string[] parts) => parts.Length > 1 && new[] { "alpha", "beta", "rc" }.Any(p => parts[1].StartsWith(p, StringComparison.OrdinalIgnoreCase));
        bool preA = Pre(aa), preB = Pre(bb);
        if (preA != preB) return preA ? -1 : 1;
        if (!preA) return 0; // Preserve the existing stable meaning of legacy -wpf suffixes.
        var sa = aa[1].Split('.'); var sb = bb[1].Split('.');
        for (int i = 0; i < Math.Min(sa.Length, sb.Length); i++)
        {
            bool na = int.TryParse(sa[i], out int va), nb = int.TryParse(sb[i], out int vb);
            int compare = na && nb ? va.CompareTo(vb) : na != nb ? (na ? -1 : 1) : StringComparer.OrdinalIgnoreCase.Compare(sa[i], sb[i]);
            if (compare != 0) return compare;
        }
        return sa.Length.CompareTo(sb.Length);
    }

    // 取字符串前导数字(如 "1-wpf" -> 1, "2" -> 2)
    static int Num(string s)
    {
        int v = 0, i = 0;
        while (i < s.Length && char.IsDigit(s[i])) { v = v * 10 + (s[i] - '0'); i++; }
        return v;
    }
}
