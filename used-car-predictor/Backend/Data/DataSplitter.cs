namespace used_car_predictor.Backend.Data;

public static class DataSplitter
{
    public static (double[,], double[], double[,], double[]) Split(
        double[,] features, double[] labels, double trainRatio = 0.8)
    {
        int sampleCount = features.GetLength(0);
        int featureCount = features.GetLength(1);
        int trainSize = (int)(sampleCount * trainRatio);
        int testSize = sampleCount - trainSize;

        var trainFeatures = new double[trainSize, featureCount];
        var trainLabels = new double[trainSize];
        var testFeatures = new double[testSize, featureCount];
        var testLabels = new double[testSize];

        for (int i = 0; i < trainSize; i++)
        {
            for (int j = 0; j < featureCount; j++)
                trainFeatures[i, j] = features[i, j];
            trainLabels[i] = labels[i];
        }
        
        for (int i = trainSize; i < sampleCount; i++)
        {
            for (int j = 0; j < featureCount; j++)
                testFeatures[i - trainSize, j] = features[i, j];
            testLabels[i - trainSize] = labels[i];
        }

        return (trainFeatures, trainLabels, testFeatures, testLabels);
    }
}