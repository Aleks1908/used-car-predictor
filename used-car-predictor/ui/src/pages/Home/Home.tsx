import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";

function Home() {
  const [count, setCount] = useState(0);
  const [catalog, setCatalog] = useState(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadCatalog() {
      try {
        const res = await fetch("/api/v1/catalog");
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        setCatalog(data);
      } catch (err) {
        console.error("Catalog fetch failed:", err);
        setError(err instanceof Error ? err.message : "An error occurred");
      }
    }
    loadCatalog();
  }, []);

  return (
    <>
      <h1>Vite + React</h1>
      <div className="card">
        <button onClick={() => setCount((count) => count + 1)}>
          count is {count}
        </button>
        <p>
          Edit <code>src/App.jsx</code> and save to test HMR
        </p>
      </div>
      {error && <p style={{ color: "red" }}>Error: {error}</p>}
      {catalog && <pre>{JSON.stringify(catalog, null, 2)}</pre>}
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
      <h1 className="text-3xl font-bold underline">Hello world!</h1>
      <Button>aaaa</Button>
    </>
  );
}

export default Home;
