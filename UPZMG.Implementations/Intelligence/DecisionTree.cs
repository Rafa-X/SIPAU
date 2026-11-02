using System.Globalization;

namespace UPZMG.Implementations.Intelligence;

/// <summary>One training example: named numeric features plus the observed label.</summary>
public sealed record Sample(IReadOnlyDictionary<string, double> Features, bool Label);

/// <summary>A tree node. A leaf carries a probability; a split carries the feature and threshold it tests.</summary>
public sealed class TreeNode
{
    public string? Feature { get; init; }
    public double Threshold { get; init; }
    public TreeNode? Left { get; init; }   // Feature <= Threshold
    public TreeNode? Right { get; init; }  // Feature >  Threshold

    // Share of positive labels among the training samples that reached this node
    public double Probability { get; init; }
    public int SampleCount { get; init; }
    public double Impurity { get; init; }

    public bool IsLeaf => Feature is null;
}

/// <summary>Quality measured on students the model did not see while training.</summary>
/// <param name="TruePositives">Flagged at risk, and did drop out.</param>
/// <param name="FalsePositives">Flagged at risk, but finished.</param>
/// <param name="TrueNegatives">Not flagged, and finished.</param>
/// <param name="FalseNegatives">Not flagged, but dropped out: the costly error here.</param>
/// <param name="TrainingSize">Samples the published model was trained on.</param>
/// <param name="Folds">Cross-validation folds used to measure it.</param>
public sealed record ModelQuality(
    int TruePositives, int FalsePositives, int TrueNegatives, int FalseNegatives, int TrainingSize, int Folds)
{
    public int Evaluated => TruePositives + FalsePositives + TrueNegatives + FalseNegatives;

    public double Accuracy => Evaluated == 0 ? 0 : (double)(TruePositives + TrueNegatives) / Evaluated;

    // Of those flagged, how many really left
    public double Precision => TruePositives + FalsePositives == 0
        ? 0 : (double)TruePositives / (TruePositives + FalsePositives);

    // Of those who left, how many were flagged; the metric that matters most here
    public double Recall => TruePositives + FalseNegatives == 0
        ? 0 : (double)TruePositives / (TruePositives + FalseNegatives);

    public double F1 => Precision + Recall == 0 ? 0 : 2 * Precision * Recall / (Precision + Recall);

    // Share of the evaluated trajectories that end up flagged
    public double FlaggedShare => Evaluated == 0 ? 0 : (double)(TruePositives + FalsePositives) / Evaluated;

    // Share of positives: the precision of flagging students at random
    public double Prevalence => Evaluated == 0 ? 0 : (double)(TruePositives + FalseNegatives) / Evaluated;

    // How many times better than random the flags are
    public double Lift => Prevalence == 0 ? 0 : Precision / Prevalence;

    // Accuracy of always answering the majority class
    public double BaselineAccuracy => Evaluated == 0
        ? 0 : (double)Math.Max(TruePositives + FalseNegatives, TrueNegatives + FalsePositives) / Evaluated;

    /// <summary>The cost of this matrix when an extra alert costs 1 and a missed positive costs <paramref name="falseNegativeCost"/>.</summary>
    public double Cost(double falseNegativeCost) => FalsePositives + falseNegativeCost * FalseNegatives;

    public static readonly ModelQuality None = new(0, 0, 0, 0, 0, 0);
}

/// <summary>A root-to-leaf path written as a readable rule.</summary>
public sealed record TreeRule(string Condition, double Probability, int SampleCount);

/// <summary>What the tree decided for one observation, and why.</summary>
/// <param name="Probability">Share of positive labels among the training samples in the leaf reached.</param>
/// <param name="Support">How many training samples reached that leaf.</param>
/// <param name="Conditions">The path to the leaf, one condition per feature, in the order first tested.</param>
public sealed record TreeDecision(double Probability, int Support, IReadOnlyList<string> Conditions)
{
    /// <summary>Positive training samples in the leaf: the k of the k / n behind the probability.</summary>
    public int Positives => (int)Math.Round(Probability * Support);
}

/// <summary>The score one sample got from the fold model that never saw it.</summary>
/// <param name="Index">Position of the sample in the list that was validated.</param>
public sealed record OutOfFoldScore(int Fold, int Index, double Probability, bool Label);

/// <summary>Every sample scored once by a model trained without it, and how big each training set was.</summary>
public sealed record CrossValidationRun(int Folds, IReadOnlyList<int> TrainingSizes, IReadOnlyList<OutOfFoldScore> Scores);

