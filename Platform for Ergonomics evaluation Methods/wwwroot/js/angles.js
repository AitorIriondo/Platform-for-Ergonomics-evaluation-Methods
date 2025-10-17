// ===== State & constants =====
let anglesChart = null;
let currentXAxis = null; // array of times on the chart
let seriesCounter = 0;

const COLOR_PALETTE = [
    "#e6194b", "#3cb44b", "#0082c8", "#f58231", "#911eb4", "#46f0f0",
    "#f032e6", "#d2f53c", "#fabebe", "#008080", "#e6beff", "#aa6e28",
    "#fffac8", "#800000", "#aaffc3", "#808000", "#ffd8b1", "#000080",
    "#808080", "#000000"
];

let xMinEl = null, xMaxEl = null, xLabelEl = null;

function nextColor(i) { return COLOR_PALETTE[i % COLOR_PALETTE.length]; }

// ===== Utilities =====
function nearlyEqual(a, b, eps = 1e-6) { return Math.abs(a - b) <= eps; }

function arraysEqual(a, b, eps = 1e-6) {
    if (!Array.isArray(a) || !Array.isArray(b) || a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) if (!nearlyEqual(+a[i], +b[i], eps)) return false;
    return true;
}

// nearest-neighbor resampling of srcValues from srcTimes onto targetTimes
function resampleToAxis(srcTimes, srcValues, targetTimes) {
    const out = new Array(targetTimes.length);
    let i = 0;
    for (let k = 0; k < targetTimes.length; k++) {
        const t = +targetTimes[k];
        while (i + 1 < srcTimes.length &&
            Math.abs(srcTimes[i + 1] - t) <= Math.abs(srcTimes[i] - t)) {
            i++;
        }
        out[k] = srcValues[i];
    }
    return out;
}

function addSeriesChip(id, label, color) {
    const list = document.getElementById('seriesList');
    if (!list) return;
    const chip = document.createElement('span');
    chip.className = 'series-chip';
    chip.dataset.seriesId = id;

    const dot = document.createElement('span');
    dot.className = 'color-dot';
    dot.style.background = color;

    const text = document.createElement('span');
    text.textContent = label;

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.textContent = '✕';
    btn.title = 'Remove series';
    btn.addEventListener('click', () => removeSeries(id));

    chip.append(dot, text, btn);
    list.appendChild(chip);
}

function removeSeries(id) {
    if (!anglesChart) return;

    // Remove dataset
    const idx = anglesChart.data.datasets.findIndex(ds => ds._seriesId === id);
    if (idx >= 0) {
        anglesChart.data.datasets.splice(idx, 1);
    }

    // Remove chip
    const chip = document.querySelector(`.series-chip[data-series-id="${id}"]`);
    if (chip) chip.remove();

    // If no datasets left, reset
    if (anglesChart.data.datasets.length === 0) {
        currentXAxis = null;
        anglesChart.data.labels = [];
        anglesChart.update('none');
        return;
    }

    // 1) Find new longest length
    const newLen = Math.max(...anglesChart.data.datasets.map(ds => ds.data.length));

    // 2) Shrink/grow currentXAxis
    currentXAxis = currentXAxis.slice(0, newLen);

    // 3) Adjust labels
    anglesChart.data.labels = currentXAxis.slice();

    // 4) Truncate/pad datasets
    for (const ds of anglesChart.data.datasets) {
        if (ds.data.length > newLen) {
            ds.data.length = newLen; // truncate
        } else {
            while (ds.data.length < newLen) ds.data.push(null); // pad
        }
    }

    anglesChart.update('none');
}


// ===== Chart bootstrap =====
function ensureChart() {
    if (anglesChart) return;
    const canvas = document.getElementById('anglesChart');
    if (!canvas) {
        console.warn('Missing #anglesChart canvas');
        return;
    }
    const ctx = canvas.getContext('2d');
    anglesChart = new Chart(ctx, {
        type: 'line',
        data: { labels: currentXAxis || [], datasets: [] },
        options: {
            responsive: true,
            elements: { point: { radius: 0, hoverRadius: 0, hitRadius: 0 } },
            plugins: {
                decimation: { enabled: true, algorithm: 'lttb', samples: 1200 },
                zoom: {
                    zoom: {
                        drag: {
                            enabled: true,
                            backgroundColor: 'rgba(0,0,0,0.12)',
                            borderColor: 'rgba(0,0,0,0.4)',
                            borderWidth: 1,
                            threshold: 2
                        },
                        mode: 'x'
                    },
                    pan: { enabled: false },
                    wheel: { enabled: false },
                    pinch: { enabled: false },
                    onZoomComplete: ({ chart }) => {
                        const s = chart.scales.x;
                        if (typeof s.min === 'number' && typeof s.max === 'number') {
                            // keep the dual sliders in sync if you're using them
                            if (window.xMinEl && window.xMaxEl) {
                                xMinEl.value = String(s.min);
                                xMaxEl.value = String(s.max);
                            }
                            if (typeof updateXRangeLabel === 'function') updateXRangeLabel();
                        }
                    }
                }
            },
            scales: {
                x: { type: 'category', title: { display: true, text: 'Time (s)' } },
                y: { title: { display: true, text: 'Angle (deg)' } }
            }
        }

    });
}

