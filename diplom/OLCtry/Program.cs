using Microsoft.EntityFrameworkCore.Metadata.Internal;

Console.WriteLine("Hello, World!");

string row_5f = "GYCGGGATCAAGGCAGGTGTGAAGATTATAAATTAACTTATTTATACTCCGGAATAKGAAACCAAARATACKGATAGGGGGCGCATTCCGAGTAACTCCGCAACCCGGAGTTCCCCTGGAAAAAGSGGGGGCCGCAGTAGCTGCTGAATCCTCTACTGGTACATGGACAACTGTGTGGACTGATGGGYTTACTAGCCTTGATCGTTACAAAGGACRATGCTMCCMCWTTGAGCCCGTTGCCGGARAGGAARATCAATACWTTGYTTATGTASYTTATCCTTTARACCTTTTTGAARAAGGTTCTGTTMCCAACWTGTTTACTTCCWTTGWAGGTAATGTATTTGGGTTCAAAGYTCTACRAGCTCTACSCTTARAGGATCTGCRAATTCCCCCTGCTTATTCCAAAACTTTTCARGGYCCMCCTCWTGKAATCCAAKTTRAAARARATARATTAAACAAATATGGCCGTCCTCTATTGGRATGTACTATTAAACCAAAATTGGRATTATCCSCRAAAAACTACGGTARAGCGGTTTATGAATGTCTACGKGGKGRACTTGATTTTAAA";
string row_5r = "GKWACGGTYGYGWCGTAGTTTTTCGCGGATATCCCAATTTTGGTTYAATAGTACATCCCAATAGAGGACGGARGAGAGTTTAATCTATCTCTTTCAACTTGGATTCCATGAGGTGGAMMRRKGAAAAGTTTTGGAATAAGCAGGGGGAATTCGCAGATCCTCTAAGCGTAGAGCTCGTAGAGCTTTGAACCCAAATACATTACCTACAATGGAAGTAAACATGTTGGTAACAGAACCTTCTTCAAAAAGGTCTAAAGGATAAGCTACATAAGCAATGTATTGATCTTCCTCTCCGGCAACGGGCTCAATGTGGTAGCATCGTCCTTTGTAACGATCAAGGCTAGTAAGCCCATCAGTCCACACAGTTGTCCATGTACCAGTAGAGGATTCAGCAGCTACTGCGGCCCCCGCTTCTTCAGGTGGAACTYCGGGTTGCGGAGTTACTCGGAATGCTGCCAAGATATCAGTATCTTTGGTTTCATATTCCGGAGTATAATAAGTTAATTTATAATCTTTCACACCTGCTTTGAATCCAAGCCCTGCTTTAGTCTCTGTTTGTGGTGACAAAAT";

char Complement(char c) => c switch
{
    'A' => 'T',
    'T' => 'A',
    'G' => 'C',
    'C' => 'G',
    'R' => 'Y',
    'Y' => 'R',
    'S' => 'S',
    'W' => 'W',
    'K' => 'M',
    'M' => 'K',
    'B' => 'V',
    'V' => 'B',
    'D' => 'H',
    'H' => 'D',
    'N' => 'N',
    _ => 'N'
};

char[] result = new char[row_5r.Length];
for (int i = 0; i < row_5r.Length; i++)
{
    result[i] = Complement(row_5r[row_5r.Length - 1 - i]);
}

row_5r = new string(result);

int max = Math.Min(row_5f.Length, row_5r.Length);

int trueLen = 0;
double trueRate = 0.0;
Console.WriteLine("5f suffix vs 5r_rc prefix");
for (int len = max; len >= 10; len--)
{
    string leftSuffix = row_5f.Substring(row_5f.Length - len, len);
    string rightPrefix = row_5r.Substring(0, len);

    int matches = 0;
    for (int i = 0; i < len; i++)
    {
        if (leftSuffix[i] == rightPrefix[i])
            matches++;
    }

    double matchRate = (double)matches / len;
    if (matchRate > 0.5)
    {
        Console.WriteLine($"len={len}, matchRate={matchRate}");
        trueRate = Math.Max(trueRate, matchRate);
        if (trueRate == matchRate)
        {
            trueLen = len;
        }
    }
}

Console.WriteLine("5r_rc suffix vs 5f prefix");
for (int len = max; len >= 10; len--)
{
    string leftSuffix = row_5r.Substring(row_5r.Length - len, len);
    string rightPrefix = row_5f.Substring(0, len);

    int matches = 0;
    for (int i = 0; i < len; i++)
    {
        if (leftSuffix[i] == rightPrefix[i])
            matches++;
    }

    double matchRate = (double)matches / len;
    if (matchRate > 0.5)
    {
        Console.WriteLine($"len={len}, matchRate={matchRate}");
        trueRate = Math.Max(trueRate, matchRate);
        if (trueRate == matchRate)
        {
            trueLen = len;
        }
    }
}

bool IsStrictBase(char c)
{
    return c == 'A' || c == 'C' || c == 'G' || c == 'T';
}

char MergeBase(char a, char b)
{
    // если обе буквы нормальные и совпали — берём её
    if (IsStrictBase(a) && IsStrictBase(b))
        return a == b ? a : 'N';

    // если одна нормальная, а вторая "грязная" — берём нормальную
    if (IsStrictBase(a))
        return a;

    if (IsStrictBase(b))
        return b;

    // если обе неоднозначные — игнорируем
    return 'N';
}

