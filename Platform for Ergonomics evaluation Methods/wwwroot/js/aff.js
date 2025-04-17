var charts = null;
var chartMaxY = 0;
var btnFitScale = document.getElementById('btnFitScale');
var curData;

function populateGraphs(graphData) {
    curData = graphData;
    var maxForce = 0;
    const vals = graphData.vals;
    for (var i = 0; i < 2; i++) {
        for (var j = 0; j < 2; j++) {
            maxForce = Math.max(maxForce, vals[j + i * graphData.labels.length].reduce((a, b) => Math.max(a, b), -Infinity));
        }
    }
    var maxY = Math.ceil(maxForce / 50) * 50;
    if (charts) {
        btnFitScale.disabled = maxY == chartMaxY;
        for (var i = 0; i < 2; i++) {
            for (var j = 0; j < graphData.labels.length; j++) {
                charts[i].data.datasets[j].data = vals[j + i * graphData.labels.length];
                charts[i].data.datasets[j].label = graphData.labels[j];
            }
            charts[i].update();
        }
        return;
    }
    chartMaxY = maxY;
    btnFitScale.disabled = true;
    charts = [];
    var mainContainer = document.getElementById("graphMain");
    mainContainer.innerHTML = "";
    var divLeft = mainContainer.appendChild(document.createElement("div"));
    mainContainer.appendChild(btnFitScale);
    var divRight = mainContainer.appendChild(document.createElement("div"));
    for (var i = 0; i < 2; i++) {
        var div = [divLeft, divRight][i];
        div.className = "chartContainer";
        div.appendChild(document.createElement("h3")).textContent = ["Left Arm", "Right Arm"][i];
        var canvas = div.appendChild(document.createElement("canvas"));
        var timeBar = div.appendChild(document.createElement("div"));
        timeBar.className = "timeBar";
        canvas.id = "chart" + i;
        var labelItr = 0;
        var datasets = [];
        for (var j = 0; j < graphData.labels.length; j++) {
            datasets.push({
                label: graphData.labels[j],
                data: vals[j + i * graphData.labels.length],
                borderColor: ["red", "green", "orange"][j],
                pointRadius: 0,
                yAxisID: j == 2 ? "percent" : "y"
            })
        }

        charts.push(new Chart(canvas.id, {
            type: "line",
            data: {
                labels: graphData.timestamps,
                datasets: datasets
            },
            options: {
                scales: {
                    y: {
                        title: {
                            display: true,
                            text: "Force (N)",
                        },
                        beginAtZero: true,
                        max: maxY
                    },
                    percent: {
                        title: {
                            display: graphData.labels.length > 2,
                            text: "Percent",
                        },
                        position: "right",
                        ticks: {
                            color: graphData.labels.length > 2 ? "darkgray" : "white"
                        },
                        beginAtZero: true

                    },
                    x: {
                        ticks: {
                            callback: function (index) {
                                value = graphData.timestamps[index];
                                return (~~(value * 1000) % 100 === 0) ? value.toFixed(2) : '';
                            }
                        },
                        title: {
                            display: true,
                            text: "Time (s)",
                        }

                    }
                }
            }
        }));
    }
    timeSlider.max = curData.timestamps.length - 1;
}
function fitScale() {
    charts = null;
    refresh();
}
function idxToTime(idx) {
    if (!curData) {
        return 0;
    }
    //idx = min(idx, curData.timestamps.length - 1);
    return ~~(100 * curData.timestamps[idx]) / 100; //Avoid epsilon artifacts; 

}
function refreshTime() {
    var idx = timeSlider.value;
    document.querySelectorAll('.timeBar').forEach(timeBar => {
        timeBar.style.left = "calc(65px + " + (77 * idx / timeSlider.max) + "%)";
    });
    console.log(curData.affJsons[idx]);
    var aff = JSON.parse(curData.affJsons[idx]);

    document.getElementById("debugPanel").innerHTML = "<pre>" + JSON.stringify(aff, null, 4) + "</pre>";
}

const genderSelect = document.getElementById('genderSelect');
genderSelect.onchange = function () { refresh(); }

function createSlider(parent, caption, val, minVal, maxVal, getValText, onInput) {
    var template = document.getElementById("templatesContainer").querySelector(".sliderContainer");
    var elm = template.cloneNode(true);
    elm.querySelector(".caption").innerHTML = caption;
    var slider = elm.querySelector(".slider");
    slider.min = minVal;
    slider.max = maxVal;
    slider.value = val;
    var valText = elm.querySelector(".sliderValue");
    valText.innerHTML = getValText(slider.value);
    slider.updateText = function () {
        valText.innerHTML = getValText(slider.value);
    }
    slider.addEventListener('input', function () {
        slider.updateText();
        onInput();
    });
    parent.appendChild(elm);
    return slider;
}
var commonControls = document.getElementById("commonControls");
const timeSlider = createSlider(commonControls, "Time", 0, 0, 1, (val) => {
    return idxToTime(val) + " s"; //Avoid epsilon artifacts;
}, refreshTime);

var AFFInputs = document.getElementById("AFFInputs");
const percentSlider = createSlider(AFFInputs, "Percent capable", 75, 1, 100, (val) => {
    return val + "%";
}, refresh);
const freqEffortsPerDaySlider = createSlider(AFFInputs, "Frequency (Efforts per Day)", 420, 1, 500, (val) => {
    return val;
}, refresh);
const effDurPerEffortSlider = createSlider(AFFInputs, "Effective Duration per Effort", 1, 1, 60, (val) => {
    return val + " sec";
}, refresh);

