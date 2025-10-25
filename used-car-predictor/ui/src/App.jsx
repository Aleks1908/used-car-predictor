import { useState, useEffect } from "react";
import reactLogo from "./assets/react.svg";
import viteLogo from "/vite.svg";
import "./App.css";
import { Button } from "@/components/ui/button";

function App() {
  const [count, setCount] = useState(0);
  const [catalog, setCatalog] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function loadCatalog() {
      try {
        const res = await fetch("/api/v1/catalog"); // ← Vite proxy forwards this
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        setCatalog(data);
      } catch (err) {
        console.error("Catalog fetch failed:", err);
        setError(err.message);
      }
    }
    loadCatalog();
  }, []);

  console.log(catalog);
  console.log(error);

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.jsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
      <h1 className="text-3xl font-bold underline">Hello world!</h1>
      <Button>aaaa</Button>
    </>
  );
}

export default App;
