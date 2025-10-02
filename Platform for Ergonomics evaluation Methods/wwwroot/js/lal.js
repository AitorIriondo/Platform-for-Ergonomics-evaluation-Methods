document.addEventListener("DOMContentLoaded", function () {
    const btn = document.getElementById("calculateBtn");
    const tableBody = document.querySelector("#lalResults tbody");

    btn.addEventListener("click", async () => {
        //const dt = parseFloat(document.getElementById("dtInput").value) || 0.1;

        try {
            const response = await fetch(`/api/lal/percentiles`);
            if (!response.ok) {
                const error = await response.json();
                alert("Error: " + (error.error || response.statusText));
                return;
            }

            const data = await response.json();

            // Clear previous rows
            tableBody.innerHTML = "";

            // Fill table
            for (const [metric, percentiles] of Object.entries(data)) {
                const row = document.createElement("tr");
                row.innerHTML = `
                    <td>${metric}</td>
                    <td>${percentiles.p10.toFixed(2)}</td>
                    <td>${percentiles.p50.toFixed(2)}</td>
                    <td>${percentiles.p90.toFixed(2)}</td>
                `;
                tableBody.appendChild(row);
            }
        } catch (err) {
            alert("Request failed: " + err);
        }
    });
});