// overlap: suffix(row_5r_rc) vs prefix(row_5f)
//row_5r: [левый хвост][-----------overlap-----------]
//row_5f:              [-----------overlap-----------][правый хвост]
string overlapLeft = row_5r.Substring(row_5r.Length - trueLen, trueLen);
string overlapRight = row_5f.Substring(0, trueLen);

char[] consensus = new char[trueLen];
for (int i = 0; i < trueLen; i++)
{
    consensus[i] = MergeBase(overlapLeft[i], overlapRight[i]);
}

// берём только overlap + правый хвост forward-рида
string res = row_5r.Substring(0, row_5r.Length - trueLen) + new string(consensus) + row_5f.Substring(trueLen);
string reference = "GATTATAAATTAACTTATTATACTCCGGAATATGAAACCAAAGATACTGATATCTTGGCAGCATTCCGAGTAACTCCGCAACCCGGAGTTCCACCTGAAGAAGCGGGGGCCGCAGTAGCTGCTGAATCCTCTACTGGTACATGGACAACTGTGTGGACTGATGGGCTTACTAGCCTTGATCGTTACAAAGGACGATGCTACCACATTGAGCCCGTTGCCGGAGAGGAAGATCAATACATTGCTTATGTAGCTTATCCTTTAGACCTTTTTGAAGAAGGTTCTGTTACCAACATGTTTACTTCCATTGTAGGTAATGTATTTGGGTTCAAAGCTCTACGAGCTCTACGCTTAGAGGATCTGCGAATTCCCCCTGCTTATTCCAAAACTTTTCAAGGTCCACCTCATGGAATCCAAGTTGAAAGAGATAGATTAAACAAATATGGCCGTCCTCTATTGGGATGTACTATTAAACCAAAATTGGGATTATCCGCGAAAAACTACGGTAGAGCGGTTTATGAATGTCTACGCGGTGGACTTGATTTTACCAAGGATGATGAAAACGTGAACTCACAGCCATTTATGCGGTGGAGAGACCGTTTCCTATTTTGTGCCGAAGCAATTTATAAAGCCCAAGACGAAACAGGTGAAATCAAAGGACATTACTTGAATGCTACTGCGGGTACATGTGAAGAAATGATCAAAAGAGCCGTATTTGCCAAAGAATTGGGAGTTCCTATCGTAATGCATGACTACTTAACAGGGGGATTCACTGCAAATACTAGCTTGGCTGAATATTGTCGAAACAACGGCTTACTTCTTCACATTCACCGCGCAATGCATGCAGTTATTGATAGACAGAAGAATCATGGCATGCATTTTCGTGTACTAGCGAAAGCATTACGTATGTCTGGCGGGGATCACATTCACTCTGGTACAGTAGTAGGTAAACTGGAAGGTGAACGTGAAATGACTTTAGGTTTTGTTGATTTACTACGTGACGATTATATTGAAAAAGACCGAAGTCGTGGTATTTTCTTCACTCAAGATTGGGTCTCTATGCCTGGTGTTTTGCCGGTGGCTTCCGGAGGTATTCATGTTTGGCATATGCCCGCCTTGACCGAGATCTTTGGAGATGATTC";

string ReverseComplement(string seq)
{
    char[] rc = new char[seq.Length];
    for (int i = 0; i < seq.Length; i++)
    {
        rc[i] = Complement(seq[seq.Length - 1 - i]);
    }
    return new string(rc);
}

(int contigStart, int refStart, int used, int matches, int mismatches, double rate) FindBestMatch(string contig, string referenceSeq)
{
    int bestContigStart = -1;
    int bestRefStart = -1;
    int bestUsed = -1;
    int bestMatches = -1;
    int bestMismatches = int.MaxValue;
    double bestRate = -1.0;

    int minUsed = 200; // можно поставить 100, 200 или 300

    for (int contigStart = 0; contigStart < contig.Length; contigStart++)
    {
        for (int refStart = 0; refStart < referenceSeq.Length; refStart++)
        {
            int len = Math.Min(contig.Length - contigStart, referenceSeq.Length - refStart);

            int used = 0;
            int matches = 0;
            int mismatches = 0;

            for (int i = 0; i < len; i++)
            {
                char a = contig[contigStart + i];
                char b = referenceSeq[refStart + i];

                if (!IsStrictBase(a) || !IsStrictBase(b))
                    continue;

                used++;

                if (a == b)
                    matches++;
                else
                    mismatches++;
            }

            if (used < minUsed)
                continue;

            double rate = (double)matches / used;

            if (
                rate > bestRate ||
                (Math.Abs(rate - bestRate) < 1e-12 && used > bestUsed) ||
                (Math.Abs(rate - bestRate) < 1e-12 && used == bestUsed && mismatches < bestMismatches)
            )
            {
                bestRate = rate;
                bestContigStart = contigStart;
                bestRefStart = refStart;
                bestUsed = used;
                bestMatches = matches;
                bestMismatches = mismatches;
            }
        }
    }

    return (bestContigStart, bestRefStart, bestUsed, bestMatches, bestMismatches, bestRate);
}

var direct = FindBestMatch(res, reference);
var resRc = ReverseComplement(res);
var reverse = FindBestMatch(resRc, reference);

Console.WriteLine($"DIRECT: contigStart={direct.contigStart}, refStart={direct.refStart}, used={direct.used}, matches={direct.matches}, mismatches={direct.mismatches}, rate={direct.rate:F4}");
Console.WriteLine($"RC:     contigStart={reverse.contigStart}, refStart={reverse.refStart}, used={reverse.used}, matches={reverse.matches}, mismatches={reverse.mismatches}, rate={reverse.rate:F4}");