// ===== API helpers (defensive) =====
async function fetchManikinList() {
    // Try API
    try {
        const r = await fetch('/api/manikin/list');
        if (r.ok) {
            const data = await r.json();
            // supports either { items: [...] } or raw array
            if (Array.isArray(data)) return data;
            if (Array.isArray(data.items)) return data.items;
        }
    } catch (e) {
        console.warn('manikin/list fetch failed', e);
    }
    // Fallback: clone global picker from layout, if present
    const globalPicker = document.getElementById('manikinPicker');
    if (globalPicker) {
        return Array.from(globalPicker.options).map(o => ({ id: o.value, name: o.textContent }));
    }
    return [];
}

async function fetchAngleOptions(manikinId) {
    const r = await fetch(`/api/angles/available${manikinId ? `?manikinId=${encodeURIComponent(manikinId)}` : ""}`);
    if (r.ok) return await r.json();  // now [{key:"backAng", label:"Back Flexion..."}]
    return [];
}


async function fetchSeries(manikinId, angleKey) {
    const url = `/api/angles/series?manikinId=${encodeURIComponent(manikinId)}&angle=${encodeURIComponent(angleKey)}`;
    const r = await fetch(url);
    if (!r.ok) throw new Error(await r.text());
    return await r.json(); // { time, values, label, manikinId, angle }
}

// ===== Selectors & Add button =====
async function populateSelectors() {
    const manSel = document.getElementById('anglesManikinSelect');
    const angSel = document.getElementById('angleKindSelect');
    if (!manSel || !angSel) return;

    // --- Fill manikin select ---
    const manikins = await fetchManikinList();
    manSel.innerHTML = '';
    for (const m of manikins) {
        const opt = document.createElement('option');
        opt.value = m.id ?? m;                // support either {id,name} or raw string
        opt.textContent = m.name ?? m.id ?? m;
        manSel.appendChild(opt);
    }

    // --- Fill angle select (based on first manikin or current selection) ---
    const chosenManikinId = manSel.value;
    const angles = await fetchAngleOptions(chosenManikinId);

    angSel.innerHTML = '';
    for (const a of angles) {
        const opt = document.createElement('option');
        if (typeof a === "string") {
            opt.value = a;
            opt.textContent = a;
        } else {
            opt.value = a.key;
            opt.textContent = a.label || a.key;
        }
        angSel.appendChild(opt);
    }
}



async function onAddClick() {
    const manSel = document.getElementById('anglesManikinSelect');
    const angSel = document.getElementById('angleKindSelect');
    if (!manSel || !angSel || !manSel.value || !angSel.value) return;

    try {
        const series = await fetchSeries(manSel.value, angSel.value);
        ensureChart();

        document.getElementById('xDragReset')?.addEventListener('click', () => {
            if (!anglesChart) return;
            anglesChart.resetZoom();                 // clear min/max on X

            // if you keep the dual sliders, refresh them too:
            initXRangeUI?.refreshDomain?.();
            updateXRangeLabel?.();
        });

        // If no X axis yet OR this series is longer, adopt it
        if (!currentXAxis || series.time.length > currentXAxis.length) {
            currentXAxis = series.time.slice();
            if (anglesChart) anglesChart.data.labels = currentXAxis.slice();

            // Pad existing datasets to new length
            if (anglesChart) {
                for (const ds of anglesChart.data.datasets) {
                    while (ds.data.length < currentXAxis.length) {
                        ds.data.push(null);
                    }
                }
            }
        }
        initXRangeUI?.refreshDomain?.();
        updateXRangeLabel();

        // Prepare Y data with null padding
        let y = series.values.slice();
        if (y.length < currentXAxis.length) {
            while (y.length < currentXAxis.length) y.push(null);
        }

        const color = nextColor(seriesCounter++);
        const id = `s${Date.now()}_${seriesCounter}`;
        anglesChart.data.datasets.push({
            _seriesId: id,
            label: series.label,
            data: y,
            borderColor: color,
            borderWidth: 1,
            fill: false,
            tension: 0.1,
            pointRadius: 0,
            pointHoverRadius: 0,
            showLine: true
        });
        addSeriesChip(id, series.label, color);
        anglesChart.update('none');
    } catch (e) {
        alert(e.message || e);
    }
}

