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

interface RangeComparisonChartProps {
  carAData: {
    year: number;
    predictedPrice: number;
  }[];
  carBData: {
    year: number;
    predictedPrice: number;
  }[];
  carALabel: string;
  carBLabel: string;
  algorithm: string;
}

const algorithmLabels: Record<string, string> = {
  linear: "Linear Regression",
  ridge: "Ridge Regression",
  ridge_rf: "Random Forest",
  ridge_gb: "Ridge GB",
};

export function RangeComparisonChart({
  carAData,
  carBData,
  carALabel,
  carBLabel,
  algorithm,
}: RangeComparisonChartProps) {
  const years = carAData.map((item) => item.year).sort((a, b) => a - b);

  const datasets = [
    {
      label: carALabel,
      data: carAData.map((item) => item.predictedPrice),
      borderColor: "rgb(59, 130, 246)", // blue
      backgroundColor: "rgba(59, 130, 246, 0.1)",
      borderWidth: 2,
      tension: 0.3,
      pointRadius: 5,
      pointHoverRadius: 7,
    },
    {
      label: carBLabel,
      data: carBData.map((item) => item.predictedPrice),
      borderColor: "rgb(239, 68, 68)", // red
      backgroundColor: "rgba(239, 68, 68, 0.1)",
      borderWidth: 2,
      tension: 0.3,
      pointRadius: 5,
      pointHoverRadius: 7,
    },
  ];

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
        text: `Price Comparison Over Years (${
          algorithmLabels[algorithm] || algorithm
        })`,
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
          text: "Year",
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
