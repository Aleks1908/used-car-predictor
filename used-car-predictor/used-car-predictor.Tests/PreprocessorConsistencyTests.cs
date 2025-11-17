using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using used_car_predictor.Backend.Data;  
using Xunit;

[ExcludeFromCodeCoverage]
public class PreprocessorConsistencyTests
{
    [Fact]
    public void SameInput_ProducesStableFuelsAndTransmissions_OrderAndEncoding()
    {
        var rows = new List<Vehicle>
        {
            new Vehicle { Manufacturer="Toyota", Model="Yaris",  Year=2018, Price=8000,  Fuel="gas",  Transmission="manual", Odometer=90000 },
            new Vehicle { Manufacturer="Toyota", Model="Yaris",  Year=2019, Price=8500,  Fuel="diesel", Transmission="automatic", Odometer=70000 },
            new Vehicle { Manufacturer="Toyota", Model="Yaris",  Year=2017, Price=7800,  Fuel="gas",  Transmission="automatic", Odometer=110000 },
        };

        var (x1, y1, fuels1, trans1) = Preprocessor.ToMatrix(rows, targetYear: 2030, anchorTargetYear: 2030);
        var (x2, y2, fuels2, trans2) = Preprocessor.ToMatrix(rows, targetYear: 2030, anchorTargetYear: 2030);
        
        fuels1.Should().BeEquivalentTo(fuels2, o => o.WithStrictOrdering());
        trans1.Should().BeEquivalentTo(trans2, o => o.WithStrictOrdering());

        x1.GetLength(0).Should().Be(x2.GetLength(0)); 
        x1.GetLength(1).Should().Be(x2.GetLength(1)); 
        for (int i = 0; i < x1.GetLength(0); i++)
            for (int j = 0; j < x1.GetLength(1); j++)
                x1[i,j].Should().BeApproximately(x2[i,j], 1e-12);

        y1.Should().HaveCount(y2.Length);
        for (int i = 0; i < y1.Length; i++)
            y1[i].Should().BeApproximately(y2[i], 1e-12);

        var row0 = GetRow(x1, 0);
        var row1 = GetRow(x1, 1);
        row0.SequenceEqual(row1).Should().BeFalse("fuel/transmission differ so one-hots must differ");
    }

    private static double[] GetRow(double[,] m, int r)
    {
        int p = m.GetLength(1);
        var a = new double[p];
        for (int j = 0; j < p; j++) a[j] = m[r, j];
        return a;
    }
}