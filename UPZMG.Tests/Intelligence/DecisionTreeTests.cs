using UPZMG.Implementations.Intelligence;
using Xunit;

namespace UPZMG.Tests.Intelligence;

// Evidence that the tree learns from data instead of repeating a hard-coded threshold.
public class DecisionTreeTests
{
    private const string Failure = "Reprobación %";
    private const string Average = "Promedio";

    private static Sample Make(double failureRate, double average, bool droppedOut) =>
        new(new Dictionary<string, double> { [Failure] = failureRate, [Average] = average }, droppedOut);

    private static List<Sample> Separable(int perClass)
    {
        var samples = new List<Sample>();
        for (int i = 0; i < perClass; i++) samples.Add(Make(5 + i % 20, 9, droppedOut: false));
        for (int i = 0; i < perClass; i++) samples.Add(Make(70 + i % 20, 4, droppedOut: true));
        return samples;
    }

    [Fact]
    public void Gini_IsZeroForAPureSetAndHalfForABalancedOne()
    {
        Assert.Equal(0, DecisionTree.Gini([Make(10, 9, true), Make(20, 8, true)]), precision: 9);
        Assert.Equal(0.5, DecisionTree.Gini([Make(10, 9, true), Make(20, 8, false)]), precision: 9);
        Assert.Equal(0, DecisionTree.Gini([]), precision: 9);
    }

    [Fact]
    public void Train_LearnsASeparableThreshold()
    {
        // Hidden truth: everyone above 50 % failure left. The learner has to find the cut.
        var samples = new List<Sample>();
        for (int i = 0; i < 40; i++) samples.Add(Make(10 + i % 30, 9, droppedOut: false));
        for (int i = 0; i < 40; i++) samples.Add(Make(60 + i % 30, 5, droppedOut: true));

        var tree = DecisionTree.Train(samples, maxDepth: 3, minSamplesLeaf: 5);

        Assert.True(tree.Predict(new Dictionary<string, double> { [Failure] = 15, [Average] = 9 }) < 0.2);
        Assert.True(tree.Predict(new Dictionary<string, double> { [Failure] = 80, [Average] = 5 }) > 0.8);
    }

    [Fact]
    public void Train_OnASinglePureClassPredictsThatClass()
    {
        var tree = DecisionTree.Train(Enumerable.Range(0, 20).Select(i => Make(i, 8, true)).ToList());

        Assert.Equal(1.0, tree.Predict(new Dictionary<string, double> { [Failure] = 5 }), precision: 6);
        Assert.True(tree.Root.IsLeaf);
    }

    [Fact]
    public void Train_OnNoSamplesStillPredictsWithoutThrowing()
    {
        var tree = DecisionTree.Train([]);

        Assert.Equal(0, tree.Predict(new Dictionary<string, double>()), precision: 6);
        Assert.True(tree.Root.IsLeaf);
        Assert.Empty(tree.FeatureImportance());
    }

    [Fact]
    public void Predict_TreatsAMissingFeatureAsZeroInsteadOfThrowing()
    {
        var tree = DecisionTree.Train(Separable(20));

        Assert.Null(Record.Exception(() => tree.Predict(new Dictionary<string, double>())));
    }

    [Fact]
    public void RespectsMaxDepth()
    {
        var samples = Enumerable.Range(0, 100).Select(i => Make(i, 10 - i / 10.0, i % 3 == 0)).ToList();

        var tree = DecisionTree.Train(samples, maxDepth: 2, minSamplesLeaf: 2);

        Assert.True(Depth(tree.Root) <= 2);
    }

    [Fact]
    public void RespectsMinSamplesLeaf()
    {
        var samples = Enumerable.Range(0, 60).Select(i => Make(i, 8, i % 2 == 0)).ToList();

        var tree = DecisionTree.Train(samples, maxDepth: 6, minSamplesLeaf: 10);

        Assert.All(LeafSizes(tree.Root), size => Assert.True(size >= 10, $"hoja con {size} muestras"));
    }

    [Fact]
    public void Explain_ReturnsThePathThatProducedThePrediction()
    {
        var tree = DecisionTree.Train(Separable(30));

        var path = tree.Explain(new Dictionary<string, double> { [Failure] = 85, [Average] = 4 });

        Assert.NotEmpty(path);
        Assert.All(path, step => Assert.Matches("≤|>", step));
    }

    [Fact]
    public void Explain_KeepsOnlyTheTightestBoundPerFeature()
    {
        // Averages 1–3 all left, 4–6 mostly left, 7–9 all stayed: the root cuts at 6.5
        // and its left child at 3.5, two cuts on the same side of the same feature.
        var samples = new List<Sample>();
        for (int i = 0; i < 30; i++) samples.Add(Make(10, 1 + i % 3, droppedOut: true));
        foreach (var (value, left, stayed) in new[] { (4.0, 7, 3), (5.0, 7, 3), (6.0, 6, 4) })
        {
            for (int i = 0; i < left; i++) samples.Add(Make(10, value, droppedOut: true));
            for (int i = 0; i < stayed; i++) samples.Add(Make(10, value, droppedOut: false));
        }
        for (int i = 0; i < 60; i++) samples.Add(Make(10, 7 + i % 3, droppedOut: false));

        var tree = DecisionTree.Train(samples, maxDepth: 2, minSamplesLeaf: 5);

        Assert.Equal(6.5, tree.Root.Threshold, precision: 9);
        Assert.Equal(new[] { "Promedio ≤ 3.5" }, tree.Explain(new Dictionary<string, double> { [Average] = 2 }));
        Assert.Equal(new[] { "3.5 < Promedio ≤ 6.5" }, tree.Explain(new Dictionary<string, double> { [Average] = 5 }));
        Assert.Equal(new[] { "Promedio > 6.5" }, tree.Explain(new Dictionary<string, double> { [Average] = 8 }));
        Assert.Contains(tree.Rules(), r => r.Condition == "Promedio ≤ 3.5");
    }

