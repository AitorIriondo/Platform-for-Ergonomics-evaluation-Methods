let anglesChart = null;

async function loadChart() {
    const res = await fetch('/api/angles/timeseries');
    const data = await res.json();

    const ctx = document.getElementById("anglesChart").getContext("2d");

    // Destroy old chart if it exists (prevents layering)
    if (anglesChart) {
        anglesChart.destroy();
    }

    anglesChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.time,
            datasets: [
                {
                    label: "Head Flexion",
                    data: data.headFlexion,
                    borderColor: "red",
                    borderWidth: 1,
                    fill: false,
                    tension: 0.1,
                    showLine: true,
                    pointRadius: 0,        // 🚀 no points
                    pointHoverRadius: 0
                },
                {
                    label: "Upper Arm Left",
                    data: data.upperArmLeft,
                    borderColor: "blue",
                    borderWidth: 1,
                    fill: false,
                    tension: 0.1,
                    showLine: true,
                    pointRadius: 0,        // 🚀 no points
                    pointHoverRadius: 0
                },
                {
                    label: "Upper Arm Right",
                    data: data.upperArmRight,
                    borderColor: "green",
                    borderWidth: 1,
                    fill: false,
                    tension: 0.1,
                    showLine: true,
                    pointRadius: 0,        // 🚀 no points
                    pointHoverRadius: 0
                }
            ]
        },
        options: {
            responsive: true,
            elements: {
                point: {
                    radius: 0,        // 🚀 global default = no points
                    hoverRadius: 0,
                    hitRadius: 0
                }
            },
            scales: {
                x: { title: { display: true, text: "Time (s)" } },
                y: { title: { display: true, text: "Angle (deg)" } }
            }
        }
    });
}

document.addEventListener("DOMContentLoaded", () => {
    loadChart();
    const picker = document.getElementById('manikinPicker');
    if (picker) {
        picker.addEventListener('change', async (e) => {
            await fetch('/api/manikin/select', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: e.target.value })
            });
            loadChart();
        });
    }
});
