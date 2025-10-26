async function refreshManikinPicker() {
    try {
        const res = await fetch('/api/manikin/list');
        if (!res.ok) return;
        const data = await res.json();
        const sel = document.getElementById('manikinPicker');
        if (!sel) return;

        sel.innerHTML = '';
        for (const item of data.items) {
            const opt = document.createElement('option');
            opt.value = item.id;
            opt.textContent = item.name || item.id;
            if (data.activeId === item.id) opt.selected = true;
            sel.appendChild(opt);
        }
    } catch (e) { /* ignore */ }
}

async function selectActiveManikin(id) {
    try {
        await fetch('/api/manikin/select', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id })
        });
        // Optionally notify or refresh graphs here.
    } catch (e) { /* ignore */ }
}

document.addEventListener('DOMContentLoaded', () => {
    const sel = document.getElementById('manikinPicker');
    if (sel) {
        refreshManikinPicker();
        sel.addEventListener('change', e => selectActiveManikin(e.target.value));
    }
});