/// <summary>Share of the total impurity reduction a feature is responsible for (0..1).</summary>
public sealed record FeatureWeight(string Feature, double Importance);

/// <summary>
/// Turns the bounds a path puts on one feature into text. <paramref name="above"/> is the
/// strict lower bound (feature &gt; above), <paramref name="atMost"/> the upper one
/// (feature ≤ atMost); at least one of them is set.
/// </summary>
public delegate string BoundDescriber(string feature, double? above, double? atMost);

/// <summary>
/// CART classification tree with Gini impurity, written from scratch.
///
///   Gini(S)      = 1 − Σ pᵢ²
///   Gain(S,d,t)  = Gini(S) − |L|/|S|·Gini(L) − |R|/|S|·Gini(R)
///
/// Every node keeps the (feature, threshold) pair with the largest gain. Growth stops
/// on a pure node, at the depth cap, or when a child would fall below the minimum size.
/// A tree was chosen because every prediction can be explained as a rule.
/// </summary>
public sealed class DecisionTree
{
    public TreeNode Root { get; }
    public IReadOnlyList<string> FeatureNames { get; }

    private DecisionTree(TreeNode root, IReadOnlyList<string> featureNames)
    {
        Root = root;
        FeatureNames = featureNames;
    }

    public static string DefaultDescriber(string feature, double? above, double? atMost) => (above, atMost) switch
    {
        ({ } low, { } high) => $"{Number(low)} < {feature} ≤ {Number(high)}",
        ({ } low, null) => $"{feature} > {Number(low)}",
        _ => $"{feature} ≤ {Number(atMost!.Value)}",
    };

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <param name="maxDepth">Depth cap. Shallow trees generalise; deep ones memorise.</param>
    /// <param name="minSamplesLeaf">A split that leaves a smaller child is rejected.</param>
    public static DecisionTree Train(IReadOnlyList<Sample> samples, int maxDepth = 4, int minSamplesLeaf = 5)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
            return new DecisionTree(Leaf([]), []);

        var featureNames = samples[0].Features.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var root = Grow(samples, featureNames, depth: 0, maxDepth, minSamplesLeaf);