// CSV download
function downloadCsv() {
    if (!anglesChart || !currentXAxis) {
        alert("No data to download.");
        return;
    }

    const header = ["Time (s)", ...anglesChart.data.datasets.map(ds => ds.label)];
    let csv = header.join(",") + "\n";

    for (let i = 0; i < currentXAxis.length; i++) {
        const row = [currentXAxis[i]];
        for (const ds of anglesChart.data.datasets) {
            row.push(ds.data[i] !== undefined && ds.data[i] !== null ? ds.data[i] : "");
        }
        csv += row.join(",") + "\n";
    }

    const blob = new Blob([csv], { type: "text/csv" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = "angles_data.csv";
    link.click();
    URL.revokeObjectURL(link.href);
}

document.getElementById("downloadCsvBtn")?.addEventListener("click", downloadCsv);

function setXAxisView(idxMin, idxMax) {
    if (!anglesChart || !currentXAxis || currentXAxis.length === 0) return;

    // clamp and floor to integers
    idxMin = Math.max(0, Math.min(currentXAxis.length - 1, Math.floor(idxMin)));
    idxMax = Math.max(idxMin, Math.min(currentXAxis.length - 1, Math.floor(idxMax)));

    const xScale = anglesChart.options.scales.x || (anglesChart.options.scales.x = { type: 'category' });
    xScale.min = idxMin;   // indices for category scale
    xScale.max = idxMax;

    anglesChart.update('none');
    updateXRangeLabel();
}


function updateXRangeLabel() {
    if (!xLabelEl || !currentXAxis || currentXAxis.length === 0) return;

    const xScale = anglesChart?.options?.scales?.x || {};
    const fullMinIdx = 0;
    const fullMaxIdx = currentXAxis.length - 1;

    const vminIdx = (typeof xScale.min === 'number') ? xScale.min : fullMinIdx;
    const vmaxIdx = (typeof xScale.max === 'number') ? xScale.max : fullMaxIdx;

    const fullMin = +currentXAxis[fullMinIdx];
    const fullMax = +currentXAxis[fullMaxIdx];
    const vmin = +currentXAxis[vminIdx];
    const vmax = +currentXAxis[vmaxIdx];

    xLabelEl.textContent =
        `showing [${vminIdx}…${vmaxIdx}] => ${vmin.toFixed(3)} → ${vmax.toFixed(3)} ` +
        `(full: 0…${fullMaxIdx} => ${fullMin.toFixed(3)} → ${fullMax.toFixed(3)})`;
}



function initXRangeUI() {
    xMinEl = document.getElementById('xMin');
    xMaxEl = document.getElementById('xMax');
    xLabelEl = document.getElementById('xRangeLabel');

    if (!xMinEl || !xMaxEl) return;

    const setDomainFromAxis = () => {
        if (!currentXAxis || currentXAxis.length < 2) return;
        const loIdx = 0;
        const hiIdx = currentXAxis.length - 1;

        xMinEl.min = loIdx; xMinEl.max = hiIdx; xMinEl.step = 1; xMinEl.value = loIdx;
        xMaxEl.min = loIdx; xMaxEl.max = hiIdx; xMaxEl.step = 1; xMaxEl.value = hiIdx;

        setXAxisView(loIdx, hiIdx); // reset to full view
    };

    const onMinInput = () => {
        let minIdx = +xMinEl.value, maxIdx = +xMaxEl.value;
        if (minIdx > maxIdx) { maxIdx = minIdx; xMaxEl.value = String(maxIdx); }
        setXAxisView(minIdx, maxIdx);
    };
    const onMaxInput = () => {
        let minIdx = +xMinEl.value, maxIdx = +xMaxEl.value;
        if (maxIdx < minIdx) { minIdx = maxIdx; xMinEl.value = String(minIdx); }
        setXAxisView(minIdx, maxIdx);
    };

    xMinEl.addEventListener('input', onMinInput);
    xMaxEl.addEventListener('input', onMaxInput);
    document.getElementById('xRangeReset')?.addEventListener('click', setDomainFromAxis);

    initXRangeUI.refreshDomain = setDomainFromAxis;
}




// ===== Bootstrap =====
document.addEventListener('DOMContentLoaded', async () => {
    await populateSelectors();
    ensureChart();
    initXRangeUI();

    const btn = document.getElementById('addSeriesBtn');
    if (btn) btn.addEventListener('click', onAddClick);

    // Optional: If global layout manikinPicker changes, refresh our manikin list
    const globalPicker = document.getElementById('manikinPicker');
    if (globalPicker) {
        globalPicker.addEventListener('change', async () => {
            await populateSelectors();
        });
    }
});
