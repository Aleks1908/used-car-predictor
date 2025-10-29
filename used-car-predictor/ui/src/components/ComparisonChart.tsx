import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { Bar } from "react-chartjs-2";

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend
);

interface ComparisonChartProps {
  carAResults: {
    algorithm: string;
    predictedPrice: number;
  }[];
  carBResults: {
    algorithm: string;
    predictedPrice: number;
  }[];
  carALabel: string;
  carBLabel: string;
}

const algorithmLabels: Record<string, string> = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Ridge GB",
};

export function ComparisonChart({
  carAResults,
  carBResults,
  carALabel,
  carBLabel,
}: ComparisonChartProps) {
  // Get algorithm names from results
  const algorithms = carAResults.map(
    (r) => algorithmLabels[r.algorithm] || r.algorithm
  );

  // Create datasets for each car
  const datasets = [
    {
      label: carALabel,
      data: carAResults.map((r) => r.predictedPrice),
      backgroundColor: "rgba(59, 130, 246, 0.8)", // blue
      borderColor: "rgb(59, 130, 246)",
      borderWidth: 1,
    },
    {
      label: carBLabel,
      data: carBResults.map((r) => r.predictedPrice),
      backgroundColor: "rgba(239, 68, 68, 0.8)", // red
      borderColor: "rgb(239, 68, 68)",
      borderWidth: 1,
    },
  ];

  const chartData = {
    labels: algorithms,
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
        text: "Price Comparison by Algorithm",
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
          text: "Algorithm",
          font: {
            size: 14,
          },
        },
      },
    },
  };

  return (
    <div className="w-full h-[500px]">
      <Bar data={chartData} options={options} />
    </div>
  );
}
