import { useState } from "react";
import SinglePrediction from "./SinglePrediction";
import RangePrediction from "./RangePrediction";
import ComparisonPrediction from "./ComparisonPrediction";

function Home() {
  const [activeView, setActiveView] = useState<string | null>(null);

  const options = [
    { id: "single", title: "Single Prediction", path: "/single-prediction" },
    { id: "range", title: "Range Prediction", path: "/range-prediction" },
    { id: "comparison", title: "Car Comparison", path: "/car-comparison" },
    {
      id: "range-comparison",
      title: "Range Comparison",
      path: "/range-comparison",
    },
  ];

  if (activeView === "single") {
    return <SinglePrediction onBack={() => setActiveView(null)} />;
  }

  if (activeView === "range") {
    return <RangePrediction onBack={() => setActiveView(null)} />;
  }

  if (activeView === "comparison") {
    return <ComparisonPrediction onBack={() => setActiveView(null)} />;
  }

  return (
    <div className="min-h-screen bg-linear-to-br from-gray-100 to-gray-200 p-8 flex items-center justify-center">
      <div className="w-full max-w-5xl bg-white rounded-3xl shadow-2xl border-2 border-gray-300 p-12">
        <h1 className="text-4xl font-bold text-center text-gray-900 mb-12">
          Welcome to used-cars-pricePredictor
        </h1>

        <div className="grid grid-cols-2 gap-6 ">
          {options.map((option) => (
            <button
              key={option.id}
              className="h-40 cursor-pointer bg-white rounded-2xl border-2 border-gray-400 hover:border-gray-900 hover:shadow-lg transition-all duration-200 flex items-center justify-center text-xl font-semibold text-gray-900 hover:bg-gray-50"
              onClick={() => setActiveView(option.id)}
            >
              {option.title}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

export default Home;