        return new DecisionTree(root, featureNames);
    }

    /// <summary>Probability that the outcome occurs for this observation.</summary>
    public double Predict(IReadOnlyDictionary<string, double> features) => LeafFor(features).Probability;

    /// <summary>The tests that led to the prediction: one condition per feature, in the order first tested.</summary>
    public IReadOnlyList<string> Explain(IReadOnlyDictionary<string, double> features, BoundDescriber? describe = null) =>
        Decide(features, describe).Conditions;

    /// <summary>
    /// The leaf this observation reaches, with the path that leads to it. The probability, the
    /// number of samples behind it and the explanation come from one walk down the tree, so the
    /// reasons shown can never belong to a different leaf than the one that produced the number.
    /// </summary>
    public TreeDecision Decide(IReadOnlyDictionary<string, double> features, BoundDescriber? describe = null)
    {
        var path = new List<SplitTest>();
        var node = Root;

        while (!node.IsLeaf)
        {
            var goesLeft = ValueOf(features, node.Feature!) <= node.Threshold;
            path.Add(new SplitTest(node.Feature!, goesLeft, node.Threshold));
            node = goesLeft ? node.Left! : node.Right!;
        }

        return new TreeDecision(node.Probability, node.SampleCount, Describe(path, describe ?? DefaultDescriber));
    }

    /// <summary>Every leaf as a rule, highest risk first.</summary>
    public IReadOnlyList<TreeRule> Rules(BoundDescriber? describe = null)
    {
        var rules = new List<TreeRule>();
        Walk(Root, [], rules, describe ?? DefaultDescriber);
        return [.. rules.OrderByDescending(r => r.Probability).ThenByDescending(r => r.SampleCount)];
    }

    private readonly record struct SplitTest(string Feature, bool LessOrEqual, double Threshold);

    // "Promedio ≤ 7.5 y … Promedio > 6.8" reads as one interval: the tightest bound on each side.
    private static List<string> Describe(IReadOnlyList<SplitTest> tests, BoundDescriber describe)
    {
        var order = new List<string>();
        var bounds = new Dictionary<string, (double? Above, double? AtMost)>();

        foreach (var test in tests)
        {
            if (!bounds.TryGetValue(test.Feature, out var bound)) order.Add(test.Feature);

            bound = test.LessOrEqual
                ? bound with { AtMost = Math.Min(bound.AtMost ?? double.MaxValue, test.Threshold) }
                : bound with { Above = Math.Max(bound.Above ?? double.MinValue, test.Threshold) };
            bounds[test.Feature] = bound;
        }

        return [.. order.Select(f => describe(f, bounds[f].Above, bounds[f].AtMost))];
    }

    /// <summary>
    /// Mean decrease in impurity: each split credits its feature with
    /// n·Gini(node) − nL·Gini(L) − nR·Gini(R), normalised to add up to 1.
    /// </summary>
    public IReadOnlyList<FeatureWeight> FeatureImportance()
    {
        var totals = FeatureNames.ToDictionary(f => f, _ => 0.0);
        Accumulate(Root, totals);

        var sum = totals.Values.Sum();
        return [.. totals
            .Select(t => new FeatureWeight(t.Key, sum <= 0 ? 0 : t.Value / sum))
            .OrderByDescending(w => w.Importance)
            .ThenBy(w => w.Feature, StringComparer.Ordinal)];
    }

    private TreeNode LeafFor(IReadOnlyDictionary<string, double> features)
    {
        var node = Root;
        while (!node.IsLeaf)
            node = ValueOf(features, node.Feature!) <= node.Threshold ? node.Left! : node.Right!;
        return node;
    }

    // A missing feature reads as 0 and goes left: an incomplete record still gets a score.
    private static double ValueOf(IReadOnlyDictionary<string, double> features, string feature) =>
        features.TryGetValue(feature, out var value) ? value : 0;

    private static void Walk(TreeNode node, List<SplitTest> path, List<TreeRule> into, BoundDescriber describe)
    {
        if (node.IsLeaf)
        {
            var condition = path.Count == 0 ? "Todos los casos" : string.Join(" · ", Describe(path, describe));
            into.Add(new TreeRule(condition, node.Probability, node.SampleCount));
            return;
        }

        Walk(node.Left!, [.. path, new SplitTest(node.Feature!, true, node.Threshold)], into, describe);
        Walk(node.Right!, [.. path, new SplitTest(node.Feature!, false, node.Threshold)], into, describe);
    }

    private static void Accumulate(TreeNode node, Dictionary<string, double> totals)
    {
        if (node.IsLeaf) return;

        var decrease = node.SampleCount * node.Impurity
                     - node.Left!.SampleCount * node.Left.Impurity
                     - node.Right!.SampleCount * node.Right.Impurity;

        totals[node.Feature!] = totals.GetValueOrDefault(node.Feature!) + Math.Max(0, decrease);
        Accumulate(node.Left, totals);
        Accumulate(node.Right, totals);
    }

    // ── Learning ────────────────────────────────────────────────────────────

    private static TreeNode Grow(
        IReadOnlyList<Sample> samples, IReadOnlyList<string> features,
        int depth, int maxDepth, int minSamplesLeaf)
    {
        var impurity = Gini(samples);

        if (depth >= maxDepth || impurity == 0 || samples.Count < minSamplesLeaf * 2)
            return Leaf(samples);

        var best = BestSplit(samples, features, minSamplesLeaf, impurity);
        if (best is null) return Leaf(samples);

        var (feature, threshold, left, right) = best.Value;

        return new TreeNode
        {
            Feature = feature,
            Threshold = threshold,
            Probability = PositiveRate(samples),
            SampleCount = samples.Count,
            Impurity = impurity,
            Left = Grow(left, features, depth + 1, maxDepth, minSamplesLeaf),
            Right = Grow(right, features, depth + 1, maxDepth, minSamplesLeaf),
        };
    }

    private static (string Feature, double Threshold, List<Sample> Left, List<Sample> Right)? BestSplit(
        IReadOnlyList<Sample> samples, IReadOnlyList<string> features, int minSamplesLeaf, double parentImpurity)
    {
        (string, double, List<Sample>, List<Sample>)? best = null;
        double bestGain = 0;

        foreach (var feature in features)
        {
            // Midpoints between consecutive distinct values: any other cut gives the same partition.
            var values = samples.Select(s => ValueOf(s.Features, feature))
                                .Distinct()
                                .OrderBy(v => v)
                                .ToList();

            for (int i = 0; i < values.Count - 1; i++)
            {
                var threshold = (values[i] + values[i + 1]) / 2;

                var left = new List<Sample>();
                var right = new List<Sample>();

                foreach (var s in samples)
                    (ValueOf(s.Features, feature) <= threshold ? left : right).Add(s);

                if (left.Count < minSamplesLeaf || right.Count < minSamplesLeaf) continue;

                var gain = parentImpurity
                         - (double)left.Count / samples.Count * Gini(left)
                         - (double)right.Count / samples.Count * Gini(right);

                if (gain > bestGain + 1e-12)
                {
                    bestGain = gain;
                    best = (feature, threshold, left, right);
                }
            }
        }

        return best;
    }

    private static TreeNode Leaf(IReadOnlyList<Sample> samples) => new()
    {
        Probability = PositiveRate(samples),
        SampleCount = samples.Count,
        Impurity = Gini(samples),
    };

    /// <summary>Gini impurity for a binary label: 1 − p² − (1−p)².</summary>
    public static double Gini(IReadOnlyList<Sample> samples)
    {
        if (samples.Count == 0) return 0;

        var p = PositiveRate(samples);
        return 1 - p * p - (1 - p) * (1 - p);
    }

    private static double PositiveRate(IReadOnlyList<Sample> samples) =>
        samples.Count == 0 ? 0 : (double)samples.Count(s => s.Label) / samples.Count;

    // ── Evaluation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Stratified k-fold cross-validation. Every sample is scored exactly once by a tree
    /// that never saw it, and the confusion matrices are pooled. Folds keep the class
    /// ratio, and the shuffle is seeded so the reported figures are reproducible.
    /// </summary>
    public static ModelQuality CrossValidate(
        IReadOnlyList<Sample> samples,
        int folds = 5,
        double decisionThreshold = 0.5,
        int maxDepth = 4,
        int minSamplesLeaf = 5,
        int seed = 20260917)
    {
        var run = CrossValidateScores(samples, folds, maxDepth, minSamplesLeaf, seed);
        return run is null ? ModelQuality.None : Confusion(run.Scores, decisionThreshold, samples.Count, folds);
    }

    /// <summary>
    /// The scores behind <see cref="CrossValidate"/>: one per sample, each produced by the fold
    /// model trained on the other folds. Keeping them, rather than only the pooled counts, is
    /// what lets the matrix be read per fold and the decision threshold be studied on data no
    /// model was trained on. Null when a fold could not hold a sample of each class.
    /// </summary>
    public static CrossValidationRun? CrossValidateScores(
        IReadOnlyList<Sample> samples,
        int folds = 5,
        int maxDepth = 4,
        int minSamplesLeaf = 5,
        int seed = 20260917)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var positives = samples.Where(s => s.Label).ToList();
        var negatives = samples.Where(s => !s.Label).ToList();

        // Each fold needs at least one sample of each class to mean anything.
        if (folds < 2 || positives.Count < folds || negatives.Count < folds)
            return null;

        var rng = new Random(seed);
        var foldOf = new Dictionary<Sample, int>(ReferenceEqualityComparer.Instance);
        foreach (var group in new[] { positives, negatives })
        {
            var shuffled = group.OrderBy(_ => rng.Next()).ToList();
            for (int i = 0; i < shuffled.Count; i++) foldOf[shuffled[i]] = i % folds;
        }

        var scores = new List<OutOfFoldScore>(samples.Count);
        var trainingSizes = new List<int>(folds);

        for (int fold = 0; fold < folds; fold++)
        {
            // The held-out fold is not in the training set: what is scored was never seen.
            var training = samples.Where(s => foldOf[s] != fold).ToList();
            var model = Train(training, maxDepth, minSamplesLeaf);
            trainingSizes.Add(training.Count);

            for (int i = 0; i < samples.Count; i++)
            {
                if (foldOf[samples[i]] != fold) continue;
                scores.Add(new OutOfFoldScore(fold, i, model.Predict(samples[i].Features), samples[i].Label));
            }
        }

        return new CrossValidationRun(folds, trainingSizes, [.. scores.OrderBy(s => s.Index)]);
    }

    /// <summary>The confusion matrix of a set of scores at one decision threshold.</summary>
    public static ModelQuality Confusion(IEnumerable<OutOfFoldScore> scores, double threshold, int trainingSize, int folds)
    {
        int tp = 0, fp = 0, tn = 0, fn = 0;
        foreach (var score in scores)
        {
            var flagged = score.Probability >= threshold;

            if (flagged && score.Label) tp++;
            else if (flagged) fp++;
            else if (score.Label) fn++;
            else tn++;
        }

        return new ModelQuality(tp, fp, tn, fn, trainingSize, folds);
    }
}
