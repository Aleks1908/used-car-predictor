import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { Line } from "react-chartjs-2";

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface PriceRangeChartProps {
  items: {
    targetYear: number;
    results: {
      algorithm: string;
      predictedPrice: number;
    }[];
  }[];
}

const algorithmColors = {
  linear: {
    border: "rgb(59, 130, 246)", // blue
    background: "rgba(59, 130, 246, 0.1)",
  },
  ridge: {
    border: "rgb(34, 197, 94)", // green
    background: "rgba(34, 197, 94, 0.1)",
  },
  ridge_rf: {
    border: "rgb(251, 146, 60)", // orange
    background: "rgba(251, 146, 60, 0.1)",
  },
  ridge_gb: {
    border: "rgb(236, 72, 153)", // pink
    background: "rgba(236, 72, 153, 0.1)",
  },
};

const algorithmLabels = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Ridge GB",
};

export function PriceRangeChart({ items }: PriceRangeChartProps) {
  // Extract years (sorted)
  const years = items.map((item) => item.targetYear).sort((a, b) => a - b);

  // Get all unique algorithms
  const algorithms = items[0]?.results.map((r) => r.algorithm) || [];

  // Create datasets for each algorithm
  const datasets = algorithms.map((algorithm) => {
    const data = items
      .sort((a, b) => a.targetYear - b.targetYear)
      .map((item) => {
        const result = item.results.find((r) => r.algorithm === algorithm);
        return result?.predictedPrice || 0;
      });

    return {
      label: algorithmLabels[algorithm as keyof typeof algorithmLabels],
      data,
      borderColor:
        algorithmColors[algorithm as keyof typeof algorithmColors].border,
      backgroundColor:
        algorithmColors[algorithm as keyof typeof algorithmColors].background,
      borderWidth: 2,
      tension: 0.3,
      pointRadius: 4,
      pointHoverRadius: 6,
    };
  });

  const chartData = {
    labels: years,
    datasets,
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: "top" as const,
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
          },
        },
      },
      title: {
        display: true,
        text: "Predicted Price Over Years by Algorithm",
        font: {
          size: 16,
        },
        padding: {
          bottom: 20,
        },
      },
      tooltip: {
        callbacks: {
          label: function (context: {
            dataset: { label?: string };
            parsed: { y: number | null };
          }) {
            let label = context.dataset.label || "";
            if (label) {
              label += ": ";
            }
            if (context.parsed.y !== null) {
              label += "$" + context.parsed.y.toLocaleString();
            }
            return label;
          },
        },
      },
    },
    scales: {
      y: {
        beginAtZero: false,
        ticks: {
          callback: function (value: number | string) {
            return "$" + Number(value).toLocaleString();
          },
        },
        title: {
          display: true,
          text: "Predicted Price",
          font: {
            size: 14,
          },
        },
      },
      x: {
        title: {
          display: true,
          text: "Target Year",
          font: {
            size: 14,
          },
        },
      },
    },
  };

  return (
    <div className="w-full h-[500px]">
      <Line data={chartData} options={options} />
    </div>
  );
}
