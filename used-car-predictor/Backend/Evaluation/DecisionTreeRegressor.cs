using used_car_predictor.Backend.Models;

namespace used_car_predictor.Backend.Evaluation
{
    public class DecisionTreeRegressor(
        int maxDepth = 10,
        int minSamplesSplit = 2,
        int minSamplesLeaf = 1,
        int maxSplitsPerFeature = 32)
        : IRegressor
    {
        private Node? _root;

        public string Name => "Decision Tree Regressor";

        public void Fit(double[,] features, double[] labels)
        {
            _root = BuildTree(features, labels, depth: 0);
        }

        public double[] Predict(double[,] features)
        {
            var n = features.GetLength(0);
            var preds = new double[n];

            for (int i = 0; i < n; i++)
            {
                double[] row = new double[features.GetLength(1)];
                for (int j = 0; j < row.Length; j++)
                    row[j] = features[i, j];

                preds[i] = Predict(row);
            }

            return preds;
        }

        public double Predict(double[] featureRow)
        {
            if (_root == null)
                throw new InvalidOperationException("Tree not trained yet.");
            return Traverse(_root, featureRow);
        }

        private Node BuildTree(double[,] features, double[] labels, int depth)
        {
            int nSamples = features.GetLength(0);
            int nFeatures = features.GetLength(1);
            double currentVar = Variance(labels);

            if (depth >= maxDepth || nSamples < minSamplesSplit || currentVar == 0)
                return new Node { Value = Mean(labels) };

            double bestGain = 0;
            int bestFeature = -1;
            double bestThreshold = 0;
            List<int>? bestLeftIdx = null;
            List<int>? bestRightIdx = null;

            for (int feature = 0; feature < nFeatures; feature++)
            {
                double[] column = GetColumn(features, feature);

                var sorted = (double[])column.Clone();
                Array.Sort(sorted);

                int nSplits = Math.Min(maxSplitsPerFeature, sorted.Length - 1);
                if (nSplits <= 0) continue;

                for (int s = 1; s <= nSplits; s++)
                {
                    int idx = (int)Math.Round((double)s / (nSplits + 1) * (sorted.Length - 1));
                    double threshold = sorted[idx];

                    var (leftIdx, rightIdx) = SplitIndices(column, threshold);
                    if (leftIdx.Count < minSamplesLeaf || rightIdx.Count < minSamplesLeaf)
                        continue;

                    double[] leftY = Subset(labels, leftIdx);
                    double[] rightY = Subset(labels, rightIdx);

                    double gain = VarianceGain(labels, leftY, rightY);
                    if (gain > bestGain)
                    {
                        bestGain = gain;
                        bestFeature = feature;
                        bestThreshold = threshold;
                        bestLeftIdx = new List<int>(leftIdx);
                        bestRightIdx = new List<int>(rightIdx);
                    }
                }
            }

            if (bestFeature == -1 || bestGain == 0)
                return new Node { Value = Mean(labels) };

            double[,] leftX = Subset(features, bestLeftIdx!);
            double[] leftYFinal = Subset(labels, bestLeftIdx!);
            double[,] rightX = Subset(features, bestRightIdx!);
            double[] rightYFinal = Subset(labels, bestRightIdx!);

            return new Node
            {
                FeatureIndex = bestFeature,
                Threshold = bestThreshold,
                Left = BuildTree(leftX, leftYFinal, depth + 1),
                Right = BuildTree(rightX, rightYFinal, depth + 1)
            };
        }

        private double Traverse(Node node, double[] x)
        {
            if (node.Left == null || node.Right == null)
                return node.Value;

            if (x[node.FeatureIndex] <= node.Threshold)
                return Traverse(node.Left, x);
            else
                return Traverse(node.Right, x);
        }


        private static double Variance(double[] y)
        {
            if (y.Length == 0) return 0;
            double mean = Mean(y);
            double sum = 0;
            foreach (var val in y)
                sum += Math.Pow(val - mean, 2);
            return sum / y.Length;
        }

        private static double VarianceGain(double[] parent, double[] left, double[] right)
        {
            double varParent = Variance(parent);
            double varLeft = Variance(left);
            double varRight = Variance(right);
            double wLeft = (double)left.Length / parent.Length;
            double wRight = (double)right.Length / parent.Length;
            return varParent - (wLeft * varLeft + wRight * varRight);
        }

        private static double Mean(double[] arr)
        {
            if (arr.Length == 0) return 0;
            double sum = 0;
            foreach (var v in arr) sum += v;
            return sum / arr.Length;
        }

        private static double[] GetColumn(double[,] matrix, int col)
        {
            int n = matrix.GetLength(0);
            double[] column = new double[n];
            for (int i = 0; i < n; i++)
                column[i] = matrix[i, col];
            return column;
        }

        private static (List<int> left, List<int> right) SplitIndices(double[] featureColumn, double threshold)
        {
            var left = new List<int>();
            var right = new List<int>();

            for (int i = 0; i < featureColumn.Length; i++)
            {
                if (featureColumn[i] <= threshold)
                    left.Add(i);
                else
                    right.Add(i);
            }

            return (left, right);
        }

        private static double[,] Subset(double[,] matrix, List<int> indices)
        {
            int n = indices.Count;
            int m = matrix.GetLength(1);
            double[,] result = new double[n, m];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                result[i, j] = matrix[indices[i], j];
            return result;
        }

        private static double[] Subset(double[] arr, List<int> indices)
        {
            double[] result = new double[indices.Count];
            for (int i = 0; i < indices.Count; i++)
                result[i] = arr[indices[i]];
            return result;
        }

        private class Node
        {
            public int FeatureIndex;
            public double Threshold;
            public double Value;
            public Node? Left;
            public Node? Right;
        }
    }
}