    [Theory]
    [InlineData(null, 7.5, "Promedio ≤ 7.5")]
    [InlineData(6.25, null, "Promedio > 6.25")]
    [InlineData(6.0, 7.5, "6 < Promedio ≤ 7.5")]
    public void DefaultDescriber_WritesBoundsAndIntervals(double? above, double? atMost, string expected)
    {
        Assert.Equal(expected, DecisionTree.DefaultDescriber(Average, above, atMost));
    }

    [Fact]
    public void Rules_CoverEveryLeafAndAreOrderedByRisk()
    {
        var tree = DecisionTree.Train(Separable(30));
        var rules = tree.Rules();

        Assert.Equal(LeafSizes(tree.Root).Count(), rules.Count);
        Assert.Equal(LeafSizes(tree.Root).Sum(), rules.Sum(r => r.SampleCount));
        Assert.Equal(rules.Select(r => r.Probability).OrderByDescending(p => p), rules.Select(r => r.Probability));
    }

    [Fact]
    public void Rules_UseTheGivenDescriber()
    {
        var tree = DecisionTree.Train(Separable(30));

        var rules = tree.Rules((feature, above, _) => $"{feature}:{(above is null ? "bajo" : "alto")}");

        Assert.All(rules, r => Assert.Matches("(bajo|alto)", r.Condition));
    }

    [Fact]
    public void FeatureImportance_AddsUpToOneAndCreditsTheUsefulFeature()
    {
        // Only the failure rate separates the classes; the average is identical for everyone.
        var samples = new List<Sample>();
        for (int i = 0; i < 40; i++) samples.Add(Make(10 + i % 20, 8, droppedOut: false));
        for (int i = 0; i < 40; i++) samples.Add(Make(60 + i % 20, 8, droppedOut: true));

        var weights = DecisionTree.Train(samples).FeatureImportance();

        Assert.Equal(1, weights.Sum(w => w.Importance), precision: 9);
        Assert.Equal(Failure, weights[0].Feature);
        Assert.Equal(0, weights.Single(w => w.Feature == Average).Importance, precision: 9);
    }

    [Fact]
    public void CrossValidate_ScoresEverySampleOnceAndIsDeterministic()
    {
        var samples = Separable(60);

        var first = DecisionTree.CrossValidate(samples, folds: 5);
        var second = DecisionTree.CrossValidate(samples, folds: 5);

        Assert.Equal(first, second);
        Assert.Equal(samples.Count, first.Evaluated);
        Assert.Equal(samples.Count, first.TrainingSize);
        Assert.Equal(5, first.Folds);
        Assert.True(first.Accuracy > 0.9, $"exactitud {first.Accuracy:P0}");
    }

    [Fact]
    public void CrossValidate_DoesNotLeakTheAnswer()
    {
        // Labels independent of the features: a model that has not memorised can only guess.
        var rng = new Random(7);
        var samples = Enumerable.Range(0, 400)
            .Select(_ => Make(rng.Next(0, 100), rng.Next(0, 100) / 10.0, rng.Next(0, 2) == 1))
            .ToList();

        var quality = DecisionTree.CrossValidate(samples, folds: 5, maxDepth: 4, minSamplesLeaf: 10);

        Assert.InRange(quality.Accuracy, 0.35, 0.65);
    }

    [Fact]
    public void CrossValidate_RefusesWhenAClassCannotFillEveryFold()
    {
        var samples = Separable(30).Where(s => !s.Label).Concat(Separable(2).Where(s => s.Label)).ToList();

        Assert.Equal(ModelQuality.None, DecisionTree.CrossValidate(samples, folds: 5));
    }

    [Fact]
    public void ModelQuality_ComputesTheStandardMetrics()
    {
        // 8 caught, 2 false alarms, 5 cleared, 5 missed.
        var q = new ModelQuality(TruePositives: 8, FalsePositives: 2, TrueNegatives: 5, FalseNegatives: 5, TrainingSize: 50, Folds: 5);

        Assert.Equal(20, q.Evaluated);
        Assert.Equal(13 / 20.0, q.Accuracy, precision: 6);
        Assert.Equal(8 / 10.0, q.Precision, precision: 6);
        Assert.Equal(8 / 13.0, q.Recall, precision: 6);
        Assert.Equal(2 * (0.8 * (8 / 13.0)) / (0.8 + 8 / 13.0), q.F1, precision: 6);
        Assert.Equal(13 / 20.0, q.Prevalence, precision: 6);
        Assert.Equal(0.8 / (13 / 20.0), q.Lift, precision: 6);
        Assert.Equal(13 / 20.0, q.BaselineAccuracy, precision: 6);
    }

    [Fact]
    public void ModelQuality_DoesNotDivideByZero()
    {
        var q = ModelQuality.None;

        Assert.Equal(0, q.Accuracy);
        Assert.Equal(0, q.Precision);
        Assert.Equal(0, q.Recall);
        Assert.Equal(0, q.F1);
        Assert.Equal(0, q.Lift);
        Assert.Equal(0, q.BaselineAccuracy);
    }

    private static int Depth(TreeNode node) =>
        node.IsLeaf ? 0 : 1 + Math.Max(Depth(node.Left!), Depth(node.Right!));

    private static IEnumerable<int> LeafSizes(TreeNode node)
    {
        if (node.IsLeaf) { yield return node.SampleCount; yield break; }

        foreach (var size in LeafSizes(node.Left!)) yield return size;
        foreach (var size in LeafSizes(node.Right!)) yield return size;
    }
}