const loadSlider = createSlider(AFFInputs, "Load multiplier (demo)", 100, 10, 1000, (val) => {
    return val + "%";
}, refresh);
loadSlider.step = 10;

function normalize(vec) {
    const length = Math.hypot(...vec); // works for any dimensions

    if (length === 0) {
        return vec.map(() => 0); // Return zero vector to avoid division by zero
    }

    return vec.map(component => component / length);
}

function ForceInputs() {
    const self = this;
    const container = document.getElementById("ForceInputs");
    self.dirSliders = [];
    const dirScale = 100;
    function getDirSliderVal(v) {
        return parseFloat((v / dirScale).toFixed(2));
    }
    for (var i = 0; i < 3; i++) {
        var slider = createSlider(container, "XYZ"[i], 0, -dirScale, dirScale, (val) => {
            return getDirSliderVal(val) + "";
        }, refresh);
        slider.setVal = function (val) {
            this.value = ~~(val * dirScale);
            this.updateText();
        }
        self.dirSliders.push(slider);
    }
    function getDirArr() {
        var ret = [];
        self.dirSliders.forEach((slider) => { ret.push(getDirSliderVal(slider.value)); });
        return ret;
    }
    var normalizeBtn = container.appendChild(document.createElement("button"));
    normalizeBtn.innerHTML = "Normalize direction";
    normalizeBtn.onclick = function () {
        var normdir = normalize(getDirArr());
        for (var i = 0; i < 3; i++) {
            self.dirSliders[i].setVal(normdir[i]);
        }
    }
    self.forceSlider = createSlider(container, "Force", 0, 0, 100, (val) => {
        return val + " N";
    }, refresh);
    self.startSlider = createSlider(container, "Start time", 0, 0, 1, (val) => {
        return idxToTime(val) + " s";
    }, refresh);
    self.durSlider = createSlider(container, "Duration", 0, 0, 1, (val) => {
        return idxToTime(val) + " s"; 
    }, refresh);
    self.update = function () {
        self.startSlider.max = timeSlider.max;
        self.durSlider.max = timeSlider.max;
    }
    
    self.getJsonable = function () {
        return {
            dir: getDirArr(),
            force: self.forceSlider.value,
            startTime: idxToTime(self.startSlider.value),
            duration: idxToTime(self.durSlider.value)
        }
    }
}
const forceInputs = new ForceInputs();

//Prevent unnecessary high refresh rate (causes ugly graphs)
var refresher = null;
var pendingRefreshRequest = false;
function refresh() {
    pendingRefreshRequest = true;
    if (refresher) {
        return;
    }
    _refresh();
    refresher = setInterval(() => {
        if (!pendingRefreshRequest) {
            clearInterval(refresher);
            refresher = null;
        }
        pendingRefreshRequest = false;
        _refresh();
    }, 100);
}
function _refresh() {
    $.ajax({
        url: '/AffTest/GetGraphValArrs',
        method: 'GET',
        data: {
            percentCapable: percentSlider.value,
            demoLoadPercent: loadSlider.value,
            altGender: genderSelect.value,
            mafParamsJson: JSON.stringify({
                freqEffortsPerDay: [freqEffortsPerDaySlider.value, freqEffortsPerDaySlider.value],
                effDurPerEffortSec: [effDurPerEffortSlider.value, effDurPerEffortSlider.value]
            }),
            additionalForceJson:JSON.stringify(forceInputs.getJsonable()),
        },
        success: function (response) {
            console.log(response);
            document.getElementById("messagePanel").innerHTML = response.messages.join("<br>");
            populateGraphs(response);
            refreshTime();
            forceInputs.update();
        },
        error: function () {
            //console.log("Failed to fetch message.");
            setTimeout(refresh, 1000);
        }

    });
}

document.getElementById("downloadCsvBtn").addEventListener("click", function () {
    if (!charts || charts.length < 2) {
        alert("No data available to download.");
        return;
    }

    let csvContent = "Time (s),"; // Start with time column

    // Retrieve dataset labels dynamically for both charts
    let leftLabels = charts[0].data.datasets.map(dataset => "Left " + dataset.label);
    let rightLabels = charts[1].data.datasets.map(dataset => "Right " + dataset.label);

    // Combine headers: Time column + Left Arm labels + Right Arm labels
    csvContent += [...leftLabels, ...rightLabels].join(",") + "\n";

    let timeLabels = charts[0].data.labels; // X-axis time values
    let leftDatasets = charts[0].data.datasets; // Left Arm datasets
    let rightDatasets = charts[1].data.datasets; // Right Arm datasets

    // Iterate over time points
    for (let i = 0; i < timeLabels.length; i++) {
        let time = timeLabels[i];
        let row = [time]; // Start with the time value

        // Extract each dataset value for Left Arm at this time index
        leftDatasets.forEach(dataset => {
            row.push(dataset.data[i]);
        });

        // Extract each dataset value for Right Arm at this time index
        rightDatasets.forEach(dataset => {
            row.push(dataset.data[i]);
        });

        csvContent += row.join(",") + "\n";
    }

    // Create a Blob with the CSV data
    let blob = new Blob([csvContent], { type: 'text/csv' });
    let link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = "force_data.csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
});


refresh